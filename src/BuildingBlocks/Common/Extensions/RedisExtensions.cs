using Common.Reliability;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Common.Extensions;

/// <summary>
/// Shared Redis + retry-queue wiring for the two services that schedule retries
/// (DocumentIngestionService, EmbeddingService) and the ReliabilityService worker that
/// drains them, matching the <see cref="ObservabilityExtensions"/> convention of
/// cross-cutting code shared via an <see cref="IHostApplicationBuilder"/> extension.
/// </summary>
public static class RedisExtensions
{
    public static void AddSharedRedis(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection(RedisSettings.SectionName));
        builder.Services.Configure<RetrySettings>(builder.Configuration.GetSection(RetrySettings.SectionName));

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            return ConnectionMultiplexer.Connect(settings.ConnectionString);
        });

        builder.Services.AddSingleton<IRetryQueue, RedisRetryQueue>();
    }
}
