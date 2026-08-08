import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AppLanguage, LanguageService } from './language.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div class="btn-group btn-group-sm lang-switcher" role="group" [attr.aria-label]="'lang.switcherLabel' | translate">
      <button
        type="button"
        class="btn"
        [class.btn-aganox]="language.currentLang() === 'ta'"
        [class.btn-outline-secondary]="language.currentLang() !== 'ta'"
        (click)="setLang('ta')"
      >
        தமிழ்
      </button>
      <button
        type="button"
        class="btn"
        [class.btn-aganox]="language.currentLang() === 'en'"
        [class.btn-outline-secondary]="language.currentLang() !== 'en'"
        (click)="setLang('en')"
      >
        EN
      </button>
    </div>
  `
})
export class LanguageSwitcherComponent {
  readonly language = inject(LanguageService);

  setLang(lang: AppLanguage): void {
    this.language.setLanguage(lang);
  }
}
