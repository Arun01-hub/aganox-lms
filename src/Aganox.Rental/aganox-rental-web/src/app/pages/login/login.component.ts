import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  template: `
    <section class="login" data-testid="login-page">
      <div class="login__card">
        <div class="login__brand">
          <span class="login__logo">A</span>
          <h1>Welcome to Aganox Rental</h1>
        </div>
        <p class="login__lead">
          Sign in to search rentals as a tenant or list and manage your properties as an owner.
        </p>
        <ul class="login__features">
          <li>Browse available homes with smart filters</li>
          <li>List properties and manage availability</li>
          <li>Secure sign-in powered by Aganox Identity</li>
        </ul>
        <button type="button" class="btn btn--primary btn--lg btn--block" data-testid="login-sign-in-button" (click)="authService.login()">
          Sign in with Aganox
        </button>
        <p class="login__hint">
          Dev account: <code>admin&#64;aganox.local</code> / <code>ChangeMe123!</code>
        </p>
      </div>
    </section>
  `,
  styles: `
    .login {
      min-height: calc(100vh - 8rem);
      display: grid;
      place-items: center;
      padding: 2rem 0;
    }

    .login__card {
      width: 100%;
      max-width: 28rem;
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      padding: 2rem;
      box-shadow: var(--shadow-md);
    }

    .login__brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 1rem;
    }

    .login__logo {
      width: 2.5rem;
      height: 2.5rem;
      border-radius: 0.625rem;
      background: linear-gradient(135deg, var(--primary), var(--primary-dark));
      color: #fff;
      display: grid;
      place-items: center;
      font-weight: 800;
      font-size: 1.1rem;
    }

    h1 {
      margin: 0;
      font-size: 1.35rem;
      line-height: 1.25;
    }

    .login__lead {
      margin: 0 0 1.25rem;
      color: var(--text-muted);
      line-height: 1.55;
    }

    .login__features {
      margin: 0 0 1.5rem;
      padding-left: 1.25rem;
      color: var(--text-muted);
      font-size: 0.925rem;
      line-height: 1.7;
    }

    .login__hint {
      margin: 1.25rem 0 0;
      font-size: 0.8rem;
      color: var(--text-muted);
      text-align: center;
    }

    code {
      background: var(--surface-muted);
      padding: 0.1rem 0.35rem;
      border-radius: 0.25rem;
      font-size: 0.75rem;
    }
  `
})
export class LoginComponent {
  protected readonly authService = inject(AuthService);
}
