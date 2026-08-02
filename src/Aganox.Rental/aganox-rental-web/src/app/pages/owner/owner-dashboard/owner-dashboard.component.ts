import { CurrencyPipe } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PropertyService } from '../../../core/services/property.service';
import { Property } from '../../../core/models/property.model';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, EmptyStateComponent],
  template: `
    <header class="page-header" data-testid="owner-dashboard-page">
      <div>
        <h1>My listings</h1>
        <p>Manage properties you have listed for rent.</p>
      </div>
      <a routerLink="/owner/new" class="btn btn--primary" data-testid="owner-add-property">+ Add property</a>
    </header>

    @if (listings.length > 0) {
      <div class="table-wrap">
        <table class="table" data-testid="owner-listings-table">
          <thead>
            <tr>
              <th>Property</th>
              <th>Location</th>
              <th>Rent</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (property of listings; track property.id) {
              <tr>
                <td>
                  <a [routerLink]="['/properties', property.id]" class="table__title">{{ property.title }}</a>
                  <span class="table__type">{{ property.type }}</span>
                </td>
                <td>{{ property.address.city }}, {{ property.address.state }}</td>
                <td>{{ property.rentMonthly | currency:'USD':'symbol':'1.0-0' }}/mo</td>
                <td>
                  <span class="status" [class]="'status--' + property.status">{{ property.status }}</span>
                </td>
                <td class="table__actions">
                  <a [routerLink]="['/owner/edit', property.id]" class="btn btn--ghost btn--sm">Edit</a>
                  <button type="button" class="btn btn--ghost btn--sm btn--danger" (click)="remove(property)">
                    Delete
                  </button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    } @else {
      <app-empty-state
        data-testid="owner-empty-state"
        icon="📋"
        title="No listings yet"
        message="Create your first property listing to start receiving tenant inquiries."
      >
        <a routerLink="/owner/new" class="btn btn--primary">List a property</a>
      </app-empty-state>
    }
  `,
  styles: `
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
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

    .table-wrap {
      overflow-x: auto;
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      background: var(--surface);
      box-shadow: var(--shadow-sm);
    }

    .table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.925rem;
    }

    .table th,
    .table td {
      padding: 0.875rem 1rem;
      text-align: left;
      border-bottom: 1px solid var(--border);
    }

    .table th {
      background: var(--surface-muted);
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--text-muted);
    }

    .table tbody tr:last-child td {
      border-bottom: none;
    }

    .table__title {
      display: block;
      font-weight: 600;
      color: var(--text);
      text-decoration: none;
    }

    .table__title:hover {
      color: var(--primary);
    }

    .table__type {
      display: block;
      font-size: 0.8rem;
      color: var(--text-muted);
      text-transform: capitalize;
    }

    .table__actions {
      display: flex;
      gap: 0.375rem;
      white-space: nowrap;
    }

    .status {
      display: inline-block;
      padding: 0.2rem 0.55rem;
      border-radius: 999px;
      font-size: 0.75rem;
      font-weight: 700;
      text-transform: capitalize;
    }

    .status--available {
      background: #dcfce7;
      color: #166534;
    }

    .status--rented {
      background: #fef3c7;
      color: #92400e;
    }

    .status--draft {
      background: var(--surface-muted);
      color: var(--text-muted);
    }

    @media (max-width: 768px) {
      .page-header {
        flex-direction: column;
      }
    }
  `
})
export class OwnerDashboardComponent implements OnInit {
  private readonly propertyService = inject(PropertyService);

  protected listings: Property[] = [];

  ngOnInit(): void {
    this.refresh();
  }

  protected remove(property: Property): void {
    if (!confirm(`Delete "${property.title}"? This cannot be undone.`)) {
      return;
    }

    this.propertyService.delete(property.id).subscribe(() => this.refresh());
  }

  private refresh(): void {
    this.propertyService.getMine().subscribe(listings => {
      this.listings = listings;
    });
  }
}
