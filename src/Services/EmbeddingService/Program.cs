using Consumers;
using HostedServices;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;
using Repositories;
using Services;
using Services.Embedding;
using Services.VectorStorage;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

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

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
    await vectorStore.EnsureCollectionAsync();
}

host.Run();
