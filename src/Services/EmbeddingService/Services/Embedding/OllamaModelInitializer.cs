using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Services.Embedding;

public sealed class OllamaModelInitializer : IHostedService
{
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaModelInitializer> _logger;

    public OllamaModelInitializer(IOptions<OllamaOptions> options, ILogger<OllamaModelInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.Endpoint),
            Timeout = TimeSpan.FromMinutes(15)
        };

        _logger.LogInformation("Ensuring Ollama model '{Model}' is pulled (this may take a while on first run)...", _options.Model);

        var response = await httpClient.PostAsJsonAsync(
            "api/pull",
            new { model = _options.Model, stream = false },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Ollama model '{Model}' is ready.", _options.Model);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
