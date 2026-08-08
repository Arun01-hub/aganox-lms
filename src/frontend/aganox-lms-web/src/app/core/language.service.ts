import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

export const LANGUAGE_STORAGE_KEY = 'aganox.lms.lang';
export const SUPPORTED_LANGUAGES = ['ta', 'en'] as const;
export type AppLanguage = (typeof SUPPORTED_LANGUAGES)[number];

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);

  readonly currentLang = signal<AppLanguage>('ta');

  init(): Promise<void> {
    this.translate.setFallbackLang('ta');
    this.translate.addLangs([...SUPPORTED_LANGUAGES]);

    const saved = localStorage.getItem(LANGUAGE_STORAGE_KEY) as AppLanguage | null;
    const lang: AppLanguage =
      saved && SUPPORTED_LANGUAGES.includes(saved) ? saved : 'ta';

    return firstValueFrom(this.translate.use(lang)).then(() => {
      this.currentLang.set(lang);
      this.applyDocumentLang(lang);
    });
  }

  setLanguage(lang: AppLanguage): void {
    if (!SUPPORTED_LANGUAGES.includes(lang)) {
      return;
    }

    this.translate.use(lang);
    localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
    this.currentLang.set(lang);
    this.applyDocumentLang(lang);
  }

  localeId(): string {
    return this.currentLang() === 'ta' ? 'ta-IN' : 'en-US';
  }

  instant(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }

  private applyDocumentLang(lang: AppLanguage): void {
    document.documentElement.lang = lang === 'ta' ? 'ta' : 'en';
  }
}
