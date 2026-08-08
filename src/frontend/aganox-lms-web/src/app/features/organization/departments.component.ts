import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
import { Department, OrganizationService } from '../../core/organization.service';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [RouterLink, FormsModule, TranslatePipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ 'departments.title' | translate }}</h1>
      <a routerLink="/employees" class="btn btn-sm btn-outline-secondary ms-auto">{{ 'nav.employees' | translate }}</a>
    </div>

    @if (canManage()) {
      <div class="card border-0 shadow-sm mb-4">
        <div class="card-body">
          <h2 class="h6">{{ (editingId() ? 'departments.editDepartment' : 'departments.addDepartment') | translate }}</h2>
          <div class="row g-3">
            <div class="col-md-4">
              <label class="form-label">{{ 'common.name' | translate }}</label>
              <input class="form-control" [(ngModel)]="form.name" />
            </div>
            <div class="col-md-3">
              <label class="form-label">{{ 'common.code' | translate }}</label>
              <input class="form-control" [(ngModel)]="form.code" />
            </div>
            <div class="col-md-2 d-flex align-items-end">
              <div class="form-check">
                <input class="form-check-input" type="checkbox" [(ngModel)]="form.isActive" id="deptActive" />
                <label class="form-check-label" for="deptActive">{{ 'common.active' | translate }}</label>
              </div>
            </div>
            <div class="col-md-3 d-flex align-items-end gap-2">
              <button class="btn btn-aganox" type="button" (click)="save()" [disabled]="saving()">
                {{ (editingId() ? 'common.update' : 'common.create') | translate }}
              </button>
              @if (editingId()) {
                <button class="btn btn-outline-secondary" type="button" (click)="resetForm()">{{ 'common.cancel' | translate }}</button>
              }
            </div>
          </div>
          @if (error()) {
            <div class="alert alert-danger mt-3 py-2">{{ error() }}</div>
          }
        </div>
      </div>
    }

    <div class="card border-0 shadow-sm">
      <div class="table-responsive">
        <table class="table table-hover mb-0 align-middle">
          <thead>
            <tr>
              <th>{{ 'common.name' | translate }}</th>
              <th>{{ 'common.code' | translate }}</th>
              <th>{{ 'common.status' | translate }}</th>
              <th>{{ 'departments.employeesCol' | translate }}</th>
              @if (canManage()) {
                <th></th>
              }
            </tr>
          </thead>
          <tbody>
            @for (dept of departments(); track dept.id) {
              <tr>
                <td>{{ dept.name }}</td>
                <td><code>{{ dept.code }}</code></td>
                <td>
                  <span class="badge" [class.text-bg-success]="dept.isActive" [class.text-bg-secondary]="!dept.isActive">
                    {{ (dept.isActive ? 'common.active' : 'common.inactive') | translate }}
                  </span>
                </td>
                <td>{{ dept.employeeCount }}</td>
                @if (canManage()) {
                  <td class="text-end">
                    <button class="btn btn-sm btn-outline-primary me-1" type="button" (click)="edit(dept)">{{ 'common.edit' | translate }}</button>
                    <button
                      class="btn btn-sm btn-outline-danger"
                      type="button"
                      (click)="remove(dept)"
                      [disabled]="dept.employeeCount > 0"
                    >
                      {{ 'common.delete' | translate }}
                    </button>
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td colspan="5" class="text-center text-muted py-4">{{ 'departments.noDepartments' | translate }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class DepartmentsComponent implements OnInit {
  private readonly org = inject(OrganizationService);
  private readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);

  readonly departments = signal<Department[]>([]);
  readonly editingId = signal<string | null>(null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  form = { name: '', code: '', isActive: true };

  ngOnInit(): void {
    this.load();
  }

  canManage(): boolean {
    return this.auth.hasPermission('Employees.Create');
  }

  load(): void {
    this.org.getDepartments().subscribe({
      next: (items) => this.departments.set(items),
      error: () => this.error.set(this.translate.instant('departments.loadFailed'))
    });
  }

  edit(dept: Department): void {
    this.editingId.set(dept.id);
    this.form = { name: dept.name, code: dept.code, isActive: dept.isActive };
    this.error.set(null);
  }

  resetForm(): void {
    this.editingId.set(null);
    this.form = { name: '', code: '', isActive: true };
    this.error.set(null);
  }

  save(): void {
    if (!this.form.name.trim() || !this.form.code.trim()) {
      this.error.set(this.translate.instant('departments.nameCodeRequired'));
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const payload = {
      name: this.form.name.trim(),
      code: this.form.code.trim(),
      isActive: this.form.isActive
    };

    const request = this.editingId()
      ? this.org.updateDepartment(this.editingId()!, payload)
      : this.org.createDepartment(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.resetForm();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  remove(dept: Department): void {
    if (!confirm(this.translate.instant('departments.deleteConfirm', { name: dept.name }))) {
      return;
    }

    this.org.deleteDepartment(dept.id).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.deleteFailed'))
    });
  }
}
