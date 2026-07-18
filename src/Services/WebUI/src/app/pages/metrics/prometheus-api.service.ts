import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PrometheusSample {
  labels: Record<string, string>;
  value: number;
}

export interface PrometheusSeries {
  labels: Record<string, string>;
  points: Array<{ timestamp: number; value: number }>;
}

interface PrometheusApiResponse {
  status: string;
  data: {
    resultType: string;
    result: Array<{
      metric: Record<string, string>;
      value?: [number, string];
      values?: Array<[number, string]>;
    }>;
  };
}

@Injectable({ providedIn: 'root' })
export class PrometheusApiService {
  private readonly http = inject(HttpClient);

  /** Instant query — current value(s) of an expression. */
  query(expr: string): Observable<PrometheusSample[]> {
    const params = new URLSearchParams({ query: expr });
    return this.http
      .get<PrometheusApiResponse>(`${environment.prometheusUrl}/api/v1/query?${params}`)
      .pipe(
        map(res =>
          res.data.result.map(r => ({
            labels: r.metric,
            value: r.value ? Number(r.value[1]) : NaN
          }))
        )
      );
  }

  /** Range query — a time series per matching label set over [start, end]. */
  queryRange(expr: string, rangeSeconds: number, stepSeconds: number): Observable<PrometheusSeries[]> {
    const end = Math.floor(Date.now() / 1000);
    const start = end - rangeSeconds;
    const params = new URLSearchParams({
      query: expr,
      start: start.toString(),
      end: end.toString(),
      step: stepSeconds.toString()
    });
    return this.http
      .get<PrometheusApiResponse>(`${environment.prometheusUrl}/api/v1/query_range?${params}`)
      .pipe(
        map(res =>
          res.data.result.map(r => ({
            labels: r.metric,
            points: (r.values ?? []).map(([timestamp, value]) => ({ timestamp, value: Number(value) }))
          }))
        )
      );
  }
}
