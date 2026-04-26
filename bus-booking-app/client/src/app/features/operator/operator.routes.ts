import { Routes } from '@angular/router';
import { OperatorComponent } from './operator.component';

export const OPERATOR_ROUTES: Routes = [
  {
    path: '',
    component: OperatorComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { 
        path: 'dashboard', 
        loadComponent: () => import('./operator-home/operator-home.component') 
      },
      { 
        path: 'fleet', 
        loadComponent: () => import('./fleet-management/fleet-management.component') 
      },
      { 
        path: 'schedules', 
        loadComponent: () => import('./schedule-management/schedule-management.component') 
      }
    ]
  }
];
