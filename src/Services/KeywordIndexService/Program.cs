using Common.Extensions;
using Common.Reliability;
using Confluent.Kafka;
using Consumers;
using HostedServices;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;
using Prometheus;
using Repositories;
using Serilog;
using Serilog.Formatting.Compact;
using Services;
using Services.Indexing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, cfg) => cfg
    .ReadFrom.Services(services)
    .Enrich.WithProperty("Service", "KeywordIndexService")
    .WriteTo.Console(new CompactJsonFormatter()));

builder.AddSharedObservability();
builder.AddSharedRedis();

var postgresHealthSettings = builder.Configuration.GetSection(PostgresSettings.SectionName).Get<PostgresSettings>() ?? new PostgresSettings();
var kafkaHealthSettings = builder.Configuration.GetSection(KafkaConsumerSettings.SectionName).Get<KafkaConsumerSettings>() ?? new KafkaConsumerSettings();
var redisHealthSettings = builder.Configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();

builder.Services.AddHealthChecks()
    .AddNpgSql(postgresHealthSettings.ConnectionString, name: "postgres")
    .AddKafka(config =>
    {
        config.BootstrapServers = kafkaHealthSettings.BootstrapServers;
    }, name: "kafka")
    .AddRedis(redisHealthSettings.ConnectionString, name: "redis");

builder.Services.Configure<KafkaConsumerSettings>(builder.Configuration.GetSection(KafkaConsumerSettings.SectionName));
builder.Services.AddScoped<IKeywordIndexingConsumer, KeywordIndexingConsumer>();
builder.Services.AddHostedService<KafkaConsumerHostedService>();

builder.Services.Configure<PostgresSettings>(builder.Configuration.GetSection(PostgresSettings.SectionName));
builder.Services.AddDbContext<KeywordIndexReadDbContext>((serviceProvider, options) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<PostgresSettings>>().Value;
    options.UseNpgsql(settings.ConnectionString);
});
builder.Services.AddDbContext<KeywordIndexDbContext>((serviceProvider, options) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<PostgresSettings>>().Value;
    options.UseNpgsql(settings.ConnectionString);
});
builder.Services.AddScoped<IDocumentChunkReadRepository, DocumentChunkReadRepository>();
builder.Services.AddScoped<IIndexRepository, IndexRepository>();
builder.Services.AddScoped<IKeywordIndexStatusRepository, KeywordIndexStatusRepository>();

builder.Services.AddSingleton<ITokenizer, RegexTokenizer>();
builder.Services.AddSingleton<IStopWordFilter, EnglishStopWordFilter>();
builder.Services.AddSingleton<IStemmer, PorterStemmerAdapter>();
builder.Services.AddSingleton<IKeywordAnalyzer, KeywordAnalyzer>();

builder.Services.AddScoped<IKeywordIndexProcessingService, KeywordIndexProcessingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KeywordIndexDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseHttpMetrics();
app.MapMetrics();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter.WriteJson });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.Run();
