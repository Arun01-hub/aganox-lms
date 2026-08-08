import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
import { LanguageSwitcherComponent } from '../../core/language-switcher.component';

@Component({
  selector: 'app-login',
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
          <p class="auth-panel-tagline">{{ 'app.tagline' | translate }}</p>

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="mb-2">
              <label class="form-label" for="email">
                <i class="bi bi-envelope me-1" aria-hidden="true"></i>{{ 'common.email' | translate }}
              </label>
              <input id="email" class="form-control" type="email" formControlName="email" autocomplete="username" />
            </div>
            <div class="mb-2">
              <label class="form-label" for="password">
                <i class="bi bi-key me-1" aria-hidden="true"></i>{{ 'common.password' | translate }}
              </label>
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

            <button class="btn btn-aganox w-100 mt-2" type="submit" [disabled]="form.invalid || loading()">
              <i class="bi bi-box-arrow-in-right me-1" aria-hidden="true"></i>
              {{ loading() ? ('auth.signingIn' | translate) : ('auth.signIn' | translate) }}
            </button>
            <div class="text-center mt-2">
              <a routerLink="/forgot-password" class="small">{{ 'auth.forgotPassword' | translate }}</a>
            </div>
          </form>
        </div>
      </section>
    </div>
  `
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: (response) => {
        this.loading.set(false);
        void this.router.navigateByUrl(response.user.mustChangePassword ? '/change-password' : '/');
      },
      error: () => {
        this.loading.set(false);
        this.error.set(this.translate.instant('auth.invalidCredentials'));
      }
    });
  }
}
