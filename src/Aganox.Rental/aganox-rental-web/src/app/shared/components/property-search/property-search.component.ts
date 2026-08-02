import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  PROPERTY_TYPES,
  PropertySearchFilters,
  defaultFilters
} from '../../../core/models/property.model';

@Component({
  selector: 'app-property-search',
  standalone: true,
  imports: [FormsModule],
  template: `
    <form class="search" (ngSubmit)="search.emit(filters)">
      <div class="search__row search__row--primary">
        <label class="field field--grow">
          <span>Search</span>
          <input
            type="search"
            name="query"
            data-testid="search-query"
            [(ngModel)]="filters.query"
            placeholder="Neighborhood, keyword, amenity..."
          />
        </label>
        <label class="field">
          <span>City</span>
          <input
            type="text"
            name="city"
            data-testid="search-city"
            [(ngModel)]="filters.city"
            [attr.list]="'cities-' + listId"
            placeholder="Any city"
          />
          <datalist [id]="'cities-' + listId">
            @for (city of cities; track city) {
              <option [value]="city"></option>
            }
          </datalist>
        </label>
        <button type="submit" class="btn btn--primary" data-testid="search-submit">Search</button>
      </div>

      @if (showAdvanced) {
        <div class="search__row search__row--advanced">
          <label class="field">
            <span>Type</span>
            <select name="type" [(ngModel)]="filters.type">
              <option value="">Any type</option>
              @for (option of propertyTypes; track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
          </label>
          <label class="field">
            <span>Min beds</span>
            <select name="bedrooms" [(ngModel)]="filters.bedrooms">
              <option [ngValue]="null">Any</option>
              <option [ngValue]="0">Studio</option>
              <option [ngValue]="1">1+</option>
              <option [ngValue]="2">2+</option>
              <option [ngValue]="3">3+</option>
              <option [ngValue]="4">4+</option>
            </select>
          </label>
          <label class="field">
            <span>Min rent</span>
            <input type="number" name="minRent" [(ngModel)]="filters.minRent" placeholder="0" min="0" />
          </label>
          <label class="field">
            <span>Max rent</span>
            <input type="number" name="maxRent" [(ngModel)]="filters.maxRent" placeholder="No max" min="0" />
          </label>
          <button type="button" class="btn btn--ghost" (click)="clearFilters()">Clear</button>
        </div>
      }
    </form>
  `,
  styles: `
    .search {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      padding: 1.25rem;
      box-shadow: var(--shadow-sm);
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .search__row {
      display: grid;
      gap: 0.75rem;
      align-items: end;
    }

    .search__row--primary {
      grid-template-columns: 2fr 1fr auto;
    }

    .search__row--advanced {
      grid-template-columns: repeat(4, 1fr) auto;
      padding-top: 0.25rem;
      border-top: 1px solid var(--border);
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }

    .field--grow {
      min-width: 0;
    }

    .field span {
      font-size: 0.8rem;
      font-weight: 600;
      color: var(--text-muted);
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    .field input,
    .field select {
      width: 100%;
      padding: 0.7rem 0.85rem;
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      background: var(--surface-elevated);
      color: var(--text);
      font: inherit;
    }

    .field input:focus,
    .field select:focus {
      outline: 2px solid color-mix(in srgb, var(--primary) 35%, transparent);
      border-color: var(--primary);
    }

    @media (max-width: 900px) {
      .search__row--primary,
      .search__row--advanced {
        grid-template-columns: 1fr;
      }
    }
  `
})
export class PropertySearchComponent {
  @Input({ required: true }) filters!: PropertySearchFilters;
  @Input() cities: string[] = [];
  @Input() showAdvanced = true;

  @Output() search = new EventEmitter<PropertySearchFilters>();

  readonly propertyTypes = PROPERTY_TYPES;
  readonly listId = crypto.randomUUID().slice(0, 8);

  clearFilters(): void {
    Object.assign(this.filters, defaultFilters());
    this.search.emit(this.filters);
  }
}
