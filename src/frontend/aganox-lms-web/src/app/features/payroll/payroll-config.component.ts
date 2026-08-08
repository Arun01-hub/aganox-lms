import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PayrollService, SalaryComponent, SalaryRule } from '../../core/payroll.service';

const PROTECTED_CODES = new Set(['BASIC', 'LOP']);

interface RuleFormModel {
  salaryComponentId: string;
  effectiveFrom: string;
  effectiveTo: string;
  priority: number;
  fixedAmount: number;
  ratePercent: number;
  expression: string;
  isActive: boolean;
}

interface ComponentFormModel {
  code: string;
  nameEn: string;
  nameTa: string;
  category: string;
  calculationType: string;
  baseComponentCode: string;
  sortOrder: number;
  isTaxable: boolean;
  includeInGross: boolean;
  isActive: boolean;
  ruleEffectiveFrom: string;
  ruleEffectiveTo: string;
  fixedAmount: number;
  ratePercent: number;
  expression: string;
}

@Component({
  selector: 'app-payroll-config',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ 'payroll.configTitle' | translate }}</h1>
    </div>
    @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }
    @if (message()) { <div class="alert alert-success py-2">{{ message() }}</div> }

    <ul class="nav nav-tabs mb-3">
      <li class="nav-item"><button class="nav-link" [class.active]="tab()==='components'" type="button" (click)="tab.set('components')">{{ 'payroll.components' | translate }}</button></li>
      <li class="nav-item"><button class="nav-link" [class.active]="tab()==='rules'" type="button" (click)="tab.set('rules'); loadRules()">{{ 'payroll.rules' | translate }}</button></li>
    </ul>

    @if (tab() === 'components') {
      <div class="gov-section mb-3">
        <div class="gov-section-head">{{ editingComponentId() ? ('payroll.editComponent' | translate) : ('payroll.addComponent' | translate) }}</div>
        <div class="gov-section-body row g-2">
          <div class="col-md-2">
            <label class="form-label">{{ 'common.code' | translate }}</label>
            <input class="form-control form-control-sm" [(ngModel)]="componentForm.code" [readonly]="!!editingComponentId() && isProtectedCode(componentForm.code)" />
          </div>
          <div class="col-md-3">
            <label class="form-label">{{ 'payroll.nameEn' | translate }}</label>
            <input class="form-control form-control-sm" [(ngModel)]="componentForm.nameEn" />
          </div>
          <div class="col-md-3">
            <label class="form-label">{{ 'payroll.nameTa' | translate }}</label>
            <input class="form-control form-control-sm" [(ngModel)]="componentForm.nameTa" />
          </div>
          <div class="col-md-2">
            <label class="form-label">{{ 'payroll.category' | translate }}</label>
            <select class="form-select form-select-sm" [(ngModel)]="componentForm.category">
              <option value="Earning">{{ 'payroll.earning' | translate }}</option>
              <option value="Deduction">{{ 'payroll.deduction' | translate }}</option>
            </select>
          </div>
          <div class="col-md-2">
            <label class="form-label">{{ 'payroll.calcType' | translate }}</label>
            <select class="form-select form-select-sm" [(ngModel)]="componentForm.calculationType">
              @for (type of calcTypes; track type) {
                <option [value]="type">{{ ('payroll.calcTypes.' + type) | translate }}</option>
              }
            </select>
          </div>
          @if (componentForm.calculationType === 'PercentOfComponent') {
            <div class="col-md-3">
              <label class="form-label">{{ 'payroll.baseComponent' | translate }}</label>
              <select class="form-select form-select-sm" [(ngModel)]="componentForm.baseComponentCode">
                <option value="">{{ 'payroll.selectComponent' | translate }}</option>
                @for (c of baseComponentOptions(); track c.id) {
                  <option [value]="c.code">{{ c.code }} — {{ c.nameEn }}</option>
                }
              </select>
            </div>
          }
          @if (componentForm.calculationType === 'Fixed') {
            <div class="col-md-2">
              <label class="form-label">{{ 'payroll.fixedAmount' | translate }}</label>
              <input type="number" step="0.01" class="form-control form-control-sm" [(ngModel)]="componentForm.fixedAmount" />
            </div>
          }
          @if (componentForm.calculationType === 'PercentOfBasic' || componentForm.calculationType === 'PercentOfGross' || componentForm.calculationType === 'PercentOfComponent') {
            <div class="col-md-2">
              <label class="form-label">{{ 'payroll.ratePercent' | translate }}</label>
              <input type="number" step="0.01" class="form-control form-control-sm" [(ngModel)]="componentForm.ratePercent" />
            </div>
          }
          @if (componentForm.calculationType === 'Expression') {
            <div class="col-md-4">
              <label class="form-label">{{ 'payroll.advancedExpression' | translate }}</label>
              <input class="form-control form-control-sm" [(ngModel)]="componentForm.expression" />
            </div>
          }
          <div class="col-md-2">
            <label class="form-label">{{ 'payroll.effectiveFrom' | translate }}</label>
            <input type="date" class="form-control form-control-sm" [(ngModel)]="componentForm.ruleEffectiveFrom" />
          </div>
          <div class="col-md-2">
            <label class="form-label">{{ 'payroll.sortOrder' | translate }}</label>
            <input type="number" class="form-control form-control-sm" [(ngModel)]="componentForm.sortOrder" />
          </div>
          <div class="col-md-4 d-flex align-items-end gap-3 flex-wrap">
            <div class="form-check"><input type="checkbox" class="form-check-input" [(ngModel)]="componentForm.includeInGross" id="incGross" /><label class="form-check-label" for="incGross">{{ 'payroll.includeInGross' | translate }}</label></div>
            <div class="form-check"><input type="checkbox" class="form-check-input" [(ngModel)]="componentForm.isTaxable" id="taxable" /><label class="form-check-label" for="taxable">{{ 'payroll.isTaxable' | translate }}</label></div>
            <div class="form-check"><input type="checkbox" class="form-check-input" [(ngModel)]="componentForm.isActive" id="compActive" /><label class="form-check-label" for="compActive">{{ 'common.active' | translate }}</label></div>
          </div>
          <div class="col-md-2 d-flex align-items-end gap-2">
            <button class="btn btn-sm btn-aganox" type="button" (click)="saveComponent()" [disabled]="busy()">{{ 'common.save' | translate }}</button>
            @if (editingComponentId()) {
              <button class="btn btn-sm btn-outline-secondary" type="button" (click)="resetComponentForm()">{{ 'common.cancel' | translate }}</button>
            }
          </div>
        </div>
      </div>

      <div class="table-responsive gov-section">
        <div class="gov-section-head">{{ 'payroll.components' | translate }}</div>
        <div class="gov-section-body">
          <table class="table table-sm">
            <thead><tr><th>{{ 'common.code' | translate }}</th><th>{{ 'common.name' | translate }}</th><th>{{ 'payroll.category' | translate }}</th><th>{{ 'payroll.calcType' | translate }}</th><th>{{ 'payroll.parameters' | translate }}</th><th>{{ 'common.active' | translate }}</th><th></th></tr></thead>
            <tbody>
              @for (c of components(); track c.id) {
                <tr>
                  <td>{{ c.code }}</td>
                  <td>{{ c.nameEn }}</td>
                  <td>{{ c.category }}</td>
                  <td>{{ ('payroll.calcTypes.' + c.calculationType) | translate }}</td>
                  <td>{{ formatComponentValue(c) }}</td>
                  <td>{{ (c.isActive ? 'common.yes' : 'common.no') | translate }}</td>
                  <td class="text-end"><button class="btn btn-sm btn-outline-primary" type="button" (click)="editComponent(c)">{{ 'common.edit' | translate }}</button></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }

    @if (tab() === 'rules') {
      <div class="gov-section mb-3">
        <div class="gov-section-head">{{ editingRuleId() ? ('payroll.editRule' | translate) : ('payroll.addRule' | translate) }}</div>
        <div class="gov-section-body row g-2">
          <div class="col-md-3">
            <label class="form-label">{{ 'payroll.selectComponent' | translate }}</label>
            <select class="form-select form-select-sm" [(ngModel)]="ruleForm.salaryComponentId" (change)="onRuleComponentChange()">
              <option value="">{{ 'payroll.selectComponent' | translate }}</option>
              @for (c of components(); track c.id) {
                <option [value]="c.id">{{ c.code }} — {{ c.nameEn }}</option>
              }
            </select>
          </div>
          <div class="col-md-2">
            <label class="form-label">{{ 'payroll.effectiveFrom' | translate }}</label>
            <input type="date" class="form-control form-control-sm" [(ngModel)]="ruleForm.effectiveFrom" />
          </div>
          <div class="col-md-2">
            <label class="form-label">{{ 'payroll.effectiveTo' | translate }}</label>
            <input type="date" class="form-control form-control-sm" [(ngModel)]="ruleForm.effectiveTo" />
          </div>
          <div class="col-md-1">
            <label class="form-label">{{ 'payroll.priority' | translate }}</label>
            <input type="number" class="form-control form-control-sm" [(ngModel)]="ruleForm.priority" />
          </div>
          @if (selectedRuleComponent()?.calculationType === 'Fixed') {
            <div class="col-md-2">
              <label class="form-label">{{ 'payroll.fixedAmount' | translate }}</label>
              <input type="number" step="0.01" class="form-control form-control-sm" [(ngModel)]="ruleForm.fixedAmount" />
            </div>
          }
          @if (selectedRuleComponent()?.calculationType === 'PercentOfBasic' || selectedRuleComponent()?.calculationType === 'PercentOfGross' || selectedRuleComponent()?.calculationType === 'PercentOfComponent') {
            <div class="col-md-2">
              <label class="form-label">{{ 'payroll.ratePercent' | translate }}</label>
              <input type="number" step="0.01" class="form-control form-control-sm" [(ngModel)]="ruleForm.ratePercent" />
            </div>
          }
          @if (selectedRuleComponent()?.calculationType === 'Expression') {
            <div class="col-md-4">
              <label class="form-label">{{ 'payroll.advancedExpression' | translate }}</label>
              <input class="form-control form-control-sm" [(ngModel)]="ruleForm.expression" />
            </div>
          }
          <div class="col-md-1 d-flex align-items-end">
            <div class="form-check"><input type="checkbox" class="form-check-input" [(ngModel)]="ruleForm.isActive" id="ruleActive" /><label class="form-check-label" for="ruleActive">{{ 'common.active' | translate }}</label></div>
          </div>
          <div class="col-md-2 d-flex align-items-end gap-2">
            <button class="btn btn-sm btn-aganox" type="button" (click)="saveRule()" [disabled]="busy()">{{ 'common.save' | translate }}</button>
            @if (editingRuleId()) {
              <button class="btn btn-sm btn-outline-secondary" type="button" (click)="resetRuleForm()">{{ 'common.cancel' | translate }}</button>
            }
          </div>
        </div>
      </div>

      <div class="table-responsive gov-section">
        <div class="gov-section-head">{{ 'payroll.rules' | translate }}</div>
        <div class="gov-section-body">
          <table class="table table-sm">
            <thead><tr><th>{{ 'common.code' | translate }}</th><th>{{ 'payroll.effectiveFrom' | translate }}</th><th>{{ 'payroll.effectiveTo' | translate }}</th><th>{{ 'payroll.priority' | translate }}</th><th>{{ 'payroll.parameters' | translate }}</th><th></th></tr></thead>
            <tbody>
              @for (r of rules(); track r.id) {
                <tr>
                  <td>{{ r.componentCode }}</td>
                  <td>{{ r.effectiveFrom }}</td>
                  <td>{{ r.effectiveTo ?? ('common.dash' | translate) }}</td>
                  <td>{{ r.priority }}</td>
                  <td>{{ formatRuleSummary(r) }}</td>
                  <td class="text-end"><button class="btn btn-sm btn-outline-primary" type="button" (click)="editRule(r)">{{ 'common.edit' | translate }}</button></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }
  `
})
export class PayrollConfigComponent implements OnInit {
  private readonly payroll = inject(PayrollService);
  private readonly translate = inject(TranslateService);

  readonly tab = signal<'components' | 'rules'>('components');
  readonly components = signal<SalaryComponent[]>([]);
  readonly rules = signal<SalaryRule[]>([]);
  readonly editingComponentId = signal<string | null>(null);
  readonly editingComponentRuleId = signal<string | null>(null);
  readonly editingRuleId = signal<string | null>(null);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  readonly calcTypes = ['Fixed', 'PercentOfBasic', 'PercentOfGross', 'PercentOfComponent', 'Expression'];
  readonly selectedRuleComponent = computed(() =>
    this.components().find((c) => c.id === this.ruleForm.salaryComponentId) ?? null);
  readonly baseComponentOptions = computed(() =>
    this.components().filter((c) => c.id !== this.editingComponentId() && c.code !== this.componentForm.code));

  componentForm: ComponentFormModel = this.defaultComponentForm();
  ruleForm: RuleFormModel = this.defaultRuleForm();

  ngOnInit(): void {
    this.loadComponents();
    this.loadRules();
  }

  loadComponents(): void {
    this.payroll.getComponents().subscribe({
      next: (items) => this.components.set(items),
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  loadRules(): void {
    this.payroll.getRules().subscribe({
      next: (items) => this.rules.set(items),
      error: () => this.error.set(this.translate.instant('payroll.loadFailed'))
    });
  }

  isProtectedCode(code: string): boolean {
    return PROTECTED_CODES.has(code.toUpperCase());
  }

  editComponent(c: SalaryComponent): void {
    this.editingComponentId.set(c.id);
    this.editingComponentRuleId.set(null);
    this.componentForm = {
      code: c.code,
      nameEn: c.nameEn,
      nameTa: c.nameTa,
      category: c.category,
      calculationType: c.calculationType,
      baseComponentCode: c.baseComponentCode ?? '',
      sortOrder: c.sortOrder,
      isTaxable: c.isTaxable,
      includeInGross: c.includeInGross,
      isActive: c.isActive,
      ruleEffectiveFrom: '2020-01-01',
      ruleEffectiveTo: '',
      fixedAmount: 0,
      ratePercent: 0,
      expression: ''
    };

    this.payroll.getRules(c.id).subscribe({
      next: (items) => {
        const rule = items
          .filter((x) => x.isActive)
          .sort((a, b) => b.effectiveFrom.localeCompare(a.effectiveFrom))[0] ?? items[0];
        if (!rule) return;
        const params = this.parseParameters(rule.parametersJson);
        this.editingComponentRuleId.set(rule.id);
        this.componentForm = {
          ...this.componentForm,
          ruleEffectiveFrom: rule.effectiveFrom,
          ruleEffectiveTo: rule.effectiveTo ?? '',
          fixedAmount: params.fixedAmount,
          ratePercent: params.rate * 100,
          expression: params.expression
        };
      }
    });
  }

  resetComponentForm(): void {
    this.editingComponentId.set(null);
    this.editingComponentRuleId.set(null);
    this.componentForm = this.defaultComponentForm();
  }

  saveComponent(): void {
    if (this.componentForm.calculationType === 'PercentOfComponent' && !this.componentForm.baseComponentCode) {
      this.error.set(this.translate.instant('payroll.selectComponent'));
      return;
    }

    this.busy.set(true);
    this.error.set('');
    const payload = {
      code: this.componentForm.code.trim(),
      nameEn: this.componentForm.nameEn.trim(),
      nameTa: this.componentForm.nameTa.trim(),
      category: this.componentForm.category,
      calculationType: this.componentForm.calculationType,
      baseComponentCode: this.componentForm.calculationType === 'PercentOfComponent'
        ? this.componentForm.baseComponentCode.trim() || null
        : null,
      sortOrder: this.componentForm.sortOrder,
      isTaxable: this.componentForm.isTaxable,
      includeInGross: this.componentForm.includeInGross,
      isActive: this.componentForm.isActive
    };
    const req = this.editingComponentId()
      ? this.payroll.updateComponent(this.editingComponentId()!, payload)
      : this.payroll.createComponent(payload);
    req.subscribe({
      next: (saved) => {
        this.saveComponentRule(saved);
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  private saveComponentRule(component: SalaryComponent): void {
    const parametersJson = this.buildParametersJsonFromComponentForm(component.calculationType);
    const rulePayload = {
      salaryComponentId: component.id,
      effectiveFrom: this.componentForm.ruleEffectiveFrom,
      effectiveTo: this.componentForm.ruleEffectiveTo || null,
      priority: 1,
      parametersJson,
      isActive: true
    };

    const req = this.editingComponentRuleId()
      ? this.payroll.updateRule(this.editingComponentRuleId()!, rulePayload)
      : this.payroll.createRule(rulePayload);

    req.subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set(this.translate.instant('payroll.componentSaved'));
        this.resetComponentForm();
        this.loadComponents();
        this.loadRules();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  formatComponentValue(component: SalaryComponent): string {
    const rule = this.rules()
      .filter((r) => r.salaryComponentId === component.id && r.isActive)
      .sort((a, b) => b.effectiveFrom.localeCompare(a.effectiveFrom))[0];
    if (!rule) return this.translate.instant('common.dash');
    const summary = this.formatRuleSummary(rule);
    if (component.calculationType === 'PercentOfComponent' && component.baseComponentCode) {
      return `${summary} ${this.translate.instant('payroll.baseComponent')}: ${component.baseComponentCode}`;
    }
    return summary;
  }

  onRuleComponentChange(): void {
    const component = this.selectedRuleComponent();
    if (!component || this.editingRuleId()) return;
    if (component.calculationType === 'Fixed') this.ruleForm.fixedAmount = 0;
    if (component.calculationType.startsWith('Percent')) this.ruleForm.ratePercent = 0;
  }

  editRule(r: SalaryRule): void {
    this.editingRuleId.set(r.id);
    const params = this.parseParameters(r.parametersJson);
    this.ruleForm = {
      salaryComponentId: r.salaryComponentId,
      effectiveFrom: r.effectiveFrom,
      effectiveTo: r.effectiveTo ?? '',
      priority: r.priority,
      fixedAmount: params.fixedAmount,
      ratePercent: params.rate * 100,
      expression: params.expression,
      isActive: r.isActive
    };
  }

  resetRuleForm(): void {
    this.editingRuleId.set(null);
    this.ruleForm = this.defaultRuleForm();
  }

  saveRule(): void {
    if (!this.ruleForm.salaryComponentId || !this.ruleForm.effectiveFrom) return;
    this.busy.set(true);
    this.error.set('');
    const component = this.selectedRuleComponent();
    const parametersJson = this.buildParametersJson(component?.calculationType ?? 'Fixed');
    const payload = {
      salaryComponentId: this.ruleForm.salaryComponentId,
      effectiveFrom: this.ruleForm.effectiveFrom,
      effectiveTo: this.ruleForm.effectiveTo || null,
      priority: this.ruleForm.priority,
      parametersJson,
      isActive: this.ruleForm.isActive
    };
    const req = this.editingRuleId()
      ? this.payroll.updateRule(this.editingRuleId()!, payload)
      : this.payroll.createRule(payload);
    req.subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set(this.translate.instant('payroll.ruleSaved'));
        this.resetRuleForm();
        this.loadRules();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  formatRuleSummary(rule: SalaryRule): string {
    const component = this.components().find((c) => c.id === rule.salaryComponentId);
    const params = this.parseParameters(rule.parametersJson);
    if (!component) return rule.parametersJson;
    switch (component.calculationType) {
      case 'Fixed':
        return `${this.translate.instant('payroll.fixedAmount')}: ${params.fixedAmount}`;
      case 'PercentOfBasic':
      case 'PercentOfGross':
      case 'PercentOfComponent':
        return `${params.rate * 100}%`;
      case 'Expression':
        return params.expression || rule.parametersJson;
      default:
        return rule.parametersJson;
    }
  }

  private defaultComponentForm(): ComponentFormModel {
    return {
      code: '',
      nameEn: '',
      nameTa: '',
      category: 'Earning',
      calculationType: 'Fixed',
      baseComponentCode: '',
      sortOrder: 0,
      isTaxable: false,
      includeInGross: true,
      isActive: true,
      ruleEffectiveFrom: '2020-01-01',
      ruleEffectiveTo: '',
      fixedAmount: 0,
      ratePercent: 0,
      expression: ''
    };
  }

  private defaultRuleForm(): RuleFormModel {
    return {
      salaryComponentId: '',
      effectiveFrom: new Date().toISOString().slice(0, 10),
      effectiveTo: '',
      priority: 1,
      fixedAmount: 0,
      ratePercent: 0,
      expression: '',
      isActive: true
    };
  }

  private parseParameters(json: string): { fixedAmount: number; rate: number; expression: string } {
    try {
      const parsed = JSON.parse(json) as { fixedAmount?: number; rate?: number; expression?: string };
      return {
        fixedAmount: parsed.fixedAmount ?? 0,
        rate: parsed.rate ?? 0,
        expression: parsed.expression ?? ''
      };
    } catch {
      return { fixedAmount: 0, rate: 0, expression: '' };
    }
  }

  private buildParametersJson(calculationType: string): string {
    return this.buildParametersJsonFromValues(
      calculationType,
      this.ruleForm.fixedAmount,
      this.ruleForm.ratePercent,
      this.ruleForm.expression
    );
  }

  private buildParametersJsonFromComponentForm(calculationType: string): string {
    return this.buildParametersJsonFromValues(
      calculationType,
      this.componentForm.fixedAmount,
      this.componentForm.ratePercent,
      this.componentForm.expression
    );
  }

  private buildParametersJsonFromValues(
    calculationType: string,
    fixedAmount: number,
    ratePercent: number,
    expression: string
  ): string {
    if (calculationType === 'Fixed') {
      return JSON.stringify({ fixedAmount: fixedAmount || 0 });
    }
    if (calculationType === 'Expression') {
      return JSON.stringify({ expression: expression || '' });
    }
    return JSON.stringify({ rate: (ratePercent || 0) / 100 });
  }
}
