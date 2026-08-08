import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
import { ReportService } from '../../core/report.service';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [RouterLink, FormsModule, TranslatePipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ 'reports.title' | translate }}</h1>
      <a routerLink="/" class="btn btn-sm btn-outline-secondary ms-auto">{{ 'common.dashboard' | translate }}</a>
    </div>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }

    <div class="card border-0 shadow-sm mb-3">
      <div class="card-body row g-3 align-items-end">
        <div class="col-md-3">
          <label class="form-label">{{ 'reports.report' | translate }}</label>
          <select class="form-select" [(ngModel)]="reportName">
            <option value="employee-leave">{{ 'reports.employeeLeave' | translate }}</option>
            <option value="monthly">{{ 'reports.monthlySummary' | translate }}</option>
            <option value="yearly">{{ 'reports.yearlySummary' | translate }}</option>
            <option value="balances">{{ 'reports.leaveBalances' | translate }}</option>
            <option value="trends">{{ 'reports.trends' | translate }}</option>
          </select>
        </div>
        <div class="col-md-2"><label class="form-label">{{ 'common.year' | translate }}</label><input class="form-control" type="number" [(ngModel)]="year" /></div>
        <div class="col-md-2"><label class="form-label">{{ 'common.month' | translate }}</label><input class="form-control" type="number" [(ngModel)]="month" min="1" max="12" /></div>
        <div class="col-md-2"><label class="form-label">{{ 'common.from' | translate }}</label><input class="form-control" type="date" [(ngModel)]="fromDate" /></div>
        <div class="col-md-2"><label class="form-label">{{ 'common.to' | translate }}</label><input class="form-control" type="date" [(ngModel)]="toDate" /></div>
        <div class="col-md-1">
          <button class="btn btn-aganox w-100" type="button" (click)="run()" [disabled]="loading()">{{ 'reports.run' | translate }}</button>
        </div>
      </div>
    </div>

    @if (title()) {
      <div class="card border-0 shadow-sm mb-3">
        <div class="card-header bg-white d-flex justify-content-between align-items-center">
          <span class="fw-semibold">{{ title() }}</span>
          @if (auth.hasPermission('Reports.Export')) {
            <div class="btn-group btn-group-sm">
              <button class="btn btn-outline-secondary" type="button" (click)="export('csv')">CSV</button>
              <button class="btn btn-outline-secondary" type="button" (click)="export('xlsx')">Excel</button>
              <button class="btn btn-outline-secondary" type="button" (click)="export('pdf')">PDF</button>
            </div>
          }
        </div>
        <div class="table-responsive">
          <table class="table mb-0">
            <thead><tr>@for (h of headers(); track h) { <th>{{ h }}</th> }</tr></thead>
            <tbody>
              @for (row of rows(); track $index) {
                <tr>@for (cell of row; track $index) { <td>{{ cell }}</td> }</tr>
              } @empty {
                <tr><td [attr.colspan]="headers().length || 1" class="text-muted text-center py-3">{{ 'common.noData' | translate }}</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }
  `
})
export class ReportsComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly reports = inject(ReportService);
  private readonly translate = inject(TranslateService);

  reportName = 'trends';
  year = new Date().getFullYear();
  month = new Date().getMonth() + 1;
  fromDate = `${this.year}-01-01`;
  toDate = `${this.year}-12-31`;

  readonly title = signal('');
  readonly headers = signal<string[]>([]);
  readonly rows = signal<string[][]>([]);
  readonly error = signal('');
  readonly loading = signal(false);

  ngOnInit(): void {
    this.run();
  }

  run(): void {
    this.error.set('');
    this.loading.set(true);

    if (this.reportName === 'trends') {
      this.reports.getTrends(this.fromDate, this.toDate).subscribe({
        next: (r) => this.showReport(
          r.title,
          [
            this.translate.instant('reports.colPeriod'),
            this.translate.instant('reports.colRequests'),
            this.translate.instant('reports.colTotalDays')
          ],
          r.rows.map((x) => [x.period, String(x.requestCount), String(x.totalDays)])
        ),
        error: () => this.fail(),
        complete: () => this.loading.set(false)
      });
      return;
    }

    if (this.reportName === 'balances') {
      this.reports.getBalances(this.year).subscribe({
        next: (r) => this.showReport(
          r.title,
          [
            this.translate.instant('reports.colCode'),
            this.translate.instant('reports.colEmployee'),
            this.translate.instant('reports.colDepartment'),
            this.translate.instant('reports.colLeaveType'),
            this.translate.instant('reports.colOpening'),
            this.translate.instant('reports.colConsumed'),
            this.translate.instant('reports.colRemaining')
          ],
          r.rows.map((x) => [
            String(x['employeeCode'] ?? ''), String(x['employeeName'] ?? ''), String(x['departmentName'] ?? ''),
            String(x['leaveTypeCode'] ?? ''), String(x['openingBalance'] ?? ''), String(x['consumed'] ?? ''), String(x['remaining'] ?? '')
          ])
        ),
        error: () => this.fail(),
        complete: () => this.loading.set(false)
      });
      return;
    }

    if (this.reportName === 'monthly') {
      this.reports.getMonthly(this.year, this.month).subscribe({
        next: (r) => this.showReport(
          r.title,
          [
            this.translate.instant('reports.colDepartment'),
            this.translate.instant('reports.colLeaveType'),
            this.translate.instant('reports.colRequests'),
            this.translate.instant('reports.colDays')
          ],
          r.rows.map((x) => [String(x['departmentName'] ?? ''), String(x['leaveTypeName'] ?? ''), String(x['requestCount'] ?? ''), String(x['totalDays'] ?? '')])
        ),
        error: () => this.fail(),
        complete: () => this.loading.set(false)
      });
      return;
    }

    this.loading.set(false);
    this.error.set(this.translate.instant('reports.previewHint'));
  }

  export(format: 'csv' | 'xlsx' | 'pdf'): void {
    const params: Record<string, string | number> = { year: this.year };
    if (this.reportName === 'monthly') params['month'] = this.month;
    if (this.fromDate) params['from'] = this.fromDate;
    if (this.toDate) params['to'] = this.toDate;

    this.reports.exportReport(this.reportName, format, params).subscribe({
      next: (blob) => this.reports.downloadBlob(blob, `${this.reportName}.${format}`),
      error: () => this.error.set(this.translate.instant('reports.exportFailed'))
    });
  }

  private showReport(title: string, headers: string[], rows: string[][]): void {
    this.title.set(title);
    this.headers.set(headers);
    this.rows.set(rows);
  }

  private fail(): void {
    this.error.set(this.translate.instant('reports.loadFailed'));
    this.loading.set(false);
  }
}
