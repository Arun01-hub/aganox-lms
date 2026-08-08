import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { OrganizationService } from '../../core/organization.service';
import { PayrollArrear, PayrollService } from '../../core/payroll.service';

@Component({
  selector: 'app-payroll-arrears',
  standalone: true,
  imports: [FormsModule, TranslatePipe, DecimalPipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ 'payroll.arrearsTitle' | translate }}</h1>
    </div>
    @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }
    @if (message()) { <div class="alert alert-success py-2">{{ message() }}</div> }

    <div class="gov-section mb-3">
      <div class="gov-section-head">{{ 'payroll.addArrear' | translate }}</div>
      <div class="gov-section-body row g-2">
        <div class="col-md-3">
          <label class="form-label">{{ 'payroll.selectEmployee' | translate }}</label>
          <select class="form-select form-select-sm" [(ngModel)]="form.employeeId">
            @for (e of employees(); track e.id) { <option [value]="e.id">{{ e.fullName }}</option> }
          </select>
        </div>
        <div class="col-md-2"><label class="form-label">{{ 'common.year' | translate }}</label><input type="number" class="form-control form-control-sm" [(ngModel)]="form.sourceYear" /></div>
        <div class="col-md-2"><label class="form-label">{{ 'common.month' | translate }}</label><input type="number" class="form-control form-control-sm" [(ngModel)]="form.sourceMonth" /></div>
        <div class="col-md-2"><label class="form-label">{{ 'payroll.amount' | translate }}</label><input type="number" class="form-control form-control-sm" [(ngModel)]="form.amount" /></div>
        <div class="col-md-3"><label class="form-label">{{ 'attendance.remarks' | translate }}</label><input class="form-control form-control-sm" [(ngModel)]="form.reason" /></div>
        <div class="col-12"><button class="btn btn-sm btn-aganox" type="button" (click)="save()" [disabled]="busy()">{{ 'common.save' | translate }}</button></div>
      </div>
    </div>

    <div class="gov-section">
      <div class="gov-section-head">{{ 'payroll.arrearsList' | translate }}</div>
      <div class="gov-section-body table-responsive">
        <table class="table table-sm">
          <thead><tr><th>{{ 'leave.employee' | translate }}</th><th>{{ 'payroll.sourcePeriod' | translate }}</th><th>{{ 'payroll.amount' | translate }}</th><th>{{ 'common.status' | translate }}</th></tr></thead>
          <tbody>
            @for (a of arrears(); track a.id) {
              <tr>
                <td>{{ a.employeeName }}</td>
                <td>{{ a.sourceYear }}-{{ a.sourceMonth.toString().padStart(2,'0') }}</td>
                <td>{{ a.amount | number:'1.2-2' }}</td>
                <td>{{ a.status }}</td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="text-muted text-center">{{ 'common.noData' | translate }}</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class PayrollArrearsComponent implements OnInit {
  private readonly payroll = inject(PayrollService);
  private readonly org = inject(OrganizationService);
  private readonly translate = inject(TranslateService);

  readonly employees = signal<{ id: string; fullName: string }[]>([]);
  readonly arrears = signal<PayrollArrear[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  form = { employeeId: '', sourceYear: new Date().getFullYear(), sourceMonth: 1, amount: 0, reason: '' };

  ngOnInit(): void {
    this.org.getEmployees({ page: 1, pageSize: 200 }).subscribe({
      next: (res) => {
        this.employees.set(res.items.map(e => ({ id: e.id, fullName: e.fullName })));
        if (res.items.length) this.form.employeeId = res.items[0].id;
      }
    });
    this.loadArrears();
  }

  loadArrears(): void {
    this.payroll.getArrears().subscribe({
      next: (items) => this.arrears.set(items),
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  save(): void {
    if (!this.form.employeeId || !this.form.reason) return;
    this.busy.set(true);
    this.payroll.createArrear({
      employeeId: this.form.employeeId,
      sourceYear: this.form.sourceYear,
      sourceMonth: this.form.sourceMonth,
      amount: this.form.amount,
      reason: this.form.reason
    }).subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set(this.translate.instant('payroll.arrearSaved'));
        this.loadArrears();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }
}
