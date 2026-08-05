import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="auth-panel shadow-sm">
      <h1 class="h4 mb-3">Forgot password</h1>
      <p class="text-muted small">Enter your email. If an account exists, reset instructions will be sent.</p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="mb-3">
          <label class="form-label" for="email">Email</label>
          <input id="email" class="form-control" type="email" formControlName="email" />
        </div>
        @if (message()) { <div class="alert alert-success py-2">{{ message() }}</div> }
        @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }
        <button class="btn btn-aganox w-100 mb-2" type="submit" [disabled]="form.invalid || loading()">Send reset link</button>
        <a routerLink="/login" class="small">Back to sign in</a>
      </form>
    </section>
  `
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly loading = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set('');
    this.auth.forgotPassword(this.form.controls.email.value).subscribe({
      next: () => {
        this.loading.set(false);
        this.message.set('If your email is registered, reset instructions have been sent. Check the API log for the token in dev mode.');
      },
      error: () => {
        this.loading.set(false);
        this.message.set('If your email is registered, reset instructions have been sent.');
      }
    });
  }
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="auth-panel shadow-sm">
      <h1 class="h4 mb-3">Reset password</h1>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="mb-3"><label class="form-label">Email</label><input class="form-control" type="email" formControlName="email" /></div>
        <div class="mb-3"><label class="form-label">Reset token</label><input class="form-control" formControlName="token" /></div>
        <div class="mb-3"><label class="form-label">New password</label><input class="form-control" type="password" formControlName="newPassword" /></div>
        @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }
        <button class="btn btn-aganox w-100 mb-2" type="submit" [disabled]="form.invalid || loading()">Reset password</button>
        <a routerLink="/login" class="small">Back to sign in</a>
      </form>
    </section>
  `
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    token: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(): void {
    if (this.form.invalid) return;
    const { email, token, newPassword } = this.form.getRawValue();
    this.loading.set(true);
    this.auth.resetPassword(email, token, newPassword).subscribe({
      next: () => { this.loading.set(false); void this.router.navigateByUrl('/login'); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.errors?.[0] ?? 'Reset failed. Check token and try again.');
      }
    });
  }
}

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="auth-panel shadow-sm mx-auto" style="max-width:420px">
      <h1 class="h4 mb-3">Change password</h1>
      <p class="text-muted small">You must change your password before continuing.</p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="mb-3"><label class="form-label">Current password</label><input class="form-control" type="password" formControlName="currentPassword" /></div>
        <div class="mb-3"><label class="form-label">New password</label><input class="form-control" type="password" formControlName="newPassword" /></div>
        @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }
        <button class="btn btn-aganox w-100" type="submit" [disabled]="form.invalid || loading()">Update password</button>
      </form>
    </section>
  `
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error = signal('');

  readonly form = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(): void {
    const { currentPassword, newPassword } = this.form.getRawValue();
    this.loading.set(true);
    this.auth.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.loading.set(false);
        const user = this.auth.user();
        if (user) this.auth.user.set({ ...user, mustChangePassword: false });
        void this.router.navigateByUrl('/');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.errors?.[0] ?? 'Password change failed.');
      }
    });
  }
}
