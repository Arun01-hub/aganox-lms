import { Component, HostListener, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../core/auth.service';
import { NavigationMenuItem, NavigationService } from '../core/navigation.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  template: `
    <header class="gov-topbar">
      <a class="gov-topbar-brand" routerLink="/">
        <i class="bi bi-bank2" aria-hidden="true"></i>
        <span>{{ 'app.name' | translate }}</span>
      </a>
      <div class="gov-topbar-meta">
        <a class="gov-topbar-user" routerLink="/profile">
          <i class="bi bi-person-badge" aria-hidden="true"></i>
          {{ auth.user()?.email }}
        </a>
        <button class="btn btn-sm btn-outline-light" type="button" (click)="auth.logout()">
          <i class="bi bi-box-arrow-right" aria-hidden="true"></i>
          {{ 'nav.logout' | translate }}
        </button>
      </div>
    </header>

    <nav class="gov-menubar navbar-nav flex-row flex-wrap">
      @for (item of navigation.menu(); track item.id) {
        @if (item.children.length) {
          <div class="nav-item dropdown" [class.show]="openDropdown() === item.id">
            <button
              type="button"
              class="nav-link dropdown-toggle"
              [class.active]="isGroupActive(item)"
              (click)="toggleDropdown(item.id, $event)"
            >
              @if (item.icon) {
                <i [class]="'bi ' + item.icon" aria-hidden="true"></i>
              }
              {{ item.labelKey | translate }}
            </button>
            <ul class="dropdown-menu" [class.show]="openDropdown() === item.id">
              @for (child of item.children; track child.id) {
                <li>
                  <a
                    class="dropdown-item"
                    [routerLink]="child.route"
                    routerLinkActive="active"
                    (click)="closeDropdown()"
                  >
                    @if (child.icon) {
                      <i [class]="'bi ' + child.icon" aria-hidden="true"></i>
                    }
                    {{ child.labelKey | translate }}
                  </a>
                </li>
              }
            </ul>
          </div>
        } @else if (item.route) {
          <a
            class="nav-link"
            [routerLink]="item.route"
            routerLinkActive="active"
            [routerLinkActiveOptions]="linkActiveOptions(item.route)"
          >
            @if (item.icon) {
              <i [class]="'bi ' + item.icon" aria-hidden="true"></i>
            }
            {{ item.labelKey | translate }}
          </a>
        }
      }
    </nav>

    <main class="gov-content container-fluid">
      <router-outlet />
    </main>
  `
})
export class ShellComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly navigation = inject(NavigationService);
  private readonly router = inject(Router);
  readonly openDropdown = signal<string | null>(null);

  ngOnInit(): void {
    this.navigation.loadMenu().subscribe();
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.closeDropdown();
  }

  toggleDropdown(id: string, event: Event): void {
    event.stopPropagation();
    this.openDropdown.set(this.openDropdown() === id ? null : id);
  }

  closeDropdown(): void {
    this.openDropdown.set(null);
  }

  linkActiveOptions(route: string) {
    return route === '/' ? { exact: true } : { exact: false };
  }

  isGroupActive(item: NavigationMenuItem): boolean {
    const url = this.router.url;
    return item.children.some(
      (child) => child.route && (child.route === '/' ? url === '/' : url.startsWith(child.route))
    );
  }
}
