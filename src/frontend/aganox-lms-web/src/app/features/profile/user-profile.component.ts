import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
import { AppLanguage, LanguageService } from '../../core/language.service';
import { AppTheme, ThemeService } from '../../core/theme.service';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  template: `
    <header class="mb-4">
      <h1 class="h4 mb-1">{{ 'userProfile.title' | translate }}</h1>
      <p class="text-muted mb-0">{{ 'userProfile.subtitle' | translate }}</p>
    </header>

    <section class="gov-section mb-3">
      <div class="gov-section-head">
        <i class="bi bi-person-circle" aria-hidden="true"></i>
        {{ 'userProfile.account' | translate }}
      </div>
      <div class="gov-section-body">
        <dl class="row mb-0 small">
          <dt class="col-sm-3">{{ 'userProfile.displayName' | translate }}</dt>
          <dd class="col-sm-9">{{ auth.user()?.userName }}</dd>
          <dt class="col-sm-3">{{ 'common.email' | translate }}</dt>
          <dd class="col-sm-9">{{ auth.user()?.email }}</dd>
          <dt class="col-sm-3">{{ 'userProfile.roles' | translate }}</dt>
          <dd class="col-sm-9">
            @for (role of auth.user()?.roles ?? []; track role; let last = $last) {
              {{ ('roles.' + role) | translate }}@if (!last) {, }
            }
          </dd>
        </dl>
        <div class="mt-3">
          <a routerLink="/change-password" class="btn btn-sm btn-outline-secondary">
            <i class="bi bi-key me-1" aria-hidden="true"></i>
            {{ 'auth.changeTitle' | translate }}
          </a>
        </div>
      </div>
    </section>

    <section class="gov-section">
      <div class="gov-section-head">
        <i class="bi bi-sliders" aria-hidden="true"></i>
        {{ 'userProfile.preferences' | translate }}
      </div>
      <div class="gov-section-body">
        @if (saved()) {
          <div class="alert alert-success py-2 mb-3">{{ 'userProfile.saved' | translate }}</div>
        }

        <div class="mb-4">
          <label class="form-label d-block">{{ 'userProfile.language' | translate }}</label>
          <p class="text-muted small mb-2">{{ 'userProfile.languageHint' | translate }}</p>
          <div class="btn-group lang-switcher" role="group" [attr.aria-label]="'lang.switcherLabel' | translate">
            <button
              type="button"
              class="btn"
              [class.btn-aganox]="language.currentLang() === 'ta'"
              [class.btn-outline-secondary]="language.currentLang() !== 'ta'"
              (click)="setLanguage('ta')"
            >
              தமிழ்
            </button>
            <button
              type="button"
              class="btn"
              [class.btn-aganox]="language.currentLang() === 'en'"
              [class.btn-outline-secondary]="language.currentLang() !== 'en'"
              (click)="setLanguage('en')"
            >
              EN
            </button>
          </div>
        </div>

        <div>
          <label class="form-label d-block">{{ 'userProfile.theme' | translate }}</label>
          <p class="text-muted small mb-2">{{ 'userProfile.themeHint' | translate }}</p>
          <div class="btn-group theme-switcher" role="group" [attr.aria-label]="'userProfile.theme' | translate">
            <button
              type="button"
              class="btn"
              [class.btn-aganox]="theme.currentTheme() === 'light'"
              [class.btn-outline-secondary]="theme.currentTheme() !== 'light'"
              (click)="setTheme('light')"
            >
              <i class="bi bi-sun me-1" aria-hidden="true"></i>
              {{ 'userProfile.themeLight' | translate }}
            </button>
            <button
              type="button"
              class="btn"
              [class.btn-aganox]="theme.currentTheme() === 'dark'"
              [class.btn-outline-secondary]="theme.currentTheme() !== 'dark'"
              (click)="setTheme('dark')"
            >
              <i class="bi bi-moon-stars me-1" aria-hidden="true"></i>
              {{ 'userProfile.themeDark' | translate }}
            </button>
          </div>
        </div>
      </div>
    </section>
  `
})
export class UserProfileComponent {
  readonly auth = inject(AuthService);
  readonly language = inject(LanguageService);
  readonly theme = inject(ThemeService);
  readonly saved = signal(false);
  private hideSavedTimer: ReturnType<typeof setTimeout> | null = null;

  setLanguage(lang: AppLanguage): void {
    this.language.setLanguage(lang);
    this.flashSaved();
  }

  setTheme(value: AppTheme): void {
    this.theme.setTheme(value);
    this.flashSaved();
  }

  private flashSaved(): void {
    this.saved.set(true);
    if (this.hideSavedTimer) {
      clearTimeout(this.hideSavedTimer);
    }
    this.hideSavedTimer = setTimeout(() => this.saved.set(false), 2000);
  }
}
