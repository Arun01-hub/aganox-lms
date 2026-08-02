import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PropertyCardComponent } from '../../shared/components/property-card/property-card.component';
import { PropertySearchComponent } from '../../shared/components/property-search/property-search.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PropertyService } from '../../core/services/property.service';
import { Property, PropertySearchFilters, defaultFilters } from '../../core/models/property.model';

@Component({
  selector: 'app-browse',
  standalone: true,
  imports: [PropertyCardComponent, PropertySearchComponent, EmptyStateComponent],
  template: `
    <header class="page-header" data-testid="browse-page">
      <h1>Find a rental</h1>
      <p>Search available properties by city, budget, size, and amenities.</p>
    </header>

    <app-property-search
      [filters]="filters"
      [cities]="cities"
      (search)="applySearch()"
    />

    <div class="results-bar">
      <p data-testid="browse-results-count">{{ results.length }} {{ results.length === 1 ? 'property' : 'properties' }} found</p>
    </div>

    @if (results.length > 0) {
      <div class="grid">
        @for (property of results; track property.id) {
          <app-property-card [property]="property" />
        }
      </div>
    } @else {
      <app-empty-state
        icon="🔍"
        title="No matches found"
        message="Try widening your search — remove filters or search a nearby city."
      />
    }
  `,
  styles: `
    .page-header {
      margin-bottom: 1.5rem;
    }

    .page-header h1 {
      margin: 0 0 0.375rem;
      font-size: 1.75rem;
    }

    .page-header p {
      margin: 0;
      color: var(--text-muted);
    }

    .results-bar {
      margin: 1.25rem 0;
    }

    .results-bar p {
      margin: 0;
      font-weight: 600;
      color: var(--text-muted);
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 1.25rem;
    }
  `
})
export class BrowseComponent implements OnInit {
  private readonly propertyService = inject(PropertyService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected filters: PropertySearchFilters = defaultFilters();
  protected results: Property[] = [];
  protected cities: string[] = [];

  ngOnInit(): void {
    this.propertyService.getDistinctCities().subscribe(cities => {
      this.cities = cities;
    });

    this.route.queryParamMap.subscribe(params => {
      this.filters = {
        ...defaultFilters(),
        query: params.get('q') ?? '',
        city: params.get('city') ?? ''
      };
      this.applySearch(false);
    });
  }

  protected applySearch(updateUrl = true): void {
    this.propertyService.search(this.filters).subscribe(results => {
      this.results = results;
    });

    if (updateUrl) {
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {
          q: this.filters.query || null,
          city: this.filters.city || null
        },
        queryParamsHandling: 'merge'
      });
    }
  }
}
