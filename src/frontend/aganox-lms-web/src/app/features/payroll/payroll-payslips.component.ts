import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Payslip, PayrollService } from '../../core/payroll.service';

@Component({
  selector: 'app-payroll-payslips',
  standalone: true,
  imports: [TranslatePipe, DecimalPipe],
  template: `
    <header class="mb-4">
      <h1 class="h4 mb-0">{{ 'payroll.payslipsTitle' | translate }}</h1>
    </header>
    @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }

    @if (!runId) {
      <p class="text-muted">{{ 'payroll.selectLockedRun' | translate }}</p>
    } @else if (payslip()) {
      <div class="gov-section">
        <div class="gov-section-head">{{ formatPeriod(payslip()!.year, payslip()!.month) }} · {{ payslip()!.employeeName }}</div>
        <div class="gov-section-body">
          <dl class="row small mb-3">
            <dt class="col-sm-3">{{ 'common.code' | translate }}</dt><dd class="col-sm-9">{{ payslip()!.employeeCode }}</dd>
            <dt class="col-sm-3">{{ 'employees.department' | translate }}</dt><dd class="col-sm-9">{{ payslip()!.departmentName }}</dd>
            <dt class="col-sm-3">{{ 'payroll.basicPay' | translate }}</dt><dd class="col-sm-9">{{ payslip()!.basicPay | number:'1.2-2' }}</dd>
            <dt class="col-sm-3">{{ 'payroll.gross' | translate }}</dt><dd class="col-sm-9">{{ payslip()!.grossPay | number:'1.2-2' }}</dd>
            <dt class="col-sm-3">{{ 'payroll.net' | translate }}</dt><dd class="col-sm-9 fw-semibold">{{ payslip()!.netPay | number:'1.2-2' }}</dd>
          </dl>
          <table class="table table-sm">
            <thead><tr><th>{{ 'common.code' | translate }}</th><th>{{ 'common.name' | translate }}</th><th>{{ 'payroll.amount' | translate }}</th></tr></thead>
            <tbody>
              @for (l of payslip()!.lineItems; track l.componentCode + l.sortOrder) {
                <tr><td>{{ l.componentCode }}</td><td>{{ l.componentName }}</td><td>{{ l.amount | number:'1.2-2' }}</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }
  `
})
export class PayrollPayslipsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly payroll = inject(PayrollService);
  private readonly translate = inject(TranslateService);

  readonly payslip = signal<Payslip | null>(null);
  readonly error = signal('');
  runId = '';

  ngOnInit(): void {
    this.runId = this.route.snapshot.paramMap.get('runId') ?? '';
    if (!this.runId) return;
    this.payroll.getPayslip(this.runId).subscribe({
      next: (item) => this.payslip.set(item),
      error: () => this.error.set(this.translate.instant('payroll.payslipNotFound'))
    });
  }

  formatPeriod(year: number, month: number): string {
    return `${year}-${month.toString().padStart(2, '0')}`;
  }
}
