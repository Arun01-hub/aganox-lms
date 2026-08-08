import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AdminService, AppSetting, AuditLog, ActivityLog, BackupHistory, RolePermissions } from '../../core/admin.service';
import { AuthService } from '../../core/auth.service';
import { NavigationMenuItemAdmin, NavigationService } from '../../core/navigation.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [RouterLink, FormsModule, TranslatePipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ 'admin.title' | translate }}</h1>
      <a routerLink="/" class="btn btn-sm btn-outline-secondary ms-auto">{{ 'common.dashboard' | translate }}</a>
    </div>

    @if (message()) { <div class="alert alert-success">{{ message() }}</div> }
    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }

    <ul class="nav nav-tabs mb-3">
      @if (auth.hasPermission('Admin.Settings')) {
        <li class="nav-item"><button class="nav-link" [class.active]="tab() === 'settings'" type="button" (click)="tab.set('settings')">{{ 'admin.settings' | translate }}</button></li>
        <li class="nav-item"><button class="nav-link" [class.active]="tab() === 'menu'" type="button" (click)="loadMenu(); tab.set('menu')">{{ 'admin.menu' | translate }}</button></li>
      }
      @if (auth.hasPermission('Users.Manage')) {
        <li class="nav-item"><button class="nav-link" [class.active]="tab() === 'permissions'" type="button" (click)="loadPermissions(); tab.set('permissions')">{{ 'admin.permissions' | translate }}</button></li>
      }
      @if (auth.hasPermission('Admin.Audit')) {
        <li class="nav-item"><button class="nav-link" [class.active]="tab() === 'audit'" type="button" (click)="loadAudit(); tab.set('audit')">{{ 'admin.audit' | translate }}</button></li>
      }
      @if (auth.hasPermission('Admin.Backup')) {
        <li class="nav-item"><button class="nav-link" [class.active]="tab() === 'backup'" type="button" (click)="loadBackups(); tab.set('backup')">{{ 'admin.backup' | translate }}</button></li>
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
          <button class="btn btn-aganox" type="button" (click)="saveSettings()">{{ 'admin.saveSettings' | translate }}</button>
        </div>
      </div>
    }

    @if (tab() === 'permissions' && selectedRole()) {
      <div class="card border-0 shadow-sm">
        <div class="card-body row g-3">
          <div class="col-md-4">
            <label class="form-label">{{ 'common.role' | translate }}</label>
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
            <button class="btn btn-aganox" type="button" (click)="savePermissions()">{{ 'admin.saveRolePermissions' | translate }}</button>
          </div>
        </div>
      </div>
    }

    @if (tab() === 'menu' && menuItems().length) {
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          <div class="d-flex flex-wrap gap-2 mb-3">
            <button class="btn btn-aganox" type="button" (click)="saveMenu()">{{ 'admin.menuSave' | translate }}</button>
            <button class="btn btn-outline-secondary" type="button" (click)="addMenuItem()">{{ 'admin.menuAddItem' | translate }}</button>
            <button class="btn btn-outline-warning" type="button" (click)="resetMenu()">{{ 'admin.menuReset' | translate }}</button>
          </div>
          <div class="table-responsive">
            <table class="table table-sm align-middle">
              <thead>
                <tr>
                  <th>{{ 'admin.menuCode' | translate }}</th>
                  <th>{{ 'admin.menuLabelKey' | translate }}</th>
                  <th>{{ 'admin.menuRoute' | translate }}</th>
                  <th>{{ 'admin.menuIcon' | translate }}</th>
                  <th>{{ 'admin.menuPermissions' | translate }}</th>
                  <th>{{ 'admin.menuParent' | translate }}</th>
                  <th>{{ 'admin.menuSortOrder' | translate }}</th>
                  <th>{{ 'admin.menuActive' | translate }}</th>
                </tr>
              </thead>
              <tbody>
                @for (item of menuItems(); track item.id) {
                  <tr>
                    <td><input class="form-control form-control-sm" [(ngModel)]="item.code" [name]="'code-' + item.id" /></td>
                    <td><input class="form-control form-control-sm" [(ngModel)]="item.labelKey" [name]="'label-' + item.id" /></td>
                    <td><input class="form-control form-control-sm" [(ngModel)]="item.route" [name]="'route-' + item.id" /></td>
                    <td><input class="form-control form-control-sm" [(ngModel)]="item.icon" [name]="'icon-' + item.id" placeholder="bi-people" /></td>
                    <td><input class="form-control form-control-sm" [(ngModel)]="item.requiredPermissions" [name]="'perm-' + item.id" /></td>
                    <td>
                      <select class="form-select form-select-sm" [(ngModel)]="item.parentId" [name]="'parent-' + item.id">
                        <option [ngValue]="null">{{ 'admin.menuTopLevel' | translate }}</option>
                        @for (parent of parentOptions(item.id); track parent.id) {
                          <option [ngValue]="parent.id">{{ parent.code }}</option>
                        }
                      </select>
                    </td>
                    <td style="width: 6rem"><input class="form-control form-control-sm" type="number" [(ngModel)]="item.sortOrder" [name]="'sort-' + item.id" /></td>
                    <td class="text-center">
                      <input class="form-check-input" type="checkbox" [(ngModel)]="item.isActive" [name]="'active-' + item.id" />
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    }

    @if (tab() === 'audit') {
      <div class="row g-3">
        <div class="col-lg-6">
          <div class="card border-0 shadow-sm">
            <div class="card-header bg-white fw-semibold">{{ 'admin.auditTrail' | translate }}</div>
            <table class="table mb-0 small">
              <thead><tr><th>{{ 'common.when' | translate }}</th><th>{{ 'admin.entity' | translate }}</th><th>{{ 'admin.action' | translate }}</th></tr></thead>
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
            <div class="card-header bg-white fw-semibold">{{ 'admin.activityLog' | translate }}</div>
            <table class="table mb-0 small">
              <thead><tr><th>{{ 'common.when' | translate }}</th><th>{{ 'admin.action' | translate }}</th><th>{{ 'common.details' | translate }}</th></tr></thead>
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
          <button class="btn btn-aganox mb-3" type="button" (click)="runBackup()" [disabled]="busy()">{{ 'admin.runBackupNow' | translate }}</button>
          <table class="table mb-0">
            <thead><tr><th>{{ 'common.date' | translate }}</th><th>{{ 'admin.file' | translate }}</th><th>{{ 'common.status' | translate }}</th><th></th></tr></thead>
            <tbody>
              @for (b of backups(); track b.id) {
                <tr>
                  <td>{{ b.createdAt }}</td>
                  <td class="small">{{ b.filePath }}</td>
                  <td>{{ ('status.' + b.status) | translate }}</td>
                  <td class="text-end">
                    @if (b.status === 'Completed') {
                      <button class="btn btn-sm btn-outline-danger" type="button" (click)="restore(b.filePath)">{{ 'common.restore' | translate }}</button>
                    }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="4" class="text-muted text-center py-3">{{ 'admin.noBackups' | translate }}</td></tr>
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
  private readonly navigation = inject(NavigationService);
  private readonly translate = inject(TranslateService);

  readonly tab = signal('settings');
  readonly settings = signal<AppSetting[]>([]);
  readonly menuItems = signal<NavigationMenuItemAdmin[]>([]);
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
    'Reports.View', 'Reports.Export', 'Admin.Settings', 'Admin.Backup', 'Admin.Audit', 'Users.Manage',
    'Attendance.View', 'Attendance.Manage', 'Attendance.Import',
    'Payroll.Config.View', 'Payroll.Config.Manage', 'Payroll.Salary.View', 'Payroll.Salary.Manage',
    'Payroll.Process', 'Payroll.Approve', 'Payroll.Lock', 'Payroll.Payslip.ViewOwn', 'Payroll.Payslip.ViewAll',
    'Payroll.Reports.View', 'Payroll.Reports.Export', 'Payroll.Arrears.Manage', 'Payroll.Audit.View'
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
      next: (s) => { this.settings.set(s); this.message.set(this.translate.instant('admin.settingsSaved')); },
      error: () => this.error.set(this.translate.instant('admin.settingsSaveFailed'))
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
        this.message.set(this.translate.instant('admin.permissionsUpdated', { role: updated.roleName }));
        this.loadPermissions();
      },
      error: (err) => this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'))
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
      next: () => { this.busy.set(false); this.message.set(this.translate.instant('admin.backupCompleted')); this.loadBackups(); },
      error: (err) => { this.busy.set(false); this.error.set(err.error?.errors?.[0] ?? this.translate.instant('admin.backupFailed')); }
    });
  }

  restore(path: string): void {
    if (!confirm(this.translate.instant('admin.restoreConfirm'))) return;
    this.admin.restoreBackup(path).subscribe({
      next: () => this.message.set(this.translate.instant('admin.restoreCompleted')),
      error: (err) => this.error.set(err.error?.errors?.[0] ?? this.translate.instant('admin.restoreFailed'))
    });
  }

  loadMenu(): void {
    this.navigation.getAdminMenu().subscribe({
      next: (items) => this.menuItems.set(items.sort((a, b) => a.sortOrder - b.sortOrder)),
      error: () => this.error.set(this.translate.instant('admin.menuSaveFailed'))
    });
  }

  saveMenu(): void {
    this.navigation.saveAdminMenu(this.menuItems()).subscribe({
      next: (items) => {
        this.menuItems.set(items.sort((a, b) => a.sortOrder - b.sortOrder));
        this.navigation.loadMenu().subscribe();
        this.message.set(this.translate.instant('admin.menuSaved'));
      },
      error: (err) => this.error.set(err.error?.errors?.[0] ?? this.translate.instant('admin.menuSaveFailed'))
    });
  }

  resetMenu(): void {
    if (!confirm(this.translate.instant('admin.menuResetConfirm'))) return;
    this.navigation.resetAdminMenu().subscribe({
      next: (items) => {
        this.menuItems.set(items.sort((a, b) => a.sortOrder - b.sortOrder));
        this.navigation.loadMenu().subscribe();
        this.message.set(this.translate.instant('admin.menuResetDone'));
      },
      error: (err) => this.error.set(err.error?.errors?.[0] ?? this.translate.instant('admin.menuResetFailed'))
    });
  }

  addMenuItem(): void {
    const nextSort = this.menuItems().reduce((max, item) => Math.max(max, item.sortOrder), 0) + 1;
    this.menuItems.set([
      ...this.menuItems(),
      {
        id: crypto.randomUUID(),
        code: `custom.${nextSort}`,
        parentId: null,
        labelKey: 'nav.profile',
        route: '/',
        icon: 'bi-link',
        requiredPermissions: null,
        sortOrder: nextSort,
        isActive: true
      }
    ]);
  }

  parentOptions(currentId: string): NavigationMenuItemAdmin[] {
    return this.menuItems().filter((item) => item.id !== currentId && !item.route);
  }
}
