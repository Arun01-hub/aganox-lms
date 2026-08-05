import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import {
  Department,
  EmployeeSummary,
  OrganizationService
} from '../../core/organization.service';

@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">Employees</h1>
      <a routerLink="/departments" class="btn btn-sm btn-outline-secondary">Departments</a>
      @if (canCreate()) {
        <a routerLink="/employees/new" class="btn btn-sm btn-aganox ms-auto">Add employee</a>
      }
    </div>

    <div class="card border-0 shadow-sm mb-3">
      <div class="card-body row g-3">
        <div class="col-md-4">
          <label class="form-label">Search</label>
          <input class="form-control" [(ngModel)]="filters.search" (keyup.enter)="load()" placeholder="Name, code, email" />
        </div>
        <div class="col-md-3">
          <label class="form-label">Department</label>
          <select class="form-select" [(ngModel)]="filters.departmentId">
            <option value="">All</option>
            @for (dept of departments(); track dept.id) {
              <option [value]="dept.id">{{ dept.name }}</option>
            }
          </select>
        </div>
        <div class="col-md-2">
          <label class="form-label">Status</label>
          <select class="form-select" [(ngModel)]="filters.status">
            <option value="">All</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
            <option value="Terminated">Terminated</option>
          </select>
        </div>
        <div class="col-md-3 d-flex align-items-end gap-2">
          <button class="btn btn-aganox" type="button" (click)="load()">Apply</button>
          <button class="btn btn-outline-secondary" type="button" (click)="resetFilters()">Reset</button>
        </div>
      </div>
    </div>

    <div class="card border-0 shadow-sm">
      <div class="table-responsive">
        <table class="table table-hover mb-0 align-middle">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>Department</th>
              <th>Designation</th>
              <th>Manager</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (emp of employees(); track emp.id) {
              <tr>
                <td><code>{{ emp.employeeCode }}</code></td>
                <td>{{ emp.fullName }}</td>
                <td>{{ emp.departmentName }}</td>
                <td>{{ emp.designation }}</td>
                <td>{{ emp.managerName ?? '—' }}</td>
                <td>{{ emp.status }}</td>
                <td class="text-end">
                  <a class="btn btn-sm btn-outline-primary me-1" [routerLink]="['/employees', emp.id]">Profile</a>
                  @if (canUpdate()) {
                    <a class="btn btn-sm btn-outline-secondary" [routerLink]="['/employees', emp.id, 'edit']">Edit</a>
                  }
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="7" class="text-center text-muted py-4">No employees found.</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <div class="card-footer d-flex justify-content-between align-items-center">
        <span class="small text-muted">{{ totalCount() }} total</span>
        <div class="btn-group">
          <button class="btn btn-sm btn-outline-secondary" type="button" (click)="prevPage()" [disabled]="page() <= 1">
            Previous
          </button>
          <button class="btn btn-sm btn-outline-secondary" disabled>Page {{ page() }}</button>
          <button
            class="btn btn-sm btn-outline-secondary"
            type="button"
            (click)="nextPage()"
            [disabled]="page() >= totalPages()"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  `
})
export class EmployeesComponent implements OnInit {
  private readonly org = inject(OrganizationService);
  private readonly auth = inject(AuthService);

  readonly employees = signal<EmployeeSummary[]>([]);
  readonly departments = signal<Department[]>([]);
  readonly page = signal(1);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);

  filters = { search: '', departmentId: '', status: '' };

  ngOnInit(): void {
    this.org.getDepartments(true).subscribe((items) => this.departments.set(items));
    this.load();
  }

  canCreate(): boolean {
    return this.auth.hasPermission('Employees.Create');
  }

  canUpdate(): boolean {
    return this.auth.hasPermission('Employees.Update');
  }

  load(): void {
    this.org
      .getEmployees({
        search: this.filters.search || undefined,
        departmentId: this.filters.departmentId || undefined,
        status: this.filters.status || undefined,
        page: this.page(),
        pageSize: 10,
        sortBy: 'fullName',
        sortDir: 'asc'
      })
      .subscribe({
        next: (result) => {
          this.employees.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
        }
      });
  }

  resetFilters(): void {
    this.filters = { search: '', departmentId: '', status: '' };
    this.page.set(1);
    this.load();
  }

  prevPage(): void {
    this.page.update((p) => Math.max(1, p - 1));
    this.load();
  }

  nextPage(): void {
    this.page.update((p) => p + 1);
    this.load();
  }
}
