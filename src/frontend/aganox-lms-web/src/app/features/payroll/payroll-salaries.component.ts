import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { OrganizationService } from '../../core/organization.service';
import { EmployeeSalaryRevision, PayrollService } from '../../core/payroll.service';
import { SalaryBreakdownPanelComponent } from './salary-breakdown-panel.component';

@Component({
  selector: 'app-payroll-salaries',
  standalone: true,
  imports: [FormsModule, TranslatePipe, DecimalPipe, SalaryBreakdownPanelComponent],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">{{ 'payroll.salariesTitle' | translate }}</h1>
    </div>
    @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }
    @if (message()) { <div class="alert alert-success py-2">{{ message() }}</div> }

    <div class="row g-3">
      <div class="col-md-4">
        <label class="form-label">{{ 'payroll.selectEmployee' | translate }}</label>
        <select class="form-select form-select-sm" [(ngModel)]="employeeId" (change)="loadRevisions()">
          @for (e of employees(); track e.id) {
            <option [value]="e.id">{{ e.fullName }} ({{ e.employeeCode }})</option>
          }
        </select>
      </div>
    </div>

    <div class="gov-section mt-3">
      <div class="gov-section-head">{{ 'payroll.salaryRevisions' | translate }}</div>
      <div class="gov-section-body table-responsive">
        <table class="table table-sm">
          <thead>
            <tr>
              <th>{{ 'payroll.effectiveFrom' | translate }}</th>
              <th>{{ 'payroll.basicPay' | translate }}</th>
              <th>{{ 'payroll.breakdown.overrides' | translate }}</th>
              <th>{{ 'common.status' | translate }}</th>
            </tr>
          </thead>
          <tbody>
            @for (r of revisions(); track r.id) {
              <tr>
                <td>{{ r.effectiveFrom }} @if (r.effectiveTo) { – {{ r.effectiveTo }} }</td>
                <td>{{ r.basicPay | number:'1.2-2' }}</td>
                <td>
                  @if (r.overrides.length) {
                    @for (o of r.overrides; track o.salaryComponentId) {
                      <div><code>{{ o.componentCode }}</code>: {{ o.fixedAmount | number:'1.2-2' }}</div>
                    }
                  } @else {
                    <span class="text-muted">{{ 'payroll.breakdown.noOverrides' | translate }}</span>
                  }
                </td>
                <td>{{ r.remarks ?? ('common.dash' | translate) }}</td>
              </tr>
            } @empty {
              <tr><td colspan="4" class="text-muted text-center">{{ 'common.noData' | translate }}</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>

    <div class="gov-section mt-3">
      <div class="gov-section-head">{{ 'payroll.addRevision' | translate }}</div>
      <div class="gov-section-body">
        <div class="row g-2 mb-3">
          <div class="col-md-4">
            <label class="form-label">{{ 'attendance.remarks' | translate }}</label>
            <input class="form-control form-control-sm" [(ngModel)]="remarks" />
          </div>
        </div>
        <app-salary-breakdown-panel #salaryBreakdown [initialEffectiveFrom]="effectiveFromDefault" />
        <div class="mt-3">
          <button class="btn btn-sm btn-aganox" type="button" (click)="save()" [disabled]="busy()">{{ 'common.save' | translate }}</button>
        </div>
      </div>
    </div>
  `
})
export class PayrollSalariesComponent implements OnInit {
  private readonly payroll = inject(PayrollService);
  private readonly org = inject(OrganizationService);
  private readonly translate = inject(TranslateService);

  @ViewChild('salaryBreakdown') salaryBreakdown?: SalaryBreakdownPanelComponent;

  readonly employees = signal<{ id: string; fullName: string; employeeCode: string }[]>([]);
  readonly revisions = signal<EmployeeSalaryRevision[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  employeeId = '';
  remarks = '';
  effectiveFromDefault = new Date().toISOString().slice(0, 10);

  ngOnInit(): void {
    this.org.getEmployees({ page: 1, pageSize: 200 }).subscribe({
      next: (res) => {
        this.employees.set(res.items.map(e => ({ id: e.id, fullName: e.fullName, employeeCode: e.employeeCode })));
        if (res.items.length) {
          this.employeeId = res.items[0].id;
          this.loadRevisions();
        }
      },
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  loadRevisions(): void {
    if (!this.employeeId) return;
    this.payroll.getSalaryRevisions(this.employeeId).subscribe({
      next: (items) => this.revisions.set(items),
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  save(): void {
    const panel = this.salaryBreakdown;
    if (!this.employeeId || !panel || !panel.isValid()) return;
    this.busy.set(true);
    this.payroll.createSalaryRevision({
      employeeId: this.employeeId,
      effectiveFrom: panel.effectiveFrom,
      basicPay: panel.basicPay,
      remarks: this.remarks || undefined,
      overrides: panel.getOverridesPayload()
    }).subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set(this.translate.instant('payroll.revisionSaved'));
        this.remarks = '';
        this.loadRevisions();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }
}
