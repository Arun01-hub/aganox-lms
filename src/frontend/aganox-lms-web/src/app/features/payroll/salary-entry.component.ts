import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { LanguageService } from '../../core/language.service';
import {
  PayrollEmployeeResult,
  PayrollRun,
  PayrollService,
  SalaryEntryEmployee,
  SalaryEntryLine
} from '../../core/payroll.service';

@Component({
  selector: 'app-salary-entry',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, DecimalPipe],
  template: `
    <div class="salary-entry-header card border-0 shadow-sm">
      <div class="card-body d-flex flex-wrap align-items-end gap-2">
            <div>
              <label class="form-label small mb-0">{{ 'payroll.salaryEntry.financialYear' | translate }}</label>
              <input type="number" class="form-control form-control-sm" style="width:6rem" [(ngModel)]="year" (change)="loadRun()" />
            </div>
            <div>
              <label class="form-label small mb-0">{{ 'common.month' | translate }}</label>
              <select class="form-select form-select-sm" style="width:8rem" [(ngModel)]="month" (change)="loadRun()">
                @for (m of months; track m.value) {
                  <option [ngValue]="m.value">{{ m.labelKey | translate }}</option>
                }
              </select>
            </div>
            <div>
              <label class="form-label small mb-0">{{ 'payroll.salaryEntry.group' | translate }}</label>
              <select class="form-select form-select-sm" style="min-width:10rem" [(ngModel)]="departmentId" (change)="onDepartmentChange()">
                <option value="">{{ 'common.all' | translate }}</option>
                @for (d of departments(); track d.id) {
                  <option [value]="d.id">{{ d.name }}</option>
                }
              </select>
            </div>
            <div class="ms-auto d-flex flex-wrap gap-2">
              @if (run()) {
                <button class="btn btn-sm btn-outline-secondary" type="button" (click)="printPayroll()" [disabled]="busy()">
                  {{ 'payroll.salaryEntry.printPayroll' | translate }}
                </button>
              }
              <button class="btn btn-sm btn-aganox" type="button" (click)="newEntry()">{{ 'payroll.salaryEntry.nextEmployee' | translate }}</button>
            </div>
          </div>
          @if (run()) {
            <div class="card-footer small text-muted py-2">
              {{ 'payroll.salaryEntry.runStatus' | translate }}:
              {{ ('payroll.status.' + run()!.status) | translate }}
              @if (!canEdit()) {
                · {{ 'payroll.salaryEntry.readOnly' | translate }}
              }
            </div>
          } @else if (runMissing()) {
            <div class="card-footer small text-warning py-2">{{ 'payroll.salaryEntry.noRun' | translate }}</div>
          }
        </div>

        @if (error()) { <div class="alert alert-danger py-2 mt-3">{{ error() }}</div> }
        @if (message()) { <div class="alert alert-success py-2 mt-3">{{ message() }}</div> }

        @if (entry()) {
          <div class="row g-3 mt-1">
            <div class="col-lg-4">
              <div class="gov-section h-100">
                <div class="gov-section-head">{{ 'payroll.salaryEntry.staffDetails' | translate }}</div>
                <div class="gov-section-body">
                  <div class="salary-entry-photo mb-3">{{ initials() }}</div>
                  <div class="mb-2">
                    <label class="form-label">{{ 'payroll.salaryEntry.staffCode' | translate }}</label>
                    <input class="form-control form-control-sm" [value]="entry()!.employeeCode" readonly />
                  </div>
                  <div class="mb-2">
                    <label class="form-label">{{ 'payroll.salaryEntry.staffName' | translate }}</label>
                    <select class="form-select form-select-sm" [(ngModel)]="employeeId" (change)="loadEntry()">
                      @for (e of staffOptions(); track e.employeeId) {
                        <option [value]="e.employeeId">{{ e.employeeName }} ({{ e.employeeCode }})</option>
                      }
                    </select>
                  </div>
                  <div class="d-flex flex-wrap gap-2 mt-3">
                    <button class="btn btn-sm btn-outline-secondary" type="button" (click)="printIndividual()" [disabled]="!run()">
                      {{ 'payroll.salaryEntry.printIndividual' | translate }}
                    </button>
                    <button class="btn btn-sm btn-outline-secondary" type="button" (click)="printPayroll()" [disabled]="!run()">
                      {{ 'payroll.salaryEntry.printAll' | translate }}
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <div class="col-lg-4">
              <div class="gov-section h-100">
                <div class="gov-section-head">{{ 'payroll.salaryEntry.earnings' | translate }}</div>
                <div class="gov-section-body salary-entry-fields">
                  @for (line of earningLines(); track line.componentCode) {
                    <div class="salary-entry-field-row">
                      <label [for]="'e-' + line.componentCode">{{ componentLabel(line) }}</label>
                      <input
                        [id]="'e-' + line.componentCode"
                        type="number"
                        step="0.01"
                        class="form-control form-control-sm text-end"
                        [(ngModel)]="line.amount"
                        (ngModelChange)="onLineChanged()"
                        [disabled]="!canEdit()"
                      />
                    </div>
                  }
                  <div class="salary-entry-total-row">
                    <span>{{ 'payroll.salaryEntry.totalEarnings' | translate }}</span>
                    <strong>{{ totalEarnings() | number:'1.2-2' }}</strong>
                  </div>
                </div>
              </div>
            </div>

            <div class="col-lg-4">
              <div class="gov-section h-100">
                <div class="gov-section-head">{{ 'payroll.salaryEntry.deductions' | translate }}</div>
                <div class="gov-section-body salary-entry-fields">
                  @for (line of deductionLines(); track line.componentCode) {
                    <div class="salary-entry-field-row">
                      <label [for]="'d-' + line.componentCode">
                        {{ componentLabel(line) }}
                        @if (line.ratePercent != null && line.componentCode === 'PF') {
                          <span class="text-muted small">({{ line.ratePercent | number:'1.0-2' }}%)</span>
                        }
                      </label>
                      <input
                        [id]="'d-' + line.componentCode"
                        type="number"
                        step="0.01"
                        class="form-control form-control-sm text-end"
                        [(ngModel)]="line.amount"
                        (ngModelChange)="onLineChanged()"
                        [disabled]="!canEdit()"
                      />
                    </div>
                  }
                  <div class="salary-entry-total-row">
                    <span>{{ 'payroll.salaryEntry.totalDeductions' | translate }}</span>
                    <strong>{{ totalDeductions() | number:'1.2-2' }}</strong>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="salary-entry-net card border-0 shadow-sm mt-3">
            <div class="card-body d-flex flex-wrap align-items-center gap-3">
              <div>
                <div class="small text-muted">{{ 'payroll.salaryEntry.netPay' | translate }}</div>
                <div class="salary-entry-net-value">{{ netPay() | number:'1.2-2' }}</div>
              </div>
              @if (entry()!.isManuallyAdjusted) {
                <span class="badge text-bg-warning">{{ 'payroll.salaryEntry.manualAdjusted' | translate }}</span>
              }
              <div class="ms-auto d-flex flex-wrap gap-2">
                <button class="btn btn-sm btn-aganox" type="button" (click)="save()" [disabled]="!canEdit() || busy()">{{ 'common.save' | translate }}</button>
                <button class="btn btn-sm btn-outline-danger" type="button" (click)="resetEntry()" [disabled]="!canEdit() || busy()">{{ 'common.delete' | translate }}</button>
                <button class="btn btn-sm btn-outline-secondary" type="button" (click)="recalcPf()" [disabled]="!canEdit()">{{ 'payroll.salaryEntry.pf' | translate }}</button>
                <a class="btn btn-sm btn-outline-secondary" routerLink="/payroll/runs">{{ 'common.close' | translate }}</a>
              </div>
            </div>
          </div>
        }
  `
})
export class SalaryEntryComponent implements OnInit {
  private readonly payroll = inject(PayrollService);
  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);

  readonly run = signal<PayrollRun | null>(null);
  readonly contextCanEdit = signal(true);
  readonly contextComponents = signal<{ code: string; nameEn: string; nameTa: string }[]>([]);
  readonly departments = signal<{ id: string; name: string; code: string }[]>([]);
  readonly staffResults = signal<PayrollEmployeeResult[]>([]);
  readonly staffOptions = signal<{ employeeId: string; employeeName: string; employeeCode: string }[]>([]);
  readonly entry = signal<SalaryEntryEmployee | null>(null);
  readonly lineDraft = signal<SalaryEntryLine[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  readonly runMissing = signal(false);

  year = new Date().getFullYear();
  month = new Date().getMonth() + 1;
  departmentId = '';
  employeeId = '';

  readonly months = [
    { value: 1, labelKey: 'months.1' }, { value: 2, labelKey: 'months.2' }, { value: 3, labelKey: 'months.3' },
    { value: 4, labelKey: 'months.4' }, { value: 5, labelKey: 'months.5' }, { value: 6, labelKey: 'months.6' },
    { value: 7, labelKey: 'months.7' }, { value: 8, labelKey: 'months.8' }, { value: 9, labelKey: 'months.9' },
    { value: 10, labelKey: 'months.10' }, { value: 11, labelKey: 'months.11' }, { value: 12, labelKey: 'months.12' }
  ];

  readonly canEdit = computed(() => this.contextCanEdit() && this.run()?.status !== 'Locked' && this.run()?.status !== 'Approved');
  readonly earningLines = computed(() =>
    this.lineDraft().filter((x) => x.category === 'Earning' && !['LOP', 'ARREAR'].includes(x.componentCode)));
  readonly deductionLines = computed(() =>
    this.lineDraft().filter((x) => x.category === 'Deduction' && x.componentCode !== 'LOP'));
  readonly totalEarnings = computed(() => this.earningLines().reduce((s, x) => s + (x.amount || 0), 0));
  readonly totalDeductions = computed(() => this.deductionLines().reduce((s, x) => s + (x.amount || 0), 0));
  readonly netPay = computed(() => this.totalEarnings() - this.totalDeductions());
  readonly initials = computed(() => {
    const name = this.entry()?.employeeName ?? '';
    return name.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase() ?? '').join('') || '?';
  });

  ngOnInit(): void {
    this.loadRun();
  }

  componentLabel(line: SalaryEntryLine): string {
    const comp = this.contextComponents().find((x) => x.code === line.componentCode);
    if (this.language.currentLang() === 'ta' && comp?.nameTa) {
      return comp.nameTa;
    }
    return comp?.nameEn ?? line.componentName;
  }

  loadRun(): void {
    this.error.set('');
    this.message.set('');
    this.runMissing.set(false);
    this.payroll.getRuns(this.year).subscribe({
      next: (runs) => {
        const match = runs.find((r) => r.month === this.month);
        if (!match) {
          this.run.set(null);
          this.entry.set(null);
          this.runMissing.set(true);
          return;
        }
        this.run.set(match);
        this.loadContextAndStaff(match.id);
      },
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  private loadContextAndStaff(runId: string): void {
    forkJoin({
      context: this.payroll.getSalaryEntryContext(runId),
      results: this.payroll.getRunResults(runId)
    }).subscribe({
      next: ({ context, results }) => {
        this.contextCanEdit.set(context.canEdit);
        this.departments.set(context.departments);
        this.contextComponents.set([
          ...context.earnings.map((c) => ({ code: c.code, nameEn: c.nameEn, nameTa: c.nameTa })),
          ...context.deductions.map((c) => ({ code: c.code, nameEn: c.nameEn, nameTa: c.nameTa }))
        ]);
        this.staffResults.set(results);
        this.updateStaffOptions(results);
        if (this.staffOptions().length) {
          this.employeeId = this.staffOptions()[0].employeeId;
          this.loadEntry();
        } else {
          this.entry.set(null);
          this.error.set(this.translate.instant('payroll.salaryEntry.noResults'));
        }
      },
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  onDepartmentChange(): void {
    this.updateStaffOptions(this.staffResults());
    if (this.staffOptions().length) {
      this.employeeId = this.staffOptions()[0].employeeId;
      this.loadEntry();
    } else {
      this.entry.set(null);
    }
  }

  private updateStaffOptions(results: PayrollEmployeeResult[]): void {
    const filtered = this.departmentId
      ? results.filter((r) => r.departmentId === this.departmentId)
      : results;
    this.staffOptions.set(
      filtered.map((r) => ({
        employeeId: r.employeeId,
        employeeName: r.employeeName,
        employeeCode: r.employeeCode
      }))
    );
  }

  loadEntry(): void {
    const run = this.run();
    if (!run || !this.employeeId) return;
    this.payroll.getSalaryEntry(run.id, this.departmentId || undefined, this.employeeId).subscribe({
      next: (entry) => {
        this.entry.set(entry);
        this.lineDraft.set(entry.lines.map((l) => ({ ...l })));
      },
      error: () => this.error.set(this.translate.instant('payroll.salaryEntry.loadFailed'))
    });
  }

  onLineChanged(): void {
    // totals are computed signals
  }

  save(): void {
    const run = this.run();
    const entry = this.entry();
    if (!run || !entry || !this.canEdit()) return;
    this.busy.set(true);
    const lines = this.lineDraft().map((l) => ({ componentCode: l.componentCode, amount: l.amount || 0 }));
    this.payroll.saveSalaryEntry(run.id, entry.employeeId, lines).subscribe({
      next: (saved) => {
        this.busy.set(false);
        this.entry.set(saved);
        this.lineDraft.set(saved.lines.map((l) => ({ ...l })));
        this.message.set(this.translate.instant('payroll.salaryEntry.saved'));
        this.refreshRunTotals(run.id);
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  resetEntry(): void {
    const run = this.run();
    const entry = this.entry();
    if (!run || !entry || !confirm(this.translate.instant('payroll.salaryEntry.resetConfirm'))) return;
    this.busy.set(true);
    this.payroll.resetSalaryEntry(run.id, entry.employeeId).subscribe({
      next: (saved) => {
        this.busy.set(false);
        this.entry.set(saved);
        this.lineDraft.set(saved.lines.map((l) => ({ ...l })));
        this.message.set(this.translate.instant('payroll.salaryEntry.resetDone'));
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  recalcPf(): void {
    const basic = this.lineDraft().find((x) => x.componentCode === 'BASIC')?.amount ?? 0;
    const pf = this.lineDraft().find((x) => x.componentCode === 'PF');
    if (!pf || pf.ratePercent == null) return;
    pf.amount = Math.round(basic * (pf.ratePercent / 100) * 100) / 100;
    this.lineDraft.set([...this.lineDraft()]);
  }

  newEntry(): void {
    const results = this.staffResults();
    if (!results.length) return;
    const idx = results.findIndex((r) => r.employeeId === this.employeeId);
    const next = results[(idx + 1) % results.length];
    this.employeeId = next.employeeId;
    this.loadEntry();
  }

  printIndividual(): void {
    const run = this.run();
    const entry = this.entry();
    if (!run || !entry) return;
    this.payroll.exportPayslipPdf(run.id, entry.employeeId).subscribe({
      next: (blob) => this.downloadBlob(blob, `payslip-${entry.employeeCode}.pdf`),
      error: () => this.error.set(this.translate.instant('payroll.salaryEntry.printFailed'))
    });
  }

  printPayroll(): void {
    const run = this.run();
    if (!run) return;
    this.payroll.exportPayrollReport(run.id, 'register', 'pdf').subscribe({
      next: (blob) => this.downloadBlob(blob, `payroll-${run.year}-${run.month}.pdf`),
      error: () => this.error.set(this.translate.instant('payroll.salaryEntry.printFailed'))
    });
  }

  private refreshRunTotals(runId: string): void {
    this.payroll.getRun(runId).subscribe((r) => this.run.set(r));
  }

  private downloadBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}
