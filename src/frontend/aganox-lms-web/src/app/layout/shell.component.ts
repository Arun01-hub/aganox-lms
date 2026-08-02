import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <nav class="navbar navbar-expand-lg shell-nav px-3">
      <a class="navbar-brand brand-mark" routerLink="/">Aganox LMS</a>
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
