using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Common.Extensions;

/// <summary>
/// Cross-cutting observability wiring shared by every service, regardless of whether it's a
/// Web API or a background worker (<see cref="IHostApplicationBuilder"/> is implemented by both
/// <c>WebApplicationBuilder</c> and <c>HostApplicationBuilder</c>). Dependency-specific health
/// checks (Postgres, Kafka, Qdrant, ...) stay in each service's own Program.cs, matching this
/// repo's existing convention of not sharing dependency-specific integration code across services.
/// </summary>
public static class ObservabilityExtensions
{
    public static void AddSharedObservability(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IHealthCheckPublisher, PrometheusHealthCheckPublisher>();
        builder.Services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Period = TimeSpan.FromSeconds(15);
        });
    }
}
