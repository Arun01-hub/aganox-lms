import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Department, EmployeeSummary, OrganizationService } from '../../core/organization.service';

@Component({
  selector: 'app-employee-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ isEdit() ? 'Edit employee' : 'Add employee' }}</h1>
      <a routerLink="/employees" class="btn btn-sm btn-outline-secondary ms-auto">Back to list</a>
    </div>

    <form class="card border-0 shadow-sm" [formGroup]="form" (ngSubmit)="submit()">
      <div class="card-body row g-3">
        <div class="col-md-3">
          <label class="form-label">Employee code</label>
          <input class="form-control" formControlName="employeeCode" />
        </div>
        <div class="col-md-5">
          <label class="form-label">Full name</label>
          <input class="form-control" formControlName="fullName" />
        </div>
        <div class="col-md-4">
          <label class="form-label">Email</label>
          <input class="form-control" type="email" formControlName="email" />
        </div>
        <div class="col-md-4">
          <label class="form-label">Department</label>
          <select class="form-select" formControlName="departmentId">
            <option value="">Select department</option>
            @for (dept of departments(); track dept.id) {
              <option [value]="dept.id">{{ dept.name }}</option>
            }
          </select>
        </div>
        <div class="col-md-4">
          <label class="form-label">Designation</label>
          <input class="form-control" formControlName="designation" />
        </div>
        <div class="col-md-4">
          <label class="form-label">Date of joining</label>
          <input class="form-control" type="date" formControlName="dateOfJoining" />
        </div>
        <div class="col-md-3">
          <label class="form-label">Status</label>
          <select class="form-select" formControlName="status">
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
            <option value="Terminated">Terminated</option>
          </select>
        </div>
        <div class="col-md-3">
          <label class="form-label">Phone</label>
          <input class="form-control" formControlName="phone" />
        </div>
        <div class="col-md-6">
          <label class="form-label">Manager</label>
          <select class="form-select" formControlName="managerId">
            <option value="">None</option>
            @for (mgr of managers(); track mgr.id) {
              <option [value]="mgr.id">{{ mgr.fullName }} ({{ mgr.employeeCode }})</option>
            }
          </select>
        </div>

        @if (!isEdit()) {
          <div class="col-12">
            <div class="form-check">
              <input class="form-check-input" type="checkbox" formControlName="createUserAccount" id="createUser" />
              <label class="form-check-label" for="createUser">Create login account</label>
            </div>
          </div>
          @if (form.controls.createUserAccount.value) {
            <div class="col-md-4">
              <label class="form-label">Initial password</label>
              <input class="form-control" type="password" formControlName="userPassword" />
            </div>
            <div class="col-md-4">
              <label class="form-label">User role</label>
              <select class="form-select" formControlName="userRole">
                <option value="Employee">Employee</option>
                <option value="Manager">Manager</option>
                <option value="HR">HR</option>
                <option value="Administrator">Administrator</option>
              </select>
            </div>
          }
        }

        @if (error()) {
          <div class="col-12">
            <div class="alert alert-danger py-2">{{ error() }}</div>
          </div>
        }

        <div class="col-12">
          <button class="btn btn-aganox" type="submit" [disabled]="form.invalid || saving()">
            {{ saving() ? 'Saving…' : isEdit() ? 'Update employee' : 'Create employee' }}
          </button>
        </div>
      </div>
    </form>
  `
})
export class EmployeeFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly org = inject(OrganizationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
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
    userRole: ['Employee']
  });

  ngOnInit(): void {
    this.org.getDepartments(true).subscribe((items) => this.departments.set(items));
    this.org.getEmployees({ pageSize: 100, status: 'Active' }).subscribe((result) => {
      this.managers.set(result.items);
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

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
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
        void this.router.navigate(['/employees', emp.id]);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.errors?.[0] ?? 'Save failed.');
      }
    });
  }
}
