import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Property } from '../../../core/models/property.model';

@Component({
  selector: 'app-property-card',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DecimalPipe],
  template: `
    <article class="card" data-testid="property-card">
      <a [routerLink]="['/properties', property().id]" class="card__media">
        <img [src]="property().imageUrl" [alt]="property().title" loading="lazy" />
        <span class="card__badge">{{ typeLabel }}</span>
      </a>
      <div class="card__body">
        <div class="card__price">{{ property().rentMonthly | currency:'USD':'symbol':'1.0-0' }}<span>/mo</span></div>
        <h3 class="card__title">
          <a [routerLink]="['/properties', property().id]">{{ property().title }}</a>
        </h3>
        <p class="card__location">{{ property().address.city }}, {{ property().address.state }}</p>
        <ul class="card__meta">
          <li>{{ property().bedrooms === 0 ? 'Studio' : property().bedrooms + ' bd' }}</li>
          <li>{{ property().bathrooms }} ba</li>
          <li>{{ property().sqft | number }} sqft</li>
        </ul>
      </div>
    </article>
  `,
  styles: `
    .card {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      overflow: hidden;
      box-shadow: var(--shadow-sm);
      transition: transform 0.2s ease, box-shadow 0.2s ease;
      height: 100%;
      display: flex;
      flex-direction: column;
    }

    .card:hover {
      transform: translateY(-2px);
      box-shadow: var(--shadow-md);
    }

    .card__media {
      position: relative;
      display: block;
      aspect-ratio: 4 / 3;
      overflow: hidden;
    }

    .card__media img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      transition: transform 0.3s ease;
    }

    .card:hover .card__media img {
      transform: scale(1.04);
    }

    .card__badge {
      position: absolute;
      top: 0.75rem;
      left: 0.75rem;
      background: rgba(15, 23, 42, 0.75);
      color: #fff;
      font-size: 0.75rem;
      font-weight: 600;
      padding: 0.25rem 0.625rem;
      border-radius: 999px;
      text-transform: capitalize;
    }

    .card__body {
      padding: 1rem 1.125rem 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.375rem;
      flex: 1;
    }

    .card__price {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--primary);
    }

    .card__price span {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--text-muted);
    }

    .card__title {
      margin: 0;
      font-size: 1.05rem;
      line-height: 1.35;
    }

    .card__title a {
      color: var(--text);
      text-decoration: none;
    }

    .card__title a:hover {
      color: var(--primary);
    }

    .card__location {
      margin: 0;
      color: var(--text-muted);
      font-size: 0.9rem;
    }

    .card__meta {
      display: flex;
      gap: 0.75rem;
      margin: 0.5rem 0 0;
      padding: 0;
      list-style: none;
      font-size: 0.85rem;
      color: var(--text-muted);
    }
  `
})
export class PropertyCardComponent {
  readonly property = input.required<Property>();

  get typeLabel(): string {
    return this.property().type.replace('_', ' ');
  }
}
