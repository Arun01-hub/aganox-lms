import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface NavigationMenuItem {
  id: string;
  code: string;
  labelKey: string;
  route?: string | null;
  icon?: string | null;
  children: NavigationMenuItem[];
}

export interface NavigationMenuItemAdmin {
  id: string;
  code: string;
  parentId?: string | null;
  labelKey: string;
  route?: string | null;
  icon?: string | null;
  requiredPermissions?: string | null;
  sortOrder: number;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  readonly menu = signal<NavigationMenuItem[]>([]);
  readonly loading = signal(false);

  loadMenu() {
    this.loading.set(true);
    return this.http.get<NavigationMenuItem[]>(`${this.base}/navigation/menu`).pipe(
      tap({
        next: (items) => {
          this.menu.set(items);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      })
    );
  }

  getAdminMenu() {
    return this.http.get<NavigationMenuItemAdmin[]>(`${this.base}/admin/navigation/menu`);
  }

  saveAdminMenu(items: NavigationMenuItemAdmin[]) {
    return this.http.put<NavigationMenuItemAdmin[]>(`${this.base}/admin/navigation/menu`, { items });
  }

  resetAdminMenu() {
    return this.http.post<NavigationMenuItemAdmin[]>(`${this.base}/admin/navigation/menu/reset`, {});
  }
}
