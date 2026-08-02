import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  AMENITY_OPTIONS,
  PROPERTY_TYPES,
  PropertyStatus,
  PropertyType
} from '../../../core/models/property.model';
import { PropertyService } from '../../../core/services/property.service';
import { AuthService } from '../../../core/auth/auth.service';
import { UpsertPropertyApiRequest } from '../../../core/models/property-api.model';

@Component({
  selector: 'app-property-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <header class="page-header">
      <div>
        <h1>{{ isEdit ? 'Edit listing' : 'List a property' }}</h1>
        <p>{{ isEdit ? 'Update your property details and availability.' : 'Add a new rental to reach tenants on Aganox.' }}</p>
      </div>
    </header>

    <form class="form" data-testid="property-form-page" [formGroup]="form" (ngSubmit)="submit()">
      <section class="form__section">
        <h2>Basics</h2>
        <div class="form__grid">
          <label class="field field--full">
            <span>Title</span>
            <input formControlName="title" data-testid="property-form-title" placeholder="e.g. Sunny 2BR near downtown" />
            @if (showError('title')) {
              <span class="field-error">{{ fieldError('title') }}</span>
            }
          </label>
          <label class="field field--full">
            <span>Description</span>
            <textarea formControlName="description" rows="4" placeholder="Describe the property, neighborhood, and highlights"></textarea>
            @if (showError('description')) {
              <span class="field-error">{{ fieldError('description') }}</span>
            }
          </label>
          <label class="field">
            <span>Type</span>
            <select formControlName="type">
              @for (option of propertyTypes; track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
          </label>
          <label class="field">
            <span>Status</span>
            <select formControlName="status">
              <option value="available">Available</option>
              <option value="rented">Rented</option>
              <option value="draft">Draft</option>
            </select>
          </label>
          <label class="field">
            <span>Monthly rent (USD)</span>
            <input type="number" formControlName="rentMonthly" min="0" />
            @if (showError('rentMonthly')) {
              <span class="field-error">{{ fieldError('rentMonthly') }}</span>
            }
          </label>
          <label class="field">
            <span>Available from</span>
            <input type="date" formControlName="availableFrom" />
            @if (showError('availableFrom')) {
              <span class="field-error">{{ fieldError('availableFrom') }}</span>
            }
          </label>
        </div>
      </section>

      <section class="form__section">
        <h2>Location</h2>
        <div class="form__grid" formGroupName="address">
          <label class="field field--full">
            <span>Street address</span>
            <input formControlName="line1" />
            @if (showError('line1', 'address')) {
              <span class="field-error">{{ fieldError('line1', 'address') }}</span>
            }
          </label>
          <label class="field">
            <span>City</span>
            <input formControlName="city" />
            @if (showError('city', 'address')) {
              <span class="field-error">{{ fieldError('city', 'address') }}</span>
            }
          </label>
          <label class="field">
            <span>State</span>
            <input
              formControlName="state"
              maxlength="2"
              placeholder="OR"
              autocapitalize="characters"
              (input)="normalizeState($event)"
            />
            <span class="field-hint">2-letter code, e.g. OR, TX, CA</span>
            @if (showError('state', 'address')) {
              <span class="field-error">{{ fieldError('state', 'address') }}</span>
            }
          </label>
          <label class="field">
            <span>ZIP</span>
            <input formControlName="zip" />
            @if (showError('zip', 'address')) {
              <span class="field-error">{{ fieldError('zip', 'address') }}</span>
            }
          </label>
        </div>
      </section>

      <section class="form__section">
        <h2>Details</h2>
        <div class="form__grid">
          <label class="field">
            <span>Bedrooms</span>
            <input type="number" formControlName="bedrooms" min="0" />
          </label>
          <label class="field">
            <span>Bathrooms</span>
            <input type="number" formControlName="bathrooms" min="0" step="0.5" />
          </label>
          <label class="field">
            <span>Square feet</span>
            <input type="number" formControlName="sqft" min="1" />
            @if (showError('sqft')) {
              <span class="field-error">{{ fieldError('sqft') }}</span>
            }
          </label>
          <label class="field field--full">
            <span>Image URL</span>
            <input formControlName="imageUrl" placeholder="https://..." />
            @if (showError('imageUrl')) {
              <span class="field-error">{{ fieldError('imageUrl') }}</span>
            }
          </label>
        </div>
      </section>

      <section class="form__section">
        <h2>Amenities</h2>
        <div class="amenities">
          @for (amenity of amenityOptions; track amenity) {
            <label class="checkbox">
              <input
                type="checkbox"
                [checked]="selectedAmenities.has(amenity)"
                (change)="toggleAmenity(amenity, $event)"
              />
              <span>{{ amenity }}</span>
            </label>
          }
        </div>
      </section>

      <div class="form__actions">
        @if (form.invalid && form.touched) {
          <p class="form__validation-summary" data-testid="property-form-validation-summary">
            Complete all required fields to publish. Check highlighted errors above.
          </p>
        }
        <a routerLink="/owner" class="btn btn--ghost">Cancel</a>
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid">
          {{ isEdit ? 'Save changes' : 'Publish listing' }}
        </button>
      </div>
    </form>
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

    .form {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-sm);
      overflow: hidden;
    }

    .form__section {
      padding: 1.5rem;
      border-bottom: 1px solid var(--border);
    }

    .form__section h2 {
      margin: 0 0 1rem;
      font-size: 1rem;
    }

    .form__grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 1rem;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }

    .field--full {
      grid-column: 1 / -1;
    }

    .field span {
      font-size: 0.8rem;
      font-weight: 600;
      color: var(--text-muted);
    }

    .field-hint {
      font-size: 0.75rem;
      color: var(--text-muted);
    }

    .field-error {
      font-size: 0.75rem;
      color: #b91c1c;
    }

    .field input.ng-invalid.ng-touched,
    .field select.ng-invalid.ng-touched,
    .field textarea.ng-invalid.ng-touched {
      border-color: #fca5a5;
    }

    .field input,
    .field select,
    .field textarea {
      padding: 0.7rem 0.85rem;
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      background: var(--surface-elevated);
      font: inherit;
      color: var(--text);
    }

    .field textarea {
      resize: vertical;
      min-height: 6rem;
    }

    .amenities {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
      gap: 0.625rem;
    }

    .checkbox {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.9rem;
      cursor: pointer;
    }

    .form__actions {
      display: flex;
      justify-content: flex-end;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.75rem;
      padding: 1.25rem 1.5rem;
      background: var(--surface-muted);
    }

    .form__validation-summary {
      flex: 1 1 100%;
      margin: 0;
      font-size: 0.875rem;
      color: #b45309;
    }

    @media (max-width: 640px) {
      .form__grid {
        grid-template-columns: 1fr;
      }
    }
  `
})
export class PropertyFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly propertyService = inject(PropertyService);
  private readonly authService = inject(AuthService);

  protected readonly propertyTypes = PROPERTY_TYPES;
  protected readonly amenityOptions = AMENITY_OPTIONS;
  protected selectedAmenities = new Set<string>();
  protected isEdit = false;
  private propertyId: string | null = null;

  protected form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(5)]],
    description: ['', [Validators.required, Validators.minLength(20)]],
    type: ['apartment' as PropertyType, Validators.required],
    status: ['available' as PropertyStatus, Validators.required],
    rentMonthly: [1500, [Validators.required, Validators.min(0)]],
    availableFrom: [new Date().toISOString().slice(0, 10), Validators.required],
    bedrooms: [1, [Validators.required, Validators.min(0)]],
    bathrooms: [1, [Validators.required, Validators.min(0)]],
    sqft: [800, [Validators.required, Validators.min(1)]],
    imageUrl: [
      'https://images.unsplash.com/photo-1560448204-e02f11c45772?w=800&q=80',
      Validators.required
    ],
    address: this.fb.nonNullable.group({
      line1: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(2)]],
      zip: ['', Validators.required]
    })
  });

  ngOnInit(): void {
    this.propertyId = this.route.snapshot.paramMap.get('id');
    this.isEdit = !!this.propertyId;

    if (!this.propertyId) {
      return;
    }

    this.propertyService.getById(this.propertyId).subscribe(property => {
      if (!property || property.ownerId !== this.authService.userId) {
        void this.router.navigate(['/owner']);
        return;
      }

      this.form.patchValue({
        title: property.title,
        description: property.description,
        type: property.type,
        status: property.status,
        rentMonthly: property.rentMonthly,
        availableFrom: property.availableFrom,
        bedrooms: property.bedrooms,
        bathrooms: property.bathrooms,
        sqft: property.sqft,
        imageUrl: property.imageUrl,
        address: property.address
      });
      this.selectedAmenities = new Set(property.amenities);
    });
  }

  protected toggleAmenity(amenity: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      this.selectedAmenities.add(amenity);
    } else {
      this.selectedAmenities.delete(amenity);
    }
  }

  protected normalizeState(event: Event): void {
    const input = event.target as HTMLInputElement;
    const normalized = input.value.toUpperCase().replace(/[^A-Z]/g, '').slice(0, 2);
    if (input.value !== normalized) {
      input.value = normalized;
    }

    this.form.controls.address.controls.state.setValue(normalized);
  }

  protected showError(name: string, group?: string): boolean {
    const control = group ? this.form.get(`${group}.${name}`) : this.form.get(name);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  protected fieldError(name: string, group?: string): string {
    const control = group ? this.form.get(`${group}.${name}`) : this.form.get(name);
    if (!control?.errors) {
      return 'This field is invalid.';
    }

    if (control.errors['required']) {
      return 'This field is required.';
    }

    if (control.errors['minlength']) {
      const required = control.errors['minlength'].requiredLength;
      return `Enter at least ${required} characters.`;
    }

    if (control.errors['min']) {
      return `Value must be at least ${control.errors['min'].min}.`;
    }

    return 'This field is invalid.';
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request: UpsertPropertyApiRequest = {
      title: value.title,
      description: value.description,
      type: value.type,
      status: value.status,
      address: value.address,
      rentMonthly: value.rentMonthly,
      bedrooms: value.bedrooms,
      bathrooms: value.bathrooms,
      sqft: value.sqft,
      amenities: [...this.selectedAmenities],
      imageUrl: value.imageUrl,
      availableFrom: value.availableFrom
    };

    if (this.isEdit && this.propertyId) {
      this.propertyService.update(this.propertyId, request).subscribe(() => {
        void this.router.navigate(['/owner']);
      });
      return;
    }

    this.propertyService.create(request).subscribe(() => {
      void this.router.navigate(['/owner']);
    });
  }
}
