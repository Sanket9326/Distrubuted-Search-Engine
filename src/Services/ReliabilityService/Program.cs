using Common.Extensions;
using Common.Reliability;
using HostedServices;
using Kafka;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, cfg) => cfg
    .ReadFrom.Services(services)
    .Enrich.WithProperty("Service", "ReliabilityService")
    .WriteTo.Console(new CompactJsonFormatter()));

builder.AddSharedObservability();
builder.AddSharedRedis();

var kafkaHealthSettings = builder.Configuration.GetSection(KafkaSettings.SectionName).Get<KafkaSettings>() ?? new KafkaSettings();
var redisHealthSettings = builder.Configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();

builder.Services.AddHealthChecks()
    .AddKafka(config =>
    {
        config.BootstrapServers = kafkaHealthSettings.BootstrapServers;
    }, name: "kafka")
    .AddRedis(redisHealthSettings.ConnectionString, name: "redis");

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(KafkaSettings.SectionName));
builder.Services.AddSingleton<IRawKafkaPublisher, RawKafkaPublisher>();
builder.Services.AddHostedService<DlqTopicInitializer>();
builder.Services.AddHostedService<RetryQueuePollingHostedService>();

var app = builder.Build();

app.UseHttpMetrics();
app.MapMetrics();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter.WriteJson });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.Run();
