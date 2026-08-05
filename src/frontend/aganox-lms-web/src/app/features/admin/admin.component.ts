import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService, AppSetting, AuditLog, ActivityLog, BackupHistory, RolePermissions } from '../../core/admin.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">Administration</h1>
      <a routerLink="/" class="btn btn-sm btn-outline-secondary ms-auto">Dashboard</a>
    </div>

    @if (message()) { <div class="alert alert-success">{{ message() }}</div> }
    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }

    <ul class="nav nav-tabs mb-3">
      @if (auth.hasPermission('Admin.Settings')) {
        <li class="nav-item"><button class="nav-link" [class.active]="tab() === 'settings'" type="button" (click)="tab.set('settings')">Settings</button></li>
      }
      @if (auth.hasPermission('Users.Manage')) {
        <li class="nav-item"><button class="nav-link" [class.active]="tab() === 'permissions'" type="button" (click)="loadPermissions(); tab.set('permissions')">Permissions</button></li>
      }
      @if (auth.hasPermission('Admin.Audit')) {
        <li class="nav-item"><button class="nav-link" [class.active]="tab() === 'audit'" type="button" (click)="loadAudit(); tab.set('audit')">Audit</button></li>
      }
      @if (auth.hasPermission('Admin.Backup')) {
        <li class="nav-item"><button class="nav-link" [class.active]="tab() === 'backup'" type="button" (click)="loadBackups(); tab.set('backup')">Backup</button></li>
      }
    </ul>

    @if (tab() === 'settings' && settings().length) {
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          @for (s of settings(); track s.key) {
            <div class="mb-3">
              <label class="form-label">{{ s.key }}</label>
              <input class="form-control" [(ngModel)]="s.value" [name]="s.key" />
            </div>
          }
          <button class="btn btn-aganox" type="button" (click)="saveSettings()">Save settings</button>
        </div>
      </div>
    }

    @if (tab() === 'permissions' && selectedRole()) {
      <div class="card border-0 shadow-sm">
        <div class="card-body row g-3">
          <div class="col-md-4">
            <label class="form-label">Role</label>
            <select class="form-select" [ngModel]="selectedRole()!.roleId" (ngModelChange)="selectRole($event)">
              @for (r of roles(); track r.roleId) {
                <option [value]="r.roleId">{{ r.roleName }}</option>
              }
            </select>
          </div>
          <div class="col-12">
            <div class="row">
              @for (p of allPermissions; track p) {
                <div class="col-md-4">
                  <div class="form-check">
                    <input class="form-check-input" type="checkbox" [checked]="rolePermissionSet().has(p)" (change)="togglePermission(p, $event)" [id]="p" />
                    <label class="form-check-label small" [for]="p">{{ p }}</label>
                  </div>
                </div>
              }
            </div>
          </div>
          <div class="col-12">
            <button class="btn btn-aganox" type="button" (click)="savePermissions()">Save role permissions</button>
          </div>
        </div>
      </div>
    }

    @if (tab() === 'audit') {
      <div class="row g-3">
        <div class="col-lg-6">
          <div class="card border-0 shadow-sm">
            <div class="card-header bg-white fw-semibold">Audit trail</div>
            <table class="table mb-0 small">
              <thead><tr><th>When</th><th>Entity</th><th>Action</th></tr></thead>
              <tbody>
                @for (a of auditLogs(); track a.id) {
                  <tr><td>{{ a.timestamp }}</td><td>{{ a.entityName }}</td><td>{{ a.action }}</td></tr>
                }
              </tbody>
            </table>
          </div>
        </div>
        <div class="col-lg-6">
          <div class="card border-0 shadow-sm">
            <div class="card-header bg-white fw-semibold">Activity log</div>
            <table class="table mb-0 small">
              <thead><tr><th>When</th><th>Action</th><th>Details</th></tr></thead>
              <tbody>
                @for (a of activityLogs(); track a.id) {
                  <tr><td>{{ a.timestamp }}</td><td>{{ a.action }}</td><td>{{ a.details }}</td></tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    }

    @if (tab() === 'backup') {
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          <button class="btn btn-aganox mb-3" type="button" (click)="runBackup()" [disabled]="busy()">Run backup now</button>
          <table class="table mb-0">
            <thead><tr><th>Date</th><th>File</th><th>Status</th><th></th></tr></thead>
            <tbody>
              @for (b of backups(); track b.id) {
                <tr>
                  <td>{{ b.createdAt }}</td>
                  <td class="small">{{ b.filePath }}</td>
                  <td>{{ b.status }}</td>
                  <td class="text-end">
                    @if (b.status === 'Completed') {
                      <button class="btn btn-sm btn-outline-danger" type="button" (click)="restore(b.filePath)">Restore</button>
                    }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="4" class="text-muted text-center py-3">No backups yet.</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }
  `
})
export class AdminComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly admin = inject(AdminService);

  readonly tab = signal('settings');
  readonly settings = signal<AppSetting[]>([]);
  readonly roles = signal<RolePermissions[]>([]);
  readonly selectedRole = signal<RolePermissions | null>(null);
  readonly rolePermissionSet = signal(new Set<string>());
  readonly auditLogs = signal<AuditLog[]>([]);
  readonly activityLogs = signal<ActivityLog[]>([]);
  readonly backups = signal<BackupHistory[]>([]);
  readonly error = signal('');
  readonly message = signal('');
  readonly busy = signal(false);

  readonly allPermissions = [
    'Employees.View', 'Employees.Create', 'Employees.Update', 'Employees.Delete',
    'Leave.Apply', 'Leave.ViewOwn', 'Leave.ViewTeam', 'Leave.ViewAll', 'Approvals.Act',
    'Balances.View', 'Balances.Adjust', 'Balances.Admin', 'LeaveTypes.Manage', 'Holidays.Manage',
    'Reports.View', 'Reports.Export', 'Admin.Settings', 'Admin.Backup', 'Admin.Audit', 'Users.Manage'
  ];

  ngOnInit(): void {
    if (this.auth.hasPermission('Admin.Settings')) {
      this.admin.getSettings().subscribe((s) => this.settings.set(s));
    } else if (this.auth.hasPermission('Users.Manage')) {
      this.tab.set('permissions');
      this.loadPermissions();
    } else if (this.auth.hasPermission('Admin.Audit')) {
      this.tab.set('audit');
      this.loadAudit();
    } else if (this.auth.hasPermission('Admin.Backup')) {
      this.tab.set('backup');
      this.loadBackups();
    }
  }

  saveSettings(): void {
    this.admin.updateSettings(this.settings()).subscribe({
      next: (s) => { this.settings.set(s); this.message.set('Settings saved.'); },
      error: () => this.error.set('Failed to save settings.')
    });
  }

  loadPermissions(): void {
    this.admin.getPermissions().subscribe((roles) => {
      this.roles.set(roles);
      if (roles.length) this.selectRole(roles[0].roleId);
    });
  }

  selectRole(roleId: string): void {
    const role = this.roles().find((r) => r.roleId === roleId) ?? null;
    this.selectedRole.set(role);
    this.rolePermissionSet.set(new Set(role?.permissions ?? []));
  }

  togglePermission(permission: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const set = new Set(this.rolePermissionSet());
    if (checked) set.add(permission); else set.delete(permission);
    this.rolePermissionSet.set(set);
  }

  savePermissions(): void {
    const role = this.selectedRole();
    if (!role) return;
    this.admin.updateRolePermissions(role.roleId, [...this.rolePermissionSet()]).subscribe({
      next: (updated) => {
        this.message.set(`Permissions updated for ${updated.roleName}.`);
        this.loadPermissions();
      },
      error: (err) => this.error.set(err.error?.errors?.[0] ?? 'Save failed.')
    });
  }

  loadAudit(): void {
    this.admin.getAuditLogs().subscribe((a) => this.auditLogs.set(a.items));
    this.admin.getActivityLogs().subscribe((a) => this.activityLogs.set(a.items));
  }

  loadBackups(): void {
    this.admin.getBackups().subscribe((b) => this.backups.set(b.items));
  }

  runBackup(): void {
    this.busy.set(true);
    this.admin.createBackup().subscribe({
      next: () => { this.busy.set(false); this.message.set('Backup completed.'); this.loadBackups(); },
      error: (err) => { this.busy.set(false); this.error.set(err.error?.errors?.[0] ?? 'Backup failed.'); }
    });
  }

  restore(path: string): void {
    if (!confirm('Restore will overwrite the database. Continue?')) return;
    this.admin.restoreBackup(path).subscribe({
      next: () => this.message.set('Restore completed. Restart the application if needed.'),
      error: (err) => this.error.set(err.error?.errors?.[0] ?? 'Restore failed.')
    });
  }
}
