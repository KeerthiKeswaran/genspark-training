import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { HomeComponent } from './features/home/home.component';
import { BusListingComponent } from './features/search/bus-listing/bus-listing.component';
import { authGuard } from './core/guards/auth.guard';
import { rootGuard } from './core/guards/root.guard';

export const routes: Routes = [
  // Root: redirect based on session
  { path: '', canActivate: [rootGuard], component: HomeComponent },

  // Auth pages (accessible only when NOT logged in)
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  { path: 'search', component: BusListingComponent },
  { 
    path: 'booking', 
    canActivate: [authGuard],
    children: [
      { path: 'seats/:id', loadComponent: () => import('./features/booking/seat-layout/seat-layout.component').then(m => m.SeatLayoutComponent) },
      { path: 'passengers', loadComponent: () => import('./features/booking/passenger-form/passenger-form.component').then(m => m.PassengerFormComponent) },
      { path: 'payment', loadComponent: () => import('./features/booking/payment/payment.component').then(m => m.PaymentComponent) },
      { path: 'history', loadComponent: () => import('./features/booking/history/history.component').then(m => m.HistoryComponent) },
    ]
  },
  { 
    path: 'admin', 
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes) 
  },
  {
    path: 'operator',
    loadChildren: () => import('./features/operator/operator.routes').then(m => m.OPERATOR_ROUTES)
  },
  { path: 'home', component: HomeComponent },
  { path: '**', redirectTo: 'home' }
];

