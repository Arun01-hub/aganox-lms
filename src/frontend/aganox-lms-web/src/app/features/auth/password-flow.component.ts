import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
import { LanguageSwitcherComponent } from '../../core/language-switcher.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent],
  template: `
    <div class="auth-shell">
      <section class="auth-panel">
        <div class="auth-panel-head">
          <i class="bi bi-bank2" aria-hidden="true"></i>
          <h1>{{ 'app.name' | translate }}</h1>
          <div class="ms-auto">
            <app-language-switcher />
          </div>
        </div>
        <div class="auth-panel-body">
          <h2 class="h6 mb-2">{{ 'auth.forgotTitle' | translate }}</h2>
          <p class="auth-panel-tagline">{{ 'auth.forgotHint' | translate }}</p>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="mb-2">
              <label class="form-label" for="email">{{ 'common.email' | translate }}</label>
              <input id="email" class="form-control" type="email" formControlName="email" />
            </div>
            @if (message()) { <div class="alert alert-success py-2">{{ message() }}</div> }
            @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }
            <button class="btn btn-aganox w-100 mb-2" type="submit" [disabled]="form.invalid || loading()">{{ 'auth.sendResetLink' | translate }}</button>
            <a routerLink="/login" class="small">{{ 'common.backToSignIn' | translate }}</a>
          </form>
        </div>
      </section>
    </div>
  `
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly translate = inject(TranslateService);

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
        this.message.set(this.translate.instant('auth.resetSentDev'));
      },
      error: () => {
        this.loading.set(false);
        this.message.set(this.translate.instant('auth.resetSent'));
      }
    });
  }
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, LanguageSwitcherComponent],
  template: `
    <div class="auth-shell">
      <section class="auth-panel">
        <div class="auth-panel-head">
          <i class="bi bi-bank2" aria-hidden="true"></i>
          <h1>{{ 'app.name' | translate }}</h1>
          <div class="ms-auto">
            <app-language-switcher />
          </div>
        </div>
        <div class="auth-panel-body">
          <h2 class="h6 mb-2">{{ 'auth.resetTitle' | translate }}</h2>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="mb-2"><label class="form-label">{{ 'common.email' | translate }}</label><input class="form-control" type="email" formControlName="email" /></div>
            <div class="mb-2"><label class="form-label">{{ 'auth.resetToken' | translate }}</label><input class="form-control" formControlName="token" /></div>
            <div class="mb-2"><label class="form-label">{{ 'auth.newPassword' | translate }}</label><input class="form-control" type="password" formControlName="newPassword" /></div>
            @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }
            <button class="btn btn-aganox w-100 mb-2" type="submit" [disabled]="form.invalid || loading()">{{ 'auth.resetPassword' | translate }}</button>
            <a routerLink="/login" class="small">{{ 'common.backToSignIn' | translate }}</a>
          </form>
        </div>
      </section>
    </div>
  `
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

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
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('auth.resetFailed'));
      }
    });
  }
}

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, LanguageSwitcherComponent],
  template: `
    <div class="auth-shell">
      <section class="auth-panel">
        <div class="auth-panel-head">
          <i class="bi bi-bank2" aria-hidden="true"></i>
          <h1>{{ 'app.name' | translate }}</h1>
          <div class="ms-auto">
            <app-language-switcher />
          </div>
        </div>
        <div class="auth-panel-body">
          <h2 class="h6 mb-2">{{ 'auth.changeTitle' | translate }}</h2>
          <p class="auth-panel-tagline">{{ 'auth.changeHint' | translate }}</p>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="mb-2"><label class="form-label">{{ 'auth.currentPassword' | translate }}</label><input class="form-control" type="password" formControlName="currentPassword" /></div>
            <div class="mb-2"><label class="form-label">{{ 'auth.newPassword' | translate }}</label><input class="form-control" type="password" formControlName="newPassword" /></div>
            @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }
            <button class="btn btn-aganox w-100" type="submit" [disabled]="form.invalid || loading()">{{ 'auth.updatePassword' | translate }}</button>
          </form>
        </div>
      </section>
    </div>
  `
})
export class ChangePasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

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
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('auth.changeFailed'));
      }
    });
  }
}
