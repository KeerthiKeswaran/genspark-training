import { Routes } from '@angular/router';
import { Login } from './components/login/login';
import { Home } from './components/home/home';
import { ProductList } from './components/product-list/product-list';
import { ProductDetails } from './components/product-details/product-details';
import { SearchComponent } from './components/search/search';
import { Profile } from './components/profile/profile';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: '',
    component: Home,
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', component: ProductList }, // Renders by default showing mixed products (Deals of the Day) - Unprotected
      { path: 'products', component: ProductList, canActivate: [authGuard] }, // Renders categorised product listings - Protected
      { path: 'products/:id', component: ProductDetails, canActivate: [authGuard] }, // Renders single product detailed view - Protected
      { path: 'search', component: SearchComponent, canActivate: [authGuard] }, // Renders search results - Protected
      { path: 'profile', component: Profile, canActivate: [authGuard] }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
