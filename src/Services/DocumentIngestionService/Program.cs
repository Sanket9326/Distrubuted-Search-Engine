using Consumers;
using HostedServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;
using Repositories;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<KafkaConsumerSettings>(builder.Configuration.GetSection(KafkaConsumerSettings.SectionName));
builder.Services.AddScoped<IDocumentUploadedConsumer, DocumentUploadedConsumer>();
builder.Services.AddHostedService<KafkaConsumerHostedService>();

builder.Services.Configure<PostgresSettings>(builder.Configuration.GetSection(PostgresSettings.SectionName));
builder.Services.AddDbContext<DocumentIngestionDbContext>((serviceProvider, options) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<PostgresSettings>>().Value;
    options.UseNpgsql(settings.ConnectionString);
});
builder.Services.AddScoped<IDocumentMetadataRepository, DocumentMetadataRepository>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DocumentIngestionDbContext>();
    await dbContext.Database.MigrateAsync();
}

host.Run();