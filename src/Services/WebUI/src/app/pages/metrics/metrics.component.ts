import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { NgxEchartsDirective, provideEchartsCore } from 'ngx-echarts';
import type { EChartsCoreOption } from 'echarts/core';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { echarts } from './echarts-setup';
import { PrometheusApiService, PrometheusSeries } from './prometheus-api.service';

type HealthStatus = 'healthy' | 'degraded' | 'unhealthy' | 'unknown';

interface ServiceHealth {
  key: string;
  label: string;
  status: HealthStatus;
}

interface CounterTile {
  label: string;
  value: number;
  unit: string;
}

const REFRESH_INTERVAL_MS = 15_000;
const RANGE_SECONDS = 30 * 60;
const STEP_SECONDS = 15;

const SERVICES: Array<{ key: string; label: string }> = [
  { key: 'upload-service', label: 'Upload Service' },
  { key: 'document-ingestion-service', label: 'Document Ingestion' },
  { key: 'embedding-service', label: 'Embedding Service' },
  { key: 'search-service', label: 'Search Service' },
  { key: 'reliability-service', label: 'Reliability Service' }
];

@Component({
  selector: 'app-metrics',
  imports: [NgxEchartsDirective, DatePipe, DecimalPipe, MatCardModule, MatIconModule],
  providers: [provideEchartsCore({ echarts })],
  templateUrl: './metrics.component.html',
  styleUrl: './metrics.component.scss'
})
export class MetricsComponent implements OnInit, OnDestroy {
  private readonly prometheus = inject(PrometheusApiService);
  private timer?: ReturnType<typeof setInterval>;

  readonly serviceHealth = signal<ServiceHealth[]>(SERVICES.map(s => ({ ...s, status: 'unknown' })));
  readonly counters = signal<CounterTile[]>([]);
  readonly requestRateOption = signal<EChartsCoreOption | null>(null);
  readonly latencyOption = signal<EChartsCoreOption | null>(null);
  readonly cpuOption = signal<EChartsCoreOption | null>(null);
  readonly memoryOption = signal<EChartsCoreOption | null>(null);
  readonly retryQueueDepthOption = signal<EChartsCoreOption | null>(null);
  readonly lastUpdated = signal<Date | null>(null);

  ngOnInit(): void {
    this.refresh();
    this.timer = setInterval(() => this.refresh(), REFRESH_INTERVAL_MS);
  }

  ngOnDestroy(): void {
    if (this.timer) {
      clearInterval(this.timer);
    }
  }

  private refresh(): void {
    this.refreshHealth();
    this.refreshCounters();
    this.refreshRequestCharts();
    this.refreshContainerCharts();
    this.refreshReliabilityChart();
    this.lastUpdated.set(new Date());
  }

  private refreshHealth(): void {
    forkJoin(SERVICES.map(s => this.prometheus.query(`health_check_status{job="${s.key}"}`))).subscribe(results => {
      this.serviceHealth.set(
        SERVICES.map((s, i) => ({ ...s, status: this.aggregateHealth(results[i].map(r => r.value)) }))
      );
    });
  }

  private aggregateHealth(values: number[]): HealthStatus {
    if (values.length === 0) {
      return 'unknown';
    }
    const min = Math.min(...values);
    if (min >= 1) return 'healthy';
    if (min >= 0.5) return 'degraded';
    return 'unhealthy';
  }

  private refreshCounters(): void {
    const queries: Array<[string, string, string]> = [
      ['Documents Uploaded', 'rate(documents_uploaded_total[5m]) * 60', '/min'],
      ['Documents Ingested', 'rate(documents_ingested_total[5m]) * 60', '/min'],
      ['Chunks Embedded', 'rate(chunks_embedded_total[5m]) * 60', '/min'],
      ['RAG Answers Generated', 'rate(rag_answers_generated_total[5m]) * 60', '/min'],
      ['Retry Queue Depth', 'retry_queue_depth', ''],
      ['Retries Scheduled', 'sum(rate(retry_scheduled_total[5m])) * 60', '/min'],
      ['Dead-Lettered', 'sum(rate(retry_exhausted_total[5m])) * 60', '/min']
    ];

    forkJoin(queries.map(([, expr]) => this.prometheus.query(expr))).subscribe(results => {
      this.counters.set(
        queries.map(([label, , unit], i) => ({
          label,
          value: results[i][0]?.value ?? 0,
          unit
        }))
      );
    });
  }

  private refreshRequestCharts(): void {
    forkJoin(
      SERVICES.map(s =>
        this.prometheus.queryRange(
          `sum(rate(http_request_duration_seconds_count{job="${s.key}"}[1m]))`,
          RANGE_SECONDS,
          STEP_SECONDS
        )
      )
    ).subscribe(results => {
      const series = SERVICES.map((s, i) => ({ name: s.label, data: results[i] }));
      this.requestRateOption.set(this.buildLineChartOption(series, 'req/s'));
    });

    forkJoin(
      SERVICES.map(s =>
        this.prometheus.queryRange(
          `histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket{job="${s.key}"}[5m])) by (le))`,
          RANGE_SECONDS,
          STEP_SECONDS
        )
      )
    ).subscribe(results => {
      const series = SERVICES.map((s, i) => ({ name: s.label, data: results[i] }));
      this.latencyOption.set(this.buildLineChartOption(series, 'seconds (p95)'));
    });
  }

  private refreshContainerCharts(): void {
    this.prometheus
      .queryRange('sum(rate(container_cpu_usage_seconds_total{name!=""}[1m])) by (name) * 100', RANGE_SECONDS, STEP_SECONDS)
      .subscribe(results => {
        const series = results.map(r => ({ name: r.labels['name'] || 'unknown', data: [r] }));
        this.cpuOption.set(this.buildLineChartOption(series, '% CPU'));
      });

    this.prometheus
      .queryRange('sum(container_memory_usage_bytes{name!=""}) by (name)', RANGE_SECONDS, STEP_SECONDS)
      .subscribe(results => {
        const series = results.map(r => ({ name: r.labels['name'] || 'unknown', data: [r] }));
        this.memoryOption.set(this.buildLineChartOption(series, 'bytes'));
      });
  }

  private refreshReliabilityChart(): void {
    this.prometheus.queryRange('retry_queue_depth', RANGE_SECONDS, STEP_SECONDS).subscribe(results => {
      const series = [{ name: 'pending retries', data: results }];
      this.retryQueueDepthOption.set(this.buildLineChartOption(series, 'envelopes'));
    });
  }

  private buildLineChartOption(
    series: Array<{ name: string; data: PrometheusSeries[] }>,
    yAxisName: string
  ): EChartsCoreOption {
    return {
      tooltip: { trigger: 'axis' },
      legend: { top: 0, textStyle: { fontSize: 11 } },
      grid: { left: 48, right: 16, top: 36, bottom: 28 },
      xAxis: { type: 'time' },
      yAxis: { type: 'value', name: yAxisName, nameTextStyle: { fontSize: 11 } },
      series: series.flatMap(s =>
        s.data.map(d => ({
          name: s.name,
          type: 'line' as const,
          showSymbol: false,
          smooth: true,
          data: d.points.map(p => [p.timestamp * 1000, p.value])
        }))
      )
    };
  }
}
