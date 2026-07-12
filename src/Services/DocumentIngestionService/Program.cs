using Common.FileValidation;
using Common.Utilities;
using Consumers;
using HostedServices;
using Infrastructure;
using Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;
using Repositories;
using Services;
using Services.Chunking;
using Services.TextExtraction;
using Storage;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

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

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DocumentIngestionDbContext>();
    await dbContext.Database.MigrateAsync();
}

host.Run();