import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <section class="auth-panel shadow-sm">
      <h1 class="h3 brand-mark mb-1">Aganox LMS</h1>
      <p class="text-muted mb-4">Sign in to manage leave.</p>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="mb-3">
          <label class="form-label" for="email">Email</label>
          <input id="email" class="form-control" type="email" formControlName="email" autocomplete="username" />
        </div>
        <div class="mb-3">
          <label class="form-label" for="password">Password</label>
          <input
            id="password"
            class="form-control"
            type="password"
            formControlName="password"
            autocomplete="current-password"
          />
        </div>

        @if (error()) {
          <div class="alert alert-danger py-2">{{ error() }}</div>
        }

        <button class="btn btn-aganox w-100" type="submit" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>
    </section>
  `
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['admin@aganox.local', [Validators.required, Validators.email]],
    password: ['ChangeMe!123', [Validators.required, Validators.minLength(6)]]
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl('/');
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Invalid email or password.');
      }
    });
  }
}
