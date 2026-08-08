import { Component, inject, OnInit, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { PayrollEmployeeResult, PayrollRun, PayrollService } from '../../core/payroll.service';

@Component({
  selector: 'app-payroll-runs',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, DecimalPipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">{{ 'payroll.runsTitle' | translate }}</h1>
      <a class="btn btn-sm btn-outline-secondary ms-auto" routerLink="/payroll/salary-entry">{{ 'nav.salaryEntry' | translate }}</a>
      <div class="d-flex gap-2">
        <input type="number" class="form-control form-control-sm" style="width:5rem" [(ngModel)]="newYear" />
        <input type="number" class="form-control form-control-sm" style="width:4rem" [(ngModel)]="newMonth" />
        <button class="btn btn-sm btn-aganox" type="button" (click)="createRun()" [disabled]="busy()">{{ 'payroll.createRun' | translate }}</button>
      </div>
    </div>
    @if (message()) { <div class="alert alert-success py-2">{{ message() }}</div> }
    @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }

    <div class="row g-3">
      <div class="col-lg-4">
        <div class="gov-section">
          <div class="gov-section-head">{{ 'payroll.runsList' | translate }}</div>
          <div class="gov-section-body list-group list-group-flush">
            @for (r of runs(); track r.id) {
              <button type="button" class="list-group-item list-group-item-action" [class.active]="selected()?.id === r.id" (click)="selectRun(r)">
                {{ formatPeriod(r.year, r.month) }} · {{ ('payroll.status.' + r.status) | translate }}
                <div class="small text-muted">{{ r.employeeCount }} · {{ r.totalNetPay | number:'1.2-2' }}</div>
              </button>
            } @empty {
              <div class="text-muted p-2">{{ 'common.noData' | translate }}</div>
            }
          </div>
        </div>
      </div>
      <div class="col-lg-8">
        @if (selected()) {
          <div class="gov-section">
            <div class="gov-section-head d-flex align-items-center gap-2">
              <span>{{ 'payroll.runDetail' | translate:{ period: formatPeriod(selected()!.year, selected()!.month) } }}</span>
              <span class="badge text-bg-secondary ms-auto">{{ ('payroll.status.' + selected()!.status) | translate }}</span>
            </div>
            <div class="gov-section-body">
              <div class="d-flex flex-wrap gap-2 mb-3">
                @if (canProcess() && selected()!.status !== 'Locked') {
                  <button class="btn btn-sm btn-aganox" type="button" (click)="calculate()" [disabled]="busy()">
                    {{ (selected()!.status === 'Draft' ? 'payroll.calculate' : 'payroll.recalculate') | translate }}
                  </button>
                }
                @if (canProcess() && selected()!.status === 'Calculated') {
                  <button class="btn btn-sm btn-outline-secondary" type="button" (click)="submit()" [disabled]="busy()">{{ 'payroll.submit' | translate }}</button>
                }
                @if (canApprove() && (selected()!.status === 'Calculated' || selected()!.status === 'PendingApproval')) {
                  <button class="btn btn-sm btn-outline-primary" type="button" (click)="approve()" [disabled]="busy()">{{ 'payroll.approve' | translate }}</button>
                }
                @if (canLock() && selected()!.status === 'Approved') {
                  <button class="btn btn-sm btn-outline-danger" type="button" (click)="lock()" [disabled]="busy()">{{ 'payroll.lock' | translate }}</button>
                }
                @if (selected()!.status === 'Locked') {
                  <a class="btn btn-sm btn-outline-secondary" [routerLink]="['/payroll/payslips', selected()!.id]">{{ 'payroll.viewPayslips' | translate }}</a>
                }
              </div>
              <div class="table-responsive">
                <table class="table table-sm">
                  <thead><tr><th>{{ 'leave.employee' | translate }}</th><th>{{ 'payroll.gross' | translate }}</th><th>{{ 'payroll.deductions' | translate }}</th><th>{{ 'payroll.net' | translate }}</th><th>{{ 'attendance.lopDays' | translate }}</th></tr></thead>
                  <tbody>
                    @for (e of results(); track e.id) {
                      <tr>
                        <td>{{ e.employeeName }}</td>
                        <td>{{ e.grossPay | number:'1.2-2' }}</td>
                        <td>{{ e.totalDeductions | number:'1.2-2' }}</td>
                        <td>{{ e.netPay | number:'1.2-2' }}</td>
                        <td>{{ e.lopDays }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class PayrollRunsComponent implements OnInit {
  private readonly payroll = inject(PayrollService);
  private readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);

  readonly runs = signal<PayrollRun[]>([]);
  readonly selected = signal<PayrollRun | null>(null);
  readonly results = signal<PayrollEmployeeResult[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  newYear = new Date().getFullYear();
  newMonth = new Date().getMonth() + 1;

  ngOnInit(): void { this.loadRuns(); }

  canProcess(): boolean { return this.auth.hasPermission('Payroll.Process'); }
  canApprove(): boolean { return this.auth.hasPermission('Payroll.Approve'); }
  canLock(): boolean { return this.auth.hasPermission('Payroll.Lock'); }

  formatPeriod(year: number, month: number): string {
    return `${year}-${month.toString().padStart(2, '0')}`;
  }

  loadRuns(): void {
    this.payroll.getRuns(this.newYear).subscribe({
      next: (items) => this.runs.set(items),
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  selectRun(run: PayrollRun): void {
    this.selected.set(run);
    this.payroll.getRunResults(run.id).subscribe({
      next: (items) => this.results.set(items),
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  createRun(): void {
    this.busy.set(true);
    this.error.set('');
    this.payroll.createRun(this.newYear, this.newMonth).subscribe({
      next: (run) => {
        this.payroll.calculateRun(run.id, false).subscribe({
          next: (calculated) => {
            this.busy.set(false);
            this.message.set(this.translate.instant('payroll.runProcessed'));
            this.loadRuns();
            this.selectRun(calculated);
          },
          error: (err) => {
            this.busy.set(false);
            this.message.set(this.translate.instant('payroll.runReset'));
            this.loadRuns();
            this.selectRun(run);
            this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.actionFailed'));
          }
        });
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  calculate(): void {
    const run = this.selected();
    if (!run) return;
    this.act(() => this.payroll.calculateRun(run.id, run.status !== 'Draft'));
  }

  submit(): void {
    const run = this.selected();
    if (!run) return;
    this.act(() => this.payroll.submitRun(run.id));
  }

  approve(): void {
    const run = this.selected();
    if (!run) return;
    this.act(() => this.payroll.approveRun(run.id));
  }

  lock(): void {
    const run = this.selected();
    if (!run) return;
    this.act(() => this.payroll.lockRun(run.id));
  }

  private act(call: () => Observable<PayrollRun>): void {
    this.busy.set(true);
    call().subscribe({
      next: (run) => {
        this.busy.set(false);
        this.message.set(this.translate.instant('payroll.actionDone'));
        this.loadRuns();
        this.selectRun(run);
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.actionFailed'));
      }
    });
  }
}
