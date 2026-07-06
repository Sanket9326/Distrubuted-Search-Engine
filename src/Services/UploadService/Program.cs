using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(KafkaSettings.SectionName));
builder.Services.AddSingleton<IKafkaProducer, KafkaProducerService>();
builder.Services.AddHostedService<KafkaTopicInitializer>();

builder.Services.AddControllers();
builder.Services.AddSingleton<IFileHandlerService, FileHandlerService>();
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
