using Common.Extensions;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Persistence;
using Prometheus;
using Serilog;
using Serilog.Formatting.Compact;
using Services;
using Services.Embedding;
using Services.Indexing;
using Services.KeywordSearch;
using Services.Llm;
using Services.Prompting;
using Services.ReRanking;
using Services.VectorSearch;

// Test line
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, cfg) => cfg
    .ReadFrom.Services(services)
    .Enrich.WithProperty("Service", "SearchService")
    .WriteTo.Console(new CompactJsonFormatter()));

builder.AddSharedObservability();

var webUiOrigin = builder.Configuration["Cors:WebUiOrigin"] ?? "http://localhost:4200";
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebUi", policy => policy
        .WithOrigins(webUiOrigin)
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var qdrantOptions = builder.Configuration.GetSection(QdrantOptions.SectionName).Get<QdrantOptions>() ?? new QdrantOptions();
var ollamaOptions = builder.Configuration.GetSection(OllamaOptions.SectionName).Get<OllamaOptions>() ?? new OllamaOptions();
var teiOptions = builder.Configuration.GetSection(TeiOptions.SectionName).Get<TeiOptions>() ?? new TeiOptions();
var geminiHealthOptions = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>() ?? new GeminiOptions();
var postgresOptions = builder.Configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>() ?? new PostgresOptions();

builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri($"http://{qdrantOptions.Endpoint}:{qdrantOptions.RestPort}/healthz"), name: "qdrant")
    .AddUrlGroup(new Uri(ollamaOptions.Endpoint), name: "ollama")
    .AddUrlGroup(new Uri($"{teiOptions.Endpoint}/health"), name: "reranker")
    .AddNpgSql(postgresOptions.ConnectionString, name: "postgres")
    .AddCheck("gemini", () => string.IsNullOrWhiteSpace(geminiHealthOptions.ApiKey)
        ? HealthCheckResult.Degraded("Gemini API key not configured")
        : HealthCheckResult.Healthy());

builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection(QdrantOptions.SectionName));
builder.Services.AddSingleton<IVectorSearchStore, QdrantVectorSearchStore>();

builder.Services.Configure<PostgresOptions>(builder.Configuration.GetSection(PostgresOptions.SectionName));
builder.Services.AddDbContext<KeywordIndexReadDbContext>((serviceProvider, options) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<PostgresOptions>>().Value;
    options.UseNpgsql(settings.ConnectionString);
});
builder.Services.AddSingleton<ITokenizer, RegexTokenizer>();
builder.Services.AddSingleton<IStopWordFilter, EnglishStopWordFilter>();
builder.Services.AddSingleton<IStemmer, PorterStemmerAdapter>();
builder.Services.AddSingleton<IKeywordAnalyzer, KeywordAnalyzer>();
builder.Services.AddScoped<IKeywordSearchStore, Bm25KeywordSearchStore>();

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.AddHttpClient<IEmbeddingGenerator, OllamaEmbeddingGenerator>();

builder.Services.Configure<SearchOptions>(builder.Configuration.GetSection(SearchOptions.SectionName));

builder.Services.Configure<TeiOptions>(builder.Configuration.GetSection(TeiOptions.SectionName));
builder.Services.AddHttpClient<IReRanker, TeiReRanker>();

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddHttpClient<ILlmClient, GeminiLlmClient>();
builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();

builder.Services.AddScoped<ISearchProcessingService, SearchProcessingService>();
builder.Services.AddScoped<IAnswerService, AnswerService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("WebUi");

app.UseAuthorization();

app.UseHttpMetrics();
app.MapMetrics();
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter.WriteJson });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.MapControllers();

app.Run();
