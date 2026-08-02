import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  template: `
    <header class="mb-4">
      <h1 class="h3 mb-1">Dashboard</h1>
      <p class="text-muted mb-0">
        Welcome, {{ auth.user()?.userName }}. Phase 1 foundation is ready.
      </p>
    </header>

    <div class="row g-3">
      <div class="col-md-4">
        <div class="p-3 border bg-white h-100">
          <div class="text-muted small">Roles</div>
          <div class="fw-semibold">{{ auth.user()?.roles?.join(', ') || '—' }}</div>
        </div>
      </div>
      <div class="col-md-8">
        <div class="p-3 border bg-white h-100">
          <div class="text-muted small">Permissions</div>
          <div class="small">{{ auth.user()?.permissions?.join(', ') || '—' }}</div>
        </div>
      </div>
    </div>
  `
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
}
