import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface ReportResult<T> {
  rows: T[];
  title: string;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getTrends(from: string, to: string) {
    return this.http.get<ReportResult<{ period: string; requestCount: number; totalDays: number }>>(
      `${this.base}/reports/trends`,
      { params: new HttpParams().set('from', from).set('to', to) }
    );
  }

  getBalances(year: number) {
    return this.http.get<ReportResult<Record<string, unknown>>>(
      `${this.base}/reports/balances`,
      { params: new HttpParams().set('year', year) }
    );
  }

  getMonthly(year: number, month: number) {
    return this.http.get<ReportResult<Record<string, unknown>>>(
      `${this.base}/reports/monthly`,
      { params: new HttpParams().set('year', year).set('month', month) }
    );
  }

  exportReport(name: string, format: 'csv' | 'xlsx' | 'pdf', params: Record<string, string | number>) {
    let httpParams = new HttpParams().set('format', format);
    for (const [key, value] of Object.entries(params)) {
      httpParams = httpParams.set(key, value);
    }
    return this.http.get(`${this.base}/reports/${name}/export`, {
      params: httpParams,
      responseType: 'blob'
    });
  }

  downloadBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}
