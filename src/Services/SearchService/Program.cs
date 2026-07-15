using Infrastructure;
using Services;
using Services.Embedding;
using Services.Llm;
using Services.Prompting;
using Services.ReRanking;
using Services.VectorSearch;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection(QdrantOptions.SectionName));
builder.Services.AddSingleton<IVectorSearchStore, QdrantVectorSearchStore>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
