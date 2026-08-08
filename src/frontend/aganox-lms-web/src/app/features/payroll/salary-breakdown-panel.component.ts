import { DecimalPipe } from '@angular/common';
import { Component, inject, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { debounceTime, Subject } from 'rxjs';
import { PayrollService, SalaryPreview } from '../../core/payroll.service';

export interface SalaryOverrideRow {
  salaryComponentId: string;
  componentCode: string;
  componentName: string;
  category: string;
  overrideAmount: number | null;
}

@Component({
  selector: 'app-salary-breakdown-panel',
  standalone: true,
  imports: [FormsModule, TranslatePipe, DecimalPipe],
  template: `
    <div class="gov-section">
      <div class="gov-section-head">{{ 'payroll.breakdown.title' | translate }}</div>
      <div class="gov-section-body">
        <div class="row g-2 mb-3">
          <div class="col-md-3">
            <label class="form-label">{{ 'payroll.basicPay' | translate }}</label>
            <input type="number" step="0.01" class="form-control form-control-sm" [(ngModel)]="basicPay" (ngModelChange)="onInputChanged()" />
          </div>
          <div class="col-md-3">
            <label class="form-label">{{ 'payroll.effectiveFrom' | translate }}</label>
            <input type="date" class="form-control form-control-sm" [(ngModel)]="effectiveFrom" (ngModelChange)="onInputChanged()" />
          </div>
        </div>

        <div class="mb-3">
          <div class="small fw-semibold mb-2">{{ 'payroll.breakdown.overrides' | translate }}</div>
          <div class="text-muted small mb-2">{{ 'payroll.breakdown.overrideHint' | translate }}</div>
          <div class="table-responsive">
            <table class="table table-sm align-middle mb-0">
              <thead>
                <tr>
                  <th>{{ 'common.code' | translate }}</th>
                  <th>{{ 'common.name' | translate }}</th>
                  <th>{{ 'payroll.category' | translate }}</th>
                  <th class="text-end">{{ 'payroll.breakdown.overrideAmount' | translate }}</th>
                </tr>
              </thead>
              <tbody>
                @for (row of overrideRows(); track row.salaryComponentId) {
                  <tr>
                    <td>{{ row.componentCode }}</td>
                    <td>{{ row.componentName }}</td>
                    <td>{{ row.category }}</td>
                    <td class="text-end" style="max-width:10rem">
                      <input
                        type="number"
                        step="0.01"
                        class="form-control form-control-sm text-end"
                        [ngModel]="row.overrideAmount"
                        (ngModelChange)="setOverride(row.salaryComponentId, $event)"
                        placeholder="—"
                      />
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>

        @if (previewError()) {
          <div class="alert alert-warning py-2 small">{{ previewError() }}</div>
        } @else if (preview()) {
          <div class="row g-3">
            <div class="col-md-6">
              <div class="small fw-semibold mb-1">{{ 'payroll.breakdown.preview' | translate }}</div>
              <table class="table table-sm mb-0">
                <tbody>
                  @for (line of earningLines(); track line.componentCode) {
                    <tr>
                      <td>{{ line.componentName }}</td>
                      <td class="text-end">{{ line.amount | number:'1.2-2' }}</td>
                    </tr>
                  }
                  <tr class="table-light"><td>{{ 'payroll.gross' | translate }}</td><td class="text-end fw-semibold">{{ preview()!.grossPay | number:'1.2-2' }}</td></tr>
                </tbody>
              </table>
            </div>
            <div class="col-md-6">
              <div class="small fw-semibold mb-1">{{ 'payroll.deductions' | translate }}</div>
              <table class="table table-sm mb-0">
                <tbody>
                  @for (line of deductionLines(); track line.componentCode) {
                    <tr>
                      <td>{{ line.componentName }}</td>
                      <td class="text-end">{{ line.amount | number:'1.2-2' }}</td>
                    </tr>
                  }
                  <tr class="table-light"><td>{{ 'payroll.net' | translate }}</td><td class="text-end fw-semibold">{{ preview()!.netPay | number:'1.2-2' }}</td></tr>
                </tbody>
              </table>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class SalaryBreakdownPanelComponent implements OnInit {
  private readonly payroll = inject(PayrollService);
  private readonly translate = inject(TranslateService);
  private readonly previewTrigger = new Subject<void>();

  readonly initialBasicPay = input(0);
  readonly initialEffectiveFrom = input(new Date().toISOString().slice(0, 10));

  readonly overridesChanged = output<{ salaryComponentId: string; componentCode: string; fixedAmount: number }[]>();

  readonly overrideRows = signal<SalaryOverrideRow[]>([]);
  readonly preview = signal<SalaryPreview | null>(null);
  readonly previewError = signal('');

  basicPay = 0;
  effectiveFrom = new Date().toISOString().slice(0, 10);

  readonly earningLines = signal<SalaryPreview['lines']>([]);
  readonly deductionLines = signal<SalaryPreview['lines']>([]);

  ngOnInit(): void {
    this.basicPay = this.initialBasicPay();
    this.effectiveFrom = this.initialEffectiveFrom() || this.effectiveFrom;
    this.payroll.getComponents(true).subscribe({
      next: (components) => {
        this.overrideRows.set(
          components
            .filter((c) => c.code !== 'BASIC')
            .map((c) => ({
              salaryComponentId: c.id,
              componentCode: c.code,
              componentName: c.nameEn,
              category: c.category,
              overrideAmount: null
            }))
        );
        this.onInputChanged();
      },
      error: () => this.previewError.set(this.translate.instant('payroll.breakdown.previewFailed'))
    });

    this.previewTrigger.pipe(debounceTime(300)).subscribe(() => this.loadPreview());
  }

  onInputChanged(): void {
    this.previewTrigger.next();
  }

  setOverride(componentId: string, value: number | null): void {
    this.overrideRows.update((rows) =>
      rows.map((row) =>
        row.salaryComponentId === componentId
          ? { ...row, overrideAmount: value === null || value === undefined || Number.isNaN(value) ? null : value }
          : row
      )
    );
    this.onInputChanged();
  }

  getOverridesPayload(): { salaryComponentId: string; componentCode: string; fixedAmount: number }[] {
    return this.overrideRows()
      .filter((row) => row.overrideAmount !== null && row.overrideAmount !== undefined)
      .map((row) => ({
        salaryComponentId: row.salaryComponentId,
        componentCode: row.componentCode,
        fixedAmount: row.overrideAmount ?? 0
      }));
  }

  isValid(): boolean {
    return this.basicPay > 0 && !!this.effectiveFrom;
  }

  private loadPreview(): void {
    if (this.basicPay <= 0 || !this.effectiveFrom) {
      this.preview.set(null);
      this.earningLines.set([]);
      this.deductionLines.set([]);
      return;
    }

    const overrides = this.getOverridesPayload();
    this.overridesChanged.emit(overrides);
    this.payroll.previewSalary({
      basicPay: this.basicPay,
      effectiveFrom: this.effectiveFrom,
      overrides: overrides.length ? overrides : undefined
    }).subscribe({
      next: (result) => {
        this.preview.set(result);
        this.previewError.set('');
        this.earningLines.set(result.lines.filter((x) => x.category === 'Earning'));
        this.deductionLines.set(result.lines.filter((x) => x.category === 'Deduction'));
      },
      error: () => this.previewError.set(this.translate.instant('payroll.breakdown.previewFailed'))
    });
  }
}
