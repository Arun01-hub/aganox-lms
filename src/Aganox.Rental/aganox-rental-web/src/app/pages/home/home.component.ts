import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { PropertyCardComponent } from '../../shared/components/property-card/property-card.component';
import { PropertySearchComponent } from '../../shared/components/property-search/property-search.component';
import { PropertyService } from '../../core/services/property.service';
import { Property, defaultFilters } from '../../core/models/property.model';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, PropertyCardComponent, PropertySearchComponent],
  template: `
    <section class="hero" data-testid="home-page">
      <div class="hero__content">
        <p class="hero__eyebrow">Rent smarter with Aganox</p>
        <h1>Find your next home or list a property in minutes</h1>
        <p class="hero__lead">
          Tenants discover verified listings with powerful search. Owners publish listings,
          manage availability, and reach qualified renters.
        </p>
        <div class="hero__actions">
          <a routerLink="/browse" class="btn btn--primary btn--lg" data-testid="hero-browse">Browse rentals</a>
          <a routerLink="/owner/new" class="btn btn--secondary btn--lg" data-testid="hero-list">List your property</a>
        </div>
      </div>
      <div class="hero__stats">
        <div class="stat">
          <strong>{{ availableCount }}</strong>
          <span>Available now</span>
        </div>
        <div class="stat">
          <strong>{{ cityCount }}</strong>
          <span>Cities</span>
        </div>
        <div class="stat">
          <strong>{{ typeCount }}</strong>
          <span>Property types</span>
        </div>
      </div>
    </section>

    <section class="section">
      <h2>Quick search</h2>
      <app-property-search
        [filters]="filters"
        [cities]="cities"
        [showAdvanced]="false"
        (search)="goToBrowse()"
      />
    </section>

    <section class="section">
      <div class="section__head">
        <h2>Featured listings</h2>
        <a routerLink="/browse" class="link">View all</a>
      </div>
      <div class="grid" data-testid="featured-listings">
        @for (property of featured; track property.id) {
          <app-property-card [property]="property" />
        }
      </div>
    </section>
  `,
  styles: `
    .hero {
      display: grid;
      grid-template-columns: 1.4fr 1fr;
      gap: 2rem;
      align-items: center;
      padding: 2rem 0 1rem;
    }

    .hero__eyebrow {
      margin: 0 0 0.5rem;
      font-size: 0.85rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: var(--primary);
    }

    h1 {
      margin: 0 0 1rem;
      font-size: clamp(1.75rem, 4vw, 2.75rem);
      line-height: 1.15;
      letter-spacing: -0.02em;
    }

    .hero__lead {
      margin: 0 0 1.5rem;
      color: var(--text-muted);
      line-height: 1.6;
      max-width: 36rem;
    }

    .hero__actions {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
    }

    .hero__stats {
      display: grid;
      gap: 0.75rem;
    }

    .stat {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      padding: 1.25rem 1.5rem;
      box-shadow: var(--shadow-sm);
    }

    .stat strong {
      display: block;
      font-size: 1.75rem;
      color: var(--primary);
      line-height: 1.2;
    }

    .stat span {
      color: var(--text-muted);
      font-size: 0.9rem;
    }

    .section {
      margin-top: 2.5rem;
    }

    .section h2 {
      margin: 0 0 1rem;
      font-size: 1.35rem;
    }

    .section__head {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .section__head h2 {
      margin: 0;
    }

    .link {
      color: var(--primary);
      text-decoration: none;
      font-weight: 600;
      font-size: 0.925rem;
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.25rem;
    }

    @media (max-width: 900px) {
      .hero {
        grid-template-columns: 1fr;
      }

      .hero__stats {
        grid-template-columns: repeat(3, 1fr);
      }
    }

    @media (max-width: 600px) {
      .hero__stats {
        grid-template-columns: 1fr;
      }
    }
  `
})
export class HomeComponent implements OnInit {
  private readonly propertyService = inject(PropertyService);
  private readonly router = inject(Router);

  protected filters = defaultFilters();
  protected featured: Property[] = [];
  protected cities: string[] = [];
  protected availableCount = 0;

  ngOnInit(): void {
    forkJoin({
      featured: this.propertyService.getFeatured(3),
      cities: this.propertyService.getDistinctCities(),
      available: this.propertyService.search(defaultFilters())
    }).subscribe({
      next: ({ featured, cities, available }) => {
        this.featured = featured;
        this.cities = cities;
        this.availableCount = available.length;
      }
    });
  }

  protected get cityCount(): number {
    return this.cities.length;
  }

  protected get typeCount(): number {
    return 5;
  }

  protected goToBrowse(): void {
    void this.router.navigate(['/browse'], {
      queryParams: {
        q: this.filters.query || null,
        city: this.filters.city || null
      }
    });
  }
}
