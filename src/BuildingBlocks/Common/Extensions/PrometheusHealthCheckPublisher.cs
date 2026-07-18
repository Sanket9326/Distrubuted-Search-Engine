using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace Common.Extensions;

/// <summary>
/// Bridges the built-in health check system into Prometheus: every periodic health check run
/// (see <see cref="ObservabilityExtensions"/>) updates a gauge per check name, so health status
/// can be charted in Grafana from the same datasource as the rest of the metrics.
/// </summary>
public sealed class PrometheusHealthCheckPublisher : IHealthCheckPublisher
{
    private static readonly Gauge HealthGauge = Metrics.CreateGauge(
        "health_check_status",
        "1 = healthy, 0.5 = degraded, 0 = unhealthy",
        "name");

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var (name, entry) in report.Entries)
        {
            HealthGauge.WithLabels(name).Set(entry.Status switch
            {
                HealthStatus.Healthy => 1,
                HealthStatus.Degraded => 0.5,
                _ => 0
            });
        }

        return Task.CompletedTask;
    }
}
