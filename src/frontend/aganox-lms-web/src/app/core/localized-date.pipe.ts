import { Pipe, PipeTransform, inject } from '@angular/core';
import { formatDate } from '@angular/common';
import { LanguageService } from './language.service';

@Pipe({
  name: 'localizedDate',
  standalone: true,
  pure: false
})
export class LocalizedDatePipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(value: string | Date | null | undefined, format = 'mediumDate'): string {
    if (value == null || value === '') {
      return '';
    }

    const date =
      typeof value === 'string'
        ? new Date(value.includes('T') ? value : `${value}T00:00:00`)
        : value;

    if (Number.isNaN(date.getTime())) {
      return String(value);
    }

    return formatDate(date, format, this.language.localeId());
  }
}
