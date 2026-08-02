import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PropertyService } from '../../core/services/property.service';
import { AuthService } from '../../core/auth/auth.service';
import { Property } from '../../core/models/property.model';

@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe, DecimalPipe],
  template: `
    @if (property) {
      <a routerLink="/browse" class="back">&larr; Back to search</a>

      <article class="detail" data-testid="property-detail-page">
        <div class="detail__gallery">
          <img [src]="property.imageUrl" [alt]="property.title" />
        </div>

        <div class="detail__content">
          <div class="detail__head">
            <div>
              <p class="detail__location">{{ property.address.city }}, {{ property.address.state }}</p>
              <h1 data-testid="property-detail-title">{{ property.title }}</h1>
              <p class="detail__address">
                {{ property.address.line1 }} · {{ property.address.zip }}
              </p>
            </div>
            <div class="detail__price" data-testid="property-detail-price">
              {{ property.rentMonthly | currency:'USD':'symbol':'1.0-0' }}
              <span>/month</span>
            </div>
          </div>

          <div class="detail__badges">
            <span class="badge">{{ property.type }}</span>
            <span class="badge" [class.badge--muted]="property.status !== 'available'">
              {{ property.status }}
            </span>
            <span class="badge">Available {{ property.availableFrom | date:'mediumDate' }}</span>
          </div>

          <ul class="detail__meta">
            <li><strong>{{ property.bedrooms === 0 ? 'Studio' : property.bedrooms + ' bed' }}</strong></li>
            <li><strong>{{ property.bathrooms }} bath</strong></li>
            <li><strong>{{ property.sqft | number }} sqft</strong></li>
          </ul>

          <section class="detail__section">
            <h2>About this property</h2>
            <p data-testid="property-detail-description">{{ property.description }}</p>
          </section>

          @if (property.amenities.length) {
            <section class="detail__section">
              <h2>Amenities</h2>
              <ul class="amenities">
                @for (amenity of property.amenities; track amenity) {
                  <li>{{ amenity }}</li>
                }
              </ul>
            </section>
          }

          <section class="detail__section detail__owner">
            <h2>Listed by</h2>
            <p>{{ property.ownerName }}</p>
            <p class="muted">{{ property.ownerEmail }}</p>
          </section>

          @if (isOwner) {
            <div class="detail__actions">
              <a [routerLink]="['/owner/edit', property.id]" class="btn btn--secondary">Edit listing</a>
            </div>
          } @else if (property.status === 'available') {
            <div class="detail__cta">
              <p>Interested in this rental? Contact the owner to schedule a viewing.</p>
              <a [href]="contactMailto" class="btn btn--primary">Contact owner</a>
            </div>
          }
        </div>
      </article>
    } @else {
      <div class="not-found">
        <h1>Property not found</h1>
        <a routerLink="/browse" class="btn btn--primary">Browse listings</a>
      </div>
    }
  `,
  styles: `
    .back {
      display: inline-block;
      margin-bottom: 1rem;
      color: var(--text-muted);
      text-decoration: none;
      font-size: 0.9rem;
    }

    .back:hover {
      color: var(--primary);
    }

    .detail {
      display: grid;
      grid-template-columns: 1.1fr 1fr;
      gap: 2rem;
      align-items: start;
    }

    .detail__gallery img {
      width: 100%;
      border-radius: var(--radius-lg);
      aspect-ratio: 4 / 3;
      object-fit: cover;
      box-shadow: var(--shadow-md);
    }

    .detail__location {
      margin: 0 0 0.25rem;
      color: var(--primary);
      font-weight: 600;
      text-transform: uppercase;
      font-size: 0.8rem;
      letter-spacing: 0.06em;
    }

    h1 {
      margin: 0 0 0.375rem;
      font-size: 1.75rem;
      line-height: 1.2;
    }

    .detail__address {
      margin: 0;
      color: var(--text-muted);
    }

    .detail__head {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .detail__price {
      font-size: 1.75rem;
      font-weight: 800;
      color: var(--primary);
      white-space: nowrap;
    }

    .detail__price span {
      font-size: 0.95rem;
      font-weight: 500;
      color: var(--text-muted);
    }

    .detail__badges {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-bottom: 1rem;
    }

    .badge {
      background: color-mix(in srgb, var(--primary) 12%, transparent);
      color: var(--primary);
      padding: 0.25rem 0.625rem;
      border-radius: 999px;
      font-size: 0.8rem;
      font-weight: 600;
      text-transform: capitalize;
    }

    .badge--muted {
      background: var(--surface-muted);
      color: var(--text-muted);
    }

    .detail__meta {
      display: flex;
      gap: 1.5rem;
      margin: 0 0 1.5rem;
      padding: 1rem 0;
      border-top: 1px solid var(--border);
      border-bottom: 1px solid var(--border);
      list-style: none;
    }

    .detail__meta li {
      color: var(--text-muted);
    }

    .detail__section {
      margin-bottom: 1.5rem;
    }

    .detail__section h2 {
      margin: 0 0 0.625rem;
      font-size: 1.05rem;
    }

    .detail__section p {
      margin: 0;
      line-height: 1.65;
      color: var(--text-muted);
    }

    .amenities {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .amenities li {
      background: var(--surface-muted);
      padding: 0.375rem 0.75rem;
      border-radius: var(--radius-md);
      font-size: 0.875rem;
    }

    .detail__owner .muted {
      font-size: 0.875rem;
    }

    .detail__cta {
      background: color-mix(in srgb, var(--primary) 8%, var(--surface));
      border: 1px solid color-mix(in srgb, var(--primary) 20%, var(--border));
      border-radius: var(--radius-lg);
      padding: 1.25rem;
    }

    .detail__cta p {
      margin: 0 0 1rem;
      color: var(--text);
    }

    .detail__actions {
      display: flex;
      gap: 0.75rem;
    }

    .not-found {
      text-align: center;
      padding: 4rem 1rem;
    }

    @media (max-width: 900px) {
      .detail {
        grid-template-columns: 1fr;
      }

      .detail__head {
        flex-direction: column;
      }
    }
  `
})
export class PropertyDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly propertyService = inject(PropertyService);
  private readonly authService = inject(AuthService);

  protected property: Property | undefined;
  protected loading = true;

  constructor() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) {
        this.property = undefined;
        this.loading = false;
        return;
      }

      this.loading = true;
      this.propertyService.getById(id).subscribe(property => {
        this.property = property;
        this.loading = false;
      });
    });
  }

  protected get isOwner(): boolean {
    return !!this.property && this.property.ownerId === this.authService.userId;
  }

  protected get contactMailto(): string {
    if (!this.property) {
      return '#';
    }

    const subject = encodeURIComponent(`Inquiry: ${this.property.title}`);
    const body = encodeURIComponent(
      `Hi ${this.property.ownerName},\n\nI'm interested in your listing at ${this.property.address.line1}.\n\nThanks!`
    );
    return `mailto:${this.property.ownerEmail}?subject=${subject}&body=${body}`;
  }
}
