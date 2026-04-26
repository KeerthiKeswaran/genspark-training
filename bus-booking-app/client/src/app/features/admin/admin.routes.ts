import { Routes } from '@angular/router';
import { AdminComponent } from './admin.component';

export const adminRoutes: Routes = [
  {
    path: '',
    component: AdminComponent,
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { 
        path: 'home', 
        loadComponent: () => import('./admin-home/admin-home.component')
      },
      { 
        path: 'routes', 
        loadComponent: () => import('./location-master')
      },
      { 
        path: 'operators', 
        loadComponent: () => import('./operator-approvals')
      },
      { 
        path: 'settings', 
        loadComponent: () => import('./fee-settings')
      }
    ]
  }
];
