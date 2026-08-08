import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
import { Department, EmployeeSummary, OrganizationService } from '../../core/organization.service';
import { PayrollService } from '../../core/payroll.service';
import { SalaryBreakdownPanelComponent } from '../payroll/salary-breakdown-panel.component';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, SalaryBreakdownPanelComponent],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ (isEdit() ? 'employees.editEmployee' : 'employees.addEmployee') | translate }}</h1>
      <a routerLink="/employees" class="btn btn-sm btn-outline-secondary ms-auto">{{ 'common.backToList' | translate }}</a>
    </div>

    <form class="card border-0 shadow-sm" [formGroup]="form" (ngSubmit)="submit()">
      <div class="card-body row g-3">
        <div class="col-md-3">
          <label class="form-label">{{ 'employees.employeeCode' | translate }}</label>
          <input class="form-control" formControlName="employeeCode" />
        </div>
        <div class="col-md-5">
          <label class="form-label">{{ 'employees.fullName' | translate }}</label>
          <input class="form-control" formControlName="fullName" />
        </div>
        <div class="col-md-4">
          <label class="form-label">{{ 'common.email' | translate }}</label>
          <input class="form-control" type="email" formControlName="email" />
        </div>
        <div class="col-md-4">
          <label class="form-label">{{ 'employees.department' | translate }}</label>
          <select class="form-select" formControlName="departmentId">
            <option value="">{{ 'employees.selectDepartment' | translate }}</option>
            @for (dept of departments(); track dept.id) {
              <option [value]="dept.id">{{ dept.name }}</option>
            }
          </select>
        </div>
        <div class="col-md-4">
          <label class="form-label">{{ 'employees.designation' | translate }}</label>
          <input class="form-control" formControlName="designation" />
        </div>
        <div class="col-md-4">
          <label class="form-label">{{ 'employees.dateOfJoining' | translate }}</label>
          <input class="form-control" type="date" formControlName="dateOfJoining" />
        </div>
        <div class="col-md-3">
          <label class="form-label">{{ 'common.status' | translate }}</label>
          <select class="form-select" formControlName="status">
            <option value="Active">{{ 'empStatus.Active' | translate }}</option>
            <option value="Inactive">{{ 'empStatus.Inactive' | translate }}</option>
            <option value="Terminated">{{ 'empStatus.Terminated' | translate }}</option>
          </select>
        </div>
        <div class="col-md-3">
          <label class="form-label">{{ 'common.phone' | translate }}</label>
          <input class="form-control" formControlName="phone" />
        </div>
        <div class="col-md-6">
          <label class="form-label">{{ 'employees.manager' | translate }}</label>
          <select class="form-select" formControlName="managerId">
            <option value="">{{ 'common.none' | translate }}</option>
            @for (mgr of managers(); track mgr.id) {
              <option [value]="mgr.id">{{ mgr.fullName }} ({{ mgr.employeeCode }})</option>
            }
          </select>
        </div>

        @if (!isEdit()) {
          <div class="col-12">
            <div class="form-check">
              <input class="form-check-input" type="checkbox" formControlName="createUserAccount" id="createUser" />
              <label class="form-check-label" for="createUser">{{ 'employees.createLogin' | translate }}</label>
            </div>
          </div>
          @if (form.controls.createUserAccount.value) {
            <div class="col-md-4">
              <label class="form-label">{{ 'employees.initialPassword' | translate }}</label>
              <input class="form-control" type="password" formControlName="userPassword" />
            </div>
            <div class="col-md-4">
              <label class="form-label">{{ 'employees.userRole' | translate }}</label>
              <select class="form-select" formControlName="userRole">
                <option value="Employee">{{ 'roles.Employee' | translate }}</option>
                <option value="Manager">{{ 'roles.Manager' | translate }}</option>
                <option value="HR">{{ 'roles.HR' | translate }}</option>
                <option value="Administrator">{{ 'roles.Administrator' | translate }}</option>
              </select>
            </div>
          }

          @if (canManageSalary()) {
            <div class="col-12">
              <div class="form-check">
                <input class="form-check-input" type="checkbox" formControlName="setSalaryNow" id="setSalaryNow" />
                <label class="form-check-label" for="setSalaryNow">{{ 'payroll.breakdown.setSalaryNow' | translate }}</label>
              </div>
            </div>
            @if (form.controls.setSalaryNow.value) {
              <div class="col-12">
                <app-salary-breakdown-panel
                  #salaryBreakdown
                  [initialEffectiveFrom]="form.controls.dateOfJoining.value"
                />
              </div>
            }
          }
        }

        @if (error()) {
          <div class="col-12">
            <div class="alert alert-danger py-2">{{ error() }}</div>
          </div>
        }
        @if (salaryWarning()) {
          <div class="col-12">
            <div class="alert alert-warning py-2">{{ salaryWarning() }}</div>
          </div>
        }

        <div class="col-12">
          <button class="btn btn-aganox" type="submit" [disabled]="form.invalid || saving() || salarySaving()">
            {{ (saving() || salarySaving()) ? ('common.saving' | translate) : ((isEdit() ? 'employees.updateEmployee' : 'employees.createEmployee') | translate) }}
          </button>
        </div>
      </div>
    </form>
  `
})
export class EmployeeFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly org = inject(OrganizationService);
  private readonly payroll = inject(PayrollService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  @ViewChild('salaryBreakdown') salaryBreakdown?: SalaryBreakdownPanelComponent;

  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly salarySaving = signal(false);
  readonly error = signal<string | null>(null);
  readonly salaryWarning = signal<string | null>(null);
  readonly departments = signal<Department[]>([]);
  readonly managers = signal<EmployeeSummary[]>([]);

  private employeeId: string | null = null;

  readonly form = this.fb.nonNullable.group({
    employeeCode: ['', Validators.required],
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    departmentId: ['', Validators.required],
    designation: ['', Validators.required],
    dateOfJoining: [new Date().toISOString().slice(0, 10), Validators.required],
    status: ['Active', Validators.required],
    phone: [''],
    managerId: [''],
    createUserAccount: [false],
    userPassword: [''],
    userRole: ['Employee'],
    setSalaryNow: [false]
  });

  ngOnInit(): void {
    this.org.getDepartments(true).subscribe((items) => this.departments.set(items));
    this.org.getEmployees({ pageSize: 100, status: 'Active' }).subscribe((result) => {
      this.managers.set(result.items);
    });

    this.form.controls.dateOfJoining.valueChanges.subscribe((value) => {
      if (this.salaryBreakdown && value) {
        this.salaryBreakdown.effectiveFrom = value;
        this.salaryBreakdown.onInputChanged();
      }
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isEdit.set(true);
      this.employeeId = id;
      this.org.getEmployee(id).subscribe({
        next: (emp) => {
          this.form.patchValue({
            employeeCode: emp.employeeCode,
            fullName: emp.fullName,
            email: emp.email,
            departmentId: emp.departmentId,
            designation: emp.designation,
            dateOfJoining: emp.dateOfJoining,
            status: emp.status,
            phone: emp.phone ?? '',
            managerId: emp.managerId ?? ''
          });
          this.managers.set(this.managers().filter((m) => m.id !== id));
        }
      });
    }
  }

  canManageSalary(): boolean {
    return this.auth.hasPermission('Payroll.Salary.Manage');
  }

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    if (this.form.controls.setSalaryNow.value && this.salaryBreakdown && !this.salaryBreakdown.isValid()) {
      this.error.set(this.translate.instant('payroll.basicPay') + ' ' + this.translate.instant('common.saveFailed'));
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.salaryWarning.set(null);
    const raw = this.form.getRawValue();
    const payload: Record<string, unknown> = {
      employeeCode: raw.employeeCode,
      fullName: raw.fullName,
      email: raw.email,
      departmentId: raw.departmentId,
      designation: raw.designation,
      dateOfJoining: raw.dateOfJoining,
      status: raw.status,
      phone: raw.phone || null,
      managerId: raw.managerId || null
    };

    if (!this.isEdit()) {
      payload['createUserAccount'] = raw.createUserAccount;
      if (raw.createUserAccount) {
        payload['userPassword'] = raw.userPassword;
        payload['userRole'] = raw.userRole;
      }
    }

    const request = this.isEdit()
      ? this.org.updateEmployee(this.employeeId!, payload)
      : this.org.createEmployee(payload);

    request.subscribe({
      next: (emp) => {
        this.saving.set(false);
        if (!this.isEdit() && raw.setSalaryNow && this.salaryBreakdown) {
          this.saveSalaryRevision(emp.id);
          return;
        }
        void this.router.navigate(['/employees', emp.id]);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  private saveSalaryRevision(employeeId: string): void {
    const panel = this.salaryBreakdown;
    if (!panel) {
      void this.router.navigate(['/employees', employeeId]);
      return;
    }

    this.salarySaving.set(true);
    this.payroll.createSalaryRevision({
      employeeId,
      effectiveFrom: panel.effectiveFrom,
      basicPay: panel.basicPay,
      overrides: panel.getOverridesPayload()
    }).subscribe({
      next: () => {
        this.salarySaving.set(false);
        void this.router.navigate(['/employees', employeeId]);
      },
      error: () => {
        this.salarySaving.set(false);
        this.salaryWarning.set(this.translate.instant('payroll.breakdown.salarySaveFailed'));
        void this.router.navigate(['/employees', employeeId]);
      }
    });
  }
}
