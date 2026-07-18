using Common.Extensions;
using Common.FileValidation;
using Common.Utilities;
using Confluent.Kafka;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSerilog((services, cfg) => cfg
    .ReadFrom.Services(services)
    .Enrich.WithProperty("Service", "UploadService")
    .WriteTo.Console(new CompactJsonFormatter()));

builder.AddSharedObservability();

var kafkaHealthSettings = builder.Configuration.GetSection(KafkaSettings.SectionName).Get<KafkaSettings>() ?? new KafkaSettings();
var minioHealthSettings = builder.Configuration.GetSection(MinioSettings.SectionName).Get<MinioSettings>() ?? new MinioSettings();
var minioScheme = minioHealthSettings.UseSSL ? "https" : "http";

builder.Services.AddHealthChecks()
    .AddKafka(config =>
    {
        config.BootstrapServers = kafkaHealthSettings.BootstrapServers;
    }, name: "kafka")
    .AddUrlGroup(new Uri($"{minioScheme}://{minioHealthSettings.Endpoint}/minio/health/live"), name: "minio");

builder.Services.AddSingleton<IGuidGenerator, GuidGenerator>();

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(KafkaSettings.SectionName));
builder.Services.AddSingleton<IKafkaProducer, KafkaProducerService>();
builder.Services.AddHostedService<KafkaTopicInitializer>();

builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection(MinioSettings.SectionName));
builder.Services.AddSingleton<IMinioStorage, MinioStorageService>();

builder.Services.AddSingleton<IFileSignatureValidator, FileSignatureValidator>();

builder.Services.AddControllers();
builder.Services.AddSingleton<IFileHandlerService, FileHandlerService>();
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseHttpMetrics();
app.MapMetrics();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter.WriteJson });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.MapControllers();

app.Run();
