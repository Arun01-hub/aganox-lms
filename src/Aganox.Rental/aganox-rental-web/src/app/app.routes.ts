import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent)
      },
      {
        path: 'browse',
        loadComponent: () => import('./pages/browse/browse.component').then(m => m.BrowseComponent)
      },
      {
        path: 'properties/:id',
        loadComponent: () => import('./pages/property-detail/property-detail.component').then(m => m.PropertyDetailComponent)
      },
      {
        path: 'owner',
        loadComponent: () => import('./pages/owner/owner-dashboard/owner-dashboard.component').then(m => m.OwnerDashboardComponent)
      },
      {
        path: 'owner/new',
        loadComponent: () => import('./pages/owner/property-form/property-form.component').then(m => m.PropertyFormComponent)
      },
      {
        path: 'owner/edit/:id',
        loadComponent: () => import('./pages/owner/property-form/property-form.component').then(m => m.PropertyFormComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
