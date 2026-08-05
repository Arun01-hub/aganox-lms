import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar navbar-expand-lg shell-nav px-3">
      <a class="navbar-brand brand-mark" routerLink="/">Aganox LMS</a>
      <div class="navbar-nav me-auto ms-3 gap-1 flex-row">
        @if (auth.hasPermission('Employees.View')) {
          <a class="nav-link" routerLink="/employees" routerLinkActive="active">Employees</a>
          <a class="nav-link" routerLink="/departments" routerLinkActive="active">Departments</a>
        }
        @if (auth.hasPermission('LeaveTypes.Manage')) {
          <a class="nav-link" routerLink="/leave/types" routerLinkActive="active">Leave config</a>
        }
        @if (auth.hasPermission('Leave.Apply')) {
          <a class="nav-link" routerLink="/leave/requests" routerLinkActive="active">My leave</a>
          <a class="nav-link" routerLink="/leave/preview" routerLinkActive="active">Day preview</a>
        }
        @if (auth.hasPermission('Approvals.Act')) {
          <a class="nav-link" routerLink="/approvals" routerLinkActive="active">Approvals</a>
        }
        @if (auth.hasPermission('Reports.View')) {
          <a class="nav-link" routerLink="/reports" routerLinkActive="active">Reports</a>
        }
        @if (auth.hasPermission('Admin.Settings') || auth.hasPermission('Admin.Audit') || auth.hasPermission('Admin.Backup') || auth.hasPermission('Users.Manage')) {
          <a class="nav-link" routerLink="/admin" routerLinkActive="active">Admin</a>
        }
      </div>
      <div class="ms-auto d-flex align-items-center gap-3">
        <span class="small text-muted">{{ auth.user()?.email }}</span>
        <button class="btn btn-sm btn-outline-secondary" type="button" (click)="auth.logout()">
          Logout
        </button>
      </div>
    </nav>
    <main class="container py-4">
      <router-outlet />
    </main>
  `
})
export class ShellComponent {
  readonly auth = inject(AuthService);
}
