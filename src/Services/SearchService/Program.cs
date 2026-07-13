using Infrastructure;
using Services;
using Services.Embedding;
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

builder.Services.AddScoped<ISearchProcessingService, SearchProcessingService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
