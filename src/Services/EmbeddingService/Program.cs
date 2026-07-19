using Common.Extensions;
using Common.Reliability;
using Confluent.Kafka;
using Consumers;
using HostedServices;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;
using Prometheus;
using Repositories;
using Serilog;
using Serilog.Formatting.Compact;
using Services;
using Services.Embedding;
using Services.VectorStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, cfg) => cfg
    .ReadFrom.Services(services)
    .Enrich.WithProperty("Service", "EmbeddingService")
    .WriteTo.Console(new CompactJsonFormatter()));

builder.AddSharedObservability();
builder.AddSharedRedis();

var postgresHealthSettings = builder.Configuration.GetSection(PostgresSettings.SectionName).Get<PostgresSettings>() ?? new PostgresSettings();
var kafkaHealthSettings = builder.Configuration.GetSection(KafkaConsumerSettings.SectionName).Get<KafkaConsumerSettings>() ?? new KafkaConsumerSettings();
var qdrantHealthOptions = builder.Configuration.GetSection(QdrantOptions.SectionName).Get<QdrantOptions>() ?? new QdrantOptions();
var ollamaHealthOptions = builder.Configuration.GetSection(OllamaOptions.SectionName).Get<OllamaOptions>() ?? new OllamaOptions();
var redisHealthSettings = builder.Configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();

builder.Services.AddHealthChecks()
    .AddNpgSql(postgresHealthSettings.ConnectionString, name: "postgres")
    .AddKafka(config =>
    {
        config.BootstrapServers = kafkaHealthSettings.BootstrapServers;
    }, name: "kafka")
    .AddUrlGroup(new Uri($"http://{qdrantHealthOptions.Endpoint}:{qdrantHealthOptions.RestPort}/healthz"), name: "qdrant")
    .AddUrlGroup(new Uri(ollamaHealthOptions.Endpoint), name: "ollama")
    .AddRedis(redisHealthSettings.ConnectionString, name: "redis");

builder.Services.Configure<KafkaConsumerSettings>(builder.Configuration.GetSection(KafkaConsumerSettings.SectionName));
builder.Services.AddScoped<IChunksCreatedConsumer, ChunksCreatedConsumer>();
builder.Services.AddHostedService<KafkaConsumerHostedService>();

builder.Services.Configure<PostgresSettings>(builder.Configuration.GetSection(PostgresSettings.SectionName));
builder.Services.AddDbContext<EmbeddingReadDbContext>((serviceProvider, options) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<PostgresSettings>>().Value;
    options.UseNpgsql(settings.ConnectionString);
});
builder.Services.AddScoped<IDocumentChunkReadRepository, DocumentChunkReadRepository>();
builder.Services.AddScoped<IDocumentMetadataStatusRepository, DocumentMetadataStatusRepository>();

builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection(QdrantOptions.SectionName));
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpClient<IEmbeddingGenerator, OllamaEmbeddingGenerator>();
builder.Services.AddHostedService<OllamaModelInitializer>();

builder.Services.AddScoped<IEmbeddingProcessingService, EmbeddingProcessingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
    await vectorStore.EnsureCollectionAsync();
}

app.UseHttpMetrics();
app.MapMetrics();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter.WriteJson });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.Run();
