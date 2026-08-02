import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell" data-testid="app-shell">
      <header class="shell__header">
        <a routerLink="/" class="shell__brand">
          <span class="shell__logo">A</span>
          <span>Aganox Rental</span>
        </a>

        @if (authService.isAuthenticated) {
          <nav class="shell__nav">
            <a routerLink="/browse" routerLinkActive="is-active" data-testid="nav-browse">Find a home</a>
            <a routerLink="/owner" routerLinkActive="is-active" data-testid="nav-owner">My listings</a>
          </nav>

          <div class="shell__user">
            <span class="shell__name">{{ authService.userName }}</span>
            <button type="button" class="btn btn--ghost btn--sm" data-testid="nav-sign-out" (click)="authService.logout()">
              Sign out
            </button>
          </div>
        }
      </header>

      <main class="shell__main">
        <router-outlet />
      </main>

      <footer class="shell__footer">
        <p>&copy; {{ year }} Aganox Rental. Find your next home or list your property.</p>
      </footer>
    </div>
  `,
  styles: `
    .shell {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .shell__header {
      position: sticky;
      top: 0;
      z-index: 50;
      display: flex;
      align-items: center;
      gap: 1.5rem;
      padding: 0.875rem 1.5rem;
      background: rgba(255, 255, 255, 0.92);
      backdrop-filter: blur(12px);
      border-bottom: 1px solid var(--border);
    }

    .shell__brand {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      font-weight: 700;
      font-size: 1.05rem;
      color: var(--text);
      text-decoration: none;
      margin-right: auto;
    }

    .shell__logo {
      width: 2rem;
      height: 2rem;
      border-radius: 0.5rem;
      background: linear-gradient(135deg, var(--primary), var(--primary-dark));
      color: #fff;
      display: grid;
      place-items: center;
      font-weight: 800;
    }

    .shell__nav {
      display: flex;
      gap: 0.25rem;
    }

    .shell__nav a {
      padding: 0.5rem 0.875rem;
      border-radius: var(--radius-md);
      color: var(--text-muted);
      text-decoration: none;
      font-weight: 500;
      font-size: 0.925rem;
      transition: background 0.15s, color 0.15s;
    }

    .shell__nav a:hover,
    .shell__nav a.is-active {
      background: color-mix(in srgb, var(--primary) 10%, transparent);
      color: var(--primary);
    }

    .shell__user {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .shell__name {
      font-size: 0.875rem;
      color: var(--text-muted);
      max-width: 12rem;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .shell__main {
      flex: 1;
      width: 100%;
      max-width: var(--content-max);
      margin: 0 auto;
      padding: 1.5rem;
    }

    .shell__footer {
      padding: 1.5rem;
      text-align: center;
      color: var(--text-muted);
      font-size: 0.85rem;
      border-top: 1px solid var(--border);
    }

    .shell__footer p {
      margin: 0;
    }

    @media (max-width: 768px) {
      .shell__header {
        flex-wrap: wrap;
        gap: 0.75rem;
      }

      .shell__nav {
        order: 3;
        width: 100%;
      }

      .shell__name {
        display: none;
      }
    }
  `
})
export class ShellComponent {
  protected readonly authService = inject(AuthService);
  protected readonly year = new Date().getFullYear();
}
