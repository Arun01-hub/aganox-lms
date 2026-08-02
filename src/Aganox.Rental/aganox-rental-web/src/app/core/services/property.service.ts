import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PropertyApiDto,
  UpsertPropertyApiRequest,
  mapPropertyFromApi
} from '../models/property-api.model';
import { Property, PropertySearchFilters, defaultFilters } from '../models/property.model';

@Injectable({ providedIn: 'root' })
export class PropertyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/properties`;

  search(filters: PropertySearchFilters, includeNonAvailable = false): Observable<Property[]> {
    let params = new HttpParams();

    if (filters.query.trim()) {
      params = params.set('query', filters.query.trim());
    }

    if (filters.city.trim()) {
      params = params.set('city', filters.city.trim());
    }

    if (filters.type) {
      params = params.set('type', filters.type);
    }

    if (filters.bedrooms !== null) {
      params = params.set('minBedrooms', filters.bedrooms.toString());
    }

    if (filters.minRent !== null) {
      params = params.set('minRent', filters.minRent.toString());
    }

    if (filters.maxRent !== null) {
      params = params.set('maxRent', filters.maxRent.toString());
    }

    if (filters.status) {
      params = params.set('status', filters.status);
    }

    params = params.set('availableOnly', (!includeNonAvailable).toString());

    return this.http
      .get<PropertyApiDto[]>(this.baseUrl, { params })
      .pipe(map(items => items.map(mapPropertyFromApi)));
  }

  getById(id: string): Observable<Property | undefined> {
    return this.http.get<PropertyApiDto>(`${this.baseUrl}/${id}`).pipe(
      map(mapPropertyFromApi),
      catchError(() => of(undefined))
    );
  }

  getMine(): Observable<Property[]> {
    return this.http
      .get<PropertyApiDto[]>(`${this.baseUrl}/mine`)
      .pipe(map(items => items.map(mapPropertyFromApi)));
  }

  getFeatured(limit = 3): Observable<Property[]> {
    return this.search(defaultFilters()).pipe(
      map(properties => properties.slice(0, limit))
    );
  }

  getDistinctCities(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/cities`);
  }

  create(request: UpsertPropertyApiRequest): Observable<Property> {
    return this.http
      .post<PropertyApiDto>(this.baseUrl, request)
      .pipe(map(mapPropertyFromApi));
  }

  update(id: string, request: UpsertPropertyApiRequest): Observable<Property> {
    return this.http
      .put<PropertyApiDto>(`${this.baseUrl}/${id}`, request)
      .pipe(map(mapPropertyFromApi));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
