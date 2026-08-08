import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { LanguageService } from './language.service';

export interface SalaryComponent {
  id: string;
  code: string;
  nameEn: string;
  nameTa: string;
  category: string;
  calculationType: string;
  baseComponentCode?: string | null;
  sortOrder: number;
  isTaxable: boolean;
  includeInGross: boolean;
  isActive: boolean;
}

export interface SalaryRule {
  id: string;
  salaryComponentId: string;
  componentCode: string;
  effectiveFrom: string;
  effectiveTo?: string | null;
  priority: number;
  parametersJson: string;
  isActive: boolean;
}

export interface EmployeeSalaryRevision {
  id: string;
  employeeId: string;
  employeeName: string;
  effectiveFrom: string;
  effectiveTo?: string | null;
  basicPay: number;
  remarks?: string | null;
  overrides: { salaryComponentId: string; componentCode: string; fixedAmount: number }[];
}

export interface PayrollRun {
  id: string;
  year: number;
  month: number;
  status: string;
  calculatedAt?: string | null;
  approvedAt?: string | null;
  lockedAt?: string | null;
  notes?: string | null;
  employeeCount: number;
  totalNetPay: number;
}

export interface PayrollEmployeeResult {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeCode: string;
  departmentId?: string | null;
  basicPay: number;
  grossPay: number;
  totalDeductions: number;
  netPay: number;
  paidDays: number;
  lopDays: number;
  lineItems: { componentCode: string; componentName: string; category: string; amount: number; sortOrder: number }[];
}

export interface PayrollArrear {
  id: string;
  employeeId: string;
  employeeName: string;
  sourceYear: number;
  sourceMonth: number;
  targetPayrollRunId?: string | null;
  amount: number;
  reason: string;
  status: string;
}

export interface Payslip {
  payrollRunId: string;
  year: number;
  month: number;
  employeeId: string;
  employeeName: string;
  employeeCode: string;
  departmentName: string;
  basicPay: number;
  grossPay: number;
  totalDeductions: number;
  netPay: number;
  paidDays: number;
  lopDays: number;
  lineItems: { componentCode: string; componentName: string; category: string; amount: number; sortOrder: number }[];
}

export interface SalaryEntryContext {
  run: PayrollRun;
  departments: { id: string; name: string; code: string }[];
  earnings: SalaryEntryComponent[];
  deductions: SalaryEntryComponent[];
  canEdit: boolean;
}

export interface SalaryEntryComponent {
  id: string;
  code: string;
  nameEn: string;
  nameTa: string;
  category: string;
  sortOrder: number;
  ratePercent?: number | null;
}

export interface SalaryEntryLine {
  componentCode: string;
  componentName: string;
  category: string;
  amount: number;
  sortOrder: number;
  ratePercent?: number | null;
}

export interface SalaryEntryEmployee {
  employeeId: string;
  employeeCode: string;
  employeeName: string;
  departmentId?: string | null;
  departmentName?: string | null;
  resultId: string;
  basicPay: number;
  grossPay: number;
  totalDeductions: number;
  netPay: number;
  paidDays: number;
  lopDays: number;
  isManuallyAdjusted: boolean;
  lines: SalaryEntryLine[];
}

export interface SalaryPreviewLine {
  componentCode: string;
  componentName: string;
  category: string;
  amount: number;
  sortOrder: number;
}

export interface SalaryPreview {
  basicPay: number;
  grossPay: number;
  totalDeductions: number;
  netPay: number;
  lines: SalaryPreviewLine[];
}

@Injectable({ providedIn: 'root' })
export class PayrollService {
  private readonly http = inject(HttpClient);
  private readonly language = inject(LanguageService);

  getComponents(activeOnly = false) {
    return this.http.get<SalaryComponent[]>(`${environment.apiBaseUrl}/payroll/components`, {
      params: new HttpParams().set('activeOnly', activeOnly)
    });
  }

  createComponent(payload: Partial<SalaryComponent>) {
    return this.http.post<SalaryComponent>(`${environment.apiBaseUrl}/payroll/components`, payload);
  }

  updateComponent(id: string, payload: Partial<SalaryComponent>) {
    return this.http.put<SalaryComponent>(`${environment.apiBaseUrl}/payroll/components/${id}`, payload);
  }

  getRules(componentId?: string) {
    let params = new HttpParams();
    if (componentId) params = params.set('componentId', componentId);
    return this.http.get<SalaryRule[]>(`${environment.apiBaseUrl}/payroll/rules`, { params });
  }

  createRule(payload: Partial<SalaryRule>) {
    return this.http.post<SalaryRule>(`${environment.apiBaseUrl}/payroll/rules`, payload);
  }

  updateRule(id: string, payload: Partial<SalaryRule>) {
    return this.http.put<SalaryRule>(`${environment.apiBaseUrl}/payroll/rules/${id}`, payload);
  }

  previewSalary(payload: {
    basicPay: number;
    effectiveFrom: string;
    overrides?: { salaryComponentId: string; componentCode: string; fixedAmount: number }[];
  }) {
    return this.http.post<SalaryPreview>(`${environment.apiBaseUrl}/payroll/salaries/preview`, payload);
  }

  getSalaryRevisions(employeeId: string) {
    return this.http.get<EmployeeSalaryRevision[]>(`${environment.apiBaseUrl}/payroll/salaries/${employeeId}`);
  }

