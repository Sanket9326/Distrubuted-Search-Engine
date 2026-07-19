using Common.Extensions;
using Common.FileValidation;
using Common.Reliability;
using Common.Utilities;
using Confluent.Kafka;
using Consumers;
using HostedServices;
using Infrastructure;
using Kafka;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;
using Prometheus;
using Repositories;
using Serilog;
using Serilog.Formatting.Compact;
using Services;
using Services.Chunking;
using Services.TextExtraction;
using Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, cfg) => cfg
    .ReadFrom.Services(services)
    .Enrich.WithProperty("Service", "DocumentIngestionService")
    .WriteTo.Console(new CompactJsonFormatter()));

builder.AddSharedObservability();
builder.AddSharedRedis();

var postgresHealthSettings = builder.Configuration.GetSection(PostgresSettings.SectionName).Get<PostgresSettings>() ?? new PostgresSettings();
var kafkaHealthSettings = builder.Configuration.GetSection(KafkaConsumerSettings.SectionName).Get<KafkaConsumerSettings>() ?? new KafkaConsumerSettings();
var minioHealthSettings = builder.Configuration.GetSection(MinioSettings.SectionName).Get<MinioSettings>() ?? new MinioSettings();
var minioScheme = minioHealthSettings.UseSSL ? "https" : "http";
var redisHealthSettings = builder.Configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();

builder.Services.AddHealthChecks()
    .AddNpgSql(postgresHealthSettings.ConnectionString, name: "postgres")
    .AddKafka(config =>
    {
        config.BootstrapServers = kafkaHealthSettings.BootstrapServers;
    }, name: "kafka")
    .AddUrlGroup(new Uri($"{minioScheme}://{minioHealthSettings.Endpoint}/minio/health/live"), name: "minio")
    .AddRedis(redisHealthSettings.ConnectionString, name: "redis");

builder.Services.Configure<KafkaConsumerSettings>(builder.Configuration.GetSection(KafkaConsumerSettings.SectionName));
builder.Services.AddScoped<IDocumentUploadedConsumer, DocumentUploadedConsumer>();
builder.Services.AddHostedService<KafkaConsumerHostedService>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducerService>();
builder.Services.AddHostedService<KafkaTopicInitializer>();

builder.Services.Configure<PostgresSettings>(builder.Configuration.GetSection(PostgresSettings.SectionName));
builder.Services.AddDbContext<DocumentIngestionDbContext>((serviceProvider, options) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<PostgresSettings>>().Value;
    options.UseNpgsql(settings.ConnectionString);
});
builder.Services.AddScoped<IDocumentMetadataRepository, DocumentMetadataRepository>();
builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();

builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection(MinioSettings.SectionName));
builder.Services.AddSingleton<IFileStorage, MinioFileStorageService>();

builder.Services.Configure<FileProcessingOptions>(builder.Configuration.GetSection(FileProcessingOptions.SectionName));
builder.Services.Configure<ChunkingOptions>(builder.Configuration.GetSection(ChunkingOptions.SectionName));

builder.Services.AddSingleton<IGuidGenerator, GuidGenerator>();
builder.Services.AddSingleton<IFileSignatureValidator, FileSignatureValidator>();
builder.Services.AddSingleton<ITextExtractor, PlainTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, PdfDocumentTextExtractor>();
builder.Services.AddSingleton<ITextExtractor, DocxTextExtractor>();
builder.Services.AddSingleton<ITextExtractorResolver, TextExtractorResolver>();
builder.Services.AddSingleton<IChunkingService, ParagraphAwareChunkingService>();

builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DocumentIngestionDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseHttpMetrics();
app.MapMetrics();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter.WriteJson });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.Run();
