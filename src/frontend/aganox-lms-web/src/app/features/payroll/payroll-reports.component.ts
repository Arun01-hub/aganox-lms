import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PayrollRun, PayrollService } from '../../core/payroll.service';

@Component({
  selector: 'app-payroll-reports',
  standalone: true,
  imports: [FormsModule, TranslatePipe, DecimalPipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">{{ 'payroll.reportsTitle' | translate }}</h1>
    </div>
    @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }

    <div class="row g-2 mb-3">
      <div class="col-md-4">
        <label class="form-label">{{ 'payroll.selectRun' | translate }}</label>
        <select class="form-select form-select-sm" [(ngModel)]="runId" (change)="loadReport()">
          @for (r of runs(); track r.id) {
            <option [value]="r.id">{{ r.year }}-{{ r.month.toString().padStart(2,'0') }} ({{ r.status }})</option>
          }
        </select>
      </div>
      <div class="col-md-3">
        <label class="form-label">{{ 'reports.reportType' | translate }}</label>
        <select class="form-select form-select-sm" [(ngModel)]="reportType" (change)="loadReport()">
          <option value="register">{{ 'payroll.reportRegister' | translate }}</option>
          <option value="components">{{ 'payroll.reportComponents' | translate }}</option>
          <option value="lop">{{ 'payroll.reportLop' | translate }}</option>
        </select>
      </div>
    </div>

    <div class="gov-section">
      <div class="gov-section-head">{{ 'reports.results' | translate }}</div>
      <div class="gov-section-body table-responsive">
        @if (rows().length) {
          <table class="table table-sm">
            <thead><tr>@for (key of columns(); track key) { <th>{{ key }}</th> }</tr></thead>
            <tbody>
              @for (row of rows(); track $index) {
                <tr>@for (key of columns(); track key) { <td>{{ row[key] }}</td> }</tr>
              }
            </tbody>
          </table>
        } @else {
          <p class="text-muted mb-0">{{ 'common.noData' | translate }}</p>
        }
      </div>
    </div>
  `
})
export class PayrollReportsComponent implements OnInit {
  private readonly payroll = inject(PayrollService);
  private readonly translate = inject(TranslateService);

  readonly runs = signal<PayrollRun[]>([]);
  readonly rows = signal<Record<string, unknown>[]>([]);
  readonly columns = signal<string[]>([]);
  readonly error = signal('');

  runId = '';
  reportType = 'register';

  ngOnInit(): void {
    this.payroll.getRuns().subscribe({
      next: (items) => {
        this.runs.set(items);
        if (items.length) {
          this.runId = items[0].id;
          this.loadReport();
        }
      },
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  loadReport(): void {
    if (!this.runId) return;
    this.payroll.getReport(this.runId, this.reportType).subscribe({
      next: (res) => {
        this.rows.set(res.rows as Record<string, unknown>[]);
        this.columns.set(res.rows.length ? Object.keys(res.rows[0]) : []);
      },
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }
}
