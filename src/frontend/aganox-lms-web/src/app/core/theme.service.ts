import { Injectable, signal } from '@angular/core';

export const THEME_STORAGE_KEY = 'aganox.lms.theme';
export const SUPPORTED_THEMES = ['light', 'dark'] as const;
export type AppTheme = (typeof SUPPORTED_THEMES)[number];

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly currentTheme = signal<AppTheme>('light');

  init(): void {
    const saved = localStorage.getItem(THEME_STORAGE_KEY) as AppTheme | null;
    const theme: AppTheme =
      saved && SUPPORTED_THEMES.includes(saved) ? saved : 'light';
    this.applyTheme(theme, false);
  }

  setTheme(theme: AppTheme): void {
    if (!SUPPORTED_THEMES.includes(theme)) {
      return;
    }

    this.applyTheme(theme, true);
  }

  private applyTheme(theme: AppTheme, persist: boolean): void {
    document.documentElement.setAttribute('data-theme', theme);
    if (persist) {
      localStorage.setItem(THEME_STORAGE_KEY, theme);
    }
    this.currentTheme.set(theme);
  }
}