  createSalaryRevision(payload: {
    employeeId: string;
    effectiveFrom: string;
    effectiveTo?: string | null;
    basicPay: number;
    remarks?: string;
    overrides?: { salaryComponentId: string; componentCode: string; fixedAmount: number }[];
  }) {
    return this.http.post<EmployeeSalaryRevision>(`${environment.apiBaseUrl}/payroll/salaries`, payload);
  }

  updateSalaryRevision(id: string, payload: {
    employeeId: string;
    effectiveFrom: string;
    effectiveTo?: string | null;
    basicPay: number;
    remarks?: string;
    overrides?: { salaryComponentId: string; componentCode: string; fixedAmount: number }[];
  }) {
    return this.http.put<EmployeeSalaryRevision>(`${environment.apiBaseUrl}/payroll/salaries/${id}`, payload);
  }

  getRuns(year?: number) {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    return this.http.get<PayrollRun[]>(`${environment.apiBaseUrl}/payroll/runs`, { params });
  }

  getRun(id: string) {
    return this.http.get<PayrollRun>(`${environment.apiBaseUrl}/payroll/runs/${id}`);
  }

  getRunResults(id: string) {
    return this.http.get<PayrollEmployeeResult[]>(`${environment.apiBaseUrl}/payroll/runs/${id}/results`);
  }

  createRun(year: number, month: number, notes?: string) {
    return this.http.post<PayrollRun>(`${environment.apiBaseUrl}/payroll/runs`, { year, month, notes });
  }

  calculateRun(id: string, recalculate = false) {
    return this.http.post<PayrollRun>(`${environment.apiBaseUrl}/payroll/runs/${id}/calculate`, null, {
      params: new HttpParams().set('recalculate', recalculate)
    });
  }

  submitRun(id: string) {
    return this.http.post<PayrollRun>(`${environment.apiBaseUrl}/payroll/runs/${id}/submit`, null);
  }

  approveRun(id: string) {
    return this.http.post<PayrollRun>(`${environment.apiBaseUrl}/payroll/runs/${id}/approve`, null);
  }

  lockRun(id: string) {
    return this.http.post<PayrollRun>(`${environment.apiBaseUrl}/payroll/runs/${id}/lock`, null);
  }

  getArrears(employeeId?: string) {
    let params = new HttpParams();
    if (employeeId) params = params.set('employeeId', employeeId);
    return this.http.get<PayrollArrear[]>(`${environment.apiBaseUrl}/payroll/arrears`, { params });
  }

  createArrear(payload: { employeeId: string; sourceYear: number; sourceMonth: number; amount: number; reason: string }) {
    return this.http.post<PayrollArrear>(`${environment.apiBaseUrl}/payroll/arrears`, payload);
  }

  getPayslip(runId: string, employeeId?: string) {
    let params = new HttpParams();
    if (employeeId) params = params.set('employeeId', employeeId);
    return this.http.get<Payslip>(`${environment.apiBaseUrl}/payroll/payslips/${runId}`, { params });
  }

  getReport(runId: string, reportType: string) {
    return this.http.get<{ reportType: string; rows: Record<string, unknown>[] }>(
      `${environment.apiBaseUrl}/payroll/reports/${runId}`,
      { params: new HttpParams().set('reportType', reportType) }
    );
  }

  getSalaryEntryContext(runId: string) {
    return this.http.get<SalaryEntryContext>(`${environment.apiBaseUrl}/payroll/runs/${runId}/salary-entry/context`);
  }

  getSalaryEntry(runId: string, departmentId?: string, employeeId?: string) {
    let params = new HttpParams();
    if (departmentId) params = params.set('departmentId', departmentId);
    if (employeeId) params = params.set('employeeId', employeeId);
    return this.http.get<SalaryEntryEmployee>(`${environment.apiBaseUrl}/payroll/runs/${runId}/salary-entry`, { params });
  }

  saveSalaryEntry(runId: string, employeeId: string, lines: { componentCode: string; amount: number }[]) {
    return this.http.put<SalaryEntryEmployee>(
      `${environment.apiBaseUrl}/payroll/runs/${runId}/salary-entry/${employeeId}`,
      { lines }
    );
  }

  resetSalaryEntry(runId: string, employeeId: string) {
    return this.http.post<SalaryEntryEmployee>(
      `${environment.apiBaseUrl}/payroll/runs/${runId}/salary-entry/${employeeId}/reset`,
      {}
    );
  }

  deleteSalaryEntry(runId: string, employeeId: string) {
    return this.http.delete<void>(`${environment.apiBaseUrl}/payroll/runs/${runId}/salary-entry/${employeeId}`);
  }

  exportPayslipPdf(runId: string, employeeId: string) {
    return this.http.get(`${environment.apiBaseUrl}/payroll/payslips/${runId}/export`, {
      params: new HttpParams()
        .set('employeeId', employeeId)
        .set('lang', this.language.currentLang()),
      responseType: 'blob'
    });
  }

  exportPayrollReport(runId: string, reportType: string, format: 'csv' | 'xlsx' | 'pdf' = 'pdf') {
    return this.http.get(`${environment.apiBaseUrl}/payroll/reports/${runId}/export`, {
      params: new HttpParams()
        .set('reportType', reportType)
        .set('format', format)
        .set('lang', this.language.currentLang()),
      responseType: 'blob'
    });
  }
}
