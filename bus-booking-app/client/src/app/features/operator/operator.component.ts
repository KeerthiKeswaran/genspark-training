import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-operator',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="operator-layout">
      <aside class="sidebar">
        <div class="brand">
        </div>

        <nav class="nav-links">
          <a routerLink="dashboard" routerLinkActive="active" class="nav-item">
            Dashboard
          </a>
          <a routerLink="fleet" routerLinkActive="active" class="nav-item">
            My Fleet
          </a>
          <a routerLink="schedules" routerLinkActive="active" class="nav-item">
            Trips & Schedules
          </a>
        </nav>
      </aside>

      <main class="main-content">
        <header class="top-bar">
          <div class="search-box">
            <input type="text" placeholder="Search trips, buses...">
          </div>
        </header>
        
        <div class="content-area">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .operator-layout { display: flex; height: 100vh; background: #f8fafc; }
    
    .sidebar { width: 280px; background: white; border-right: 1px solid #e2e8f0; display: flex; flex-direction: column; }
    .brand { padding: 2rem; min-height: 72px; display: flex; align-items: center; }

    .nav-links { flex: 1; padding: 1rem; display: flex; flex-direction: column; gap: 0.5rem; }
    .nav-item { display: flex; align-items: center; gap: 0.75rem; padding: 0.85rem 1rem; text-decoration: none; color: #64748b; border-radius: 10px; font-weight: 500; transition: 0.2s; font-size: 0.95rem; }
    .nav-item:hover { background: #f1f5f9; color: #3b82f6; }
    .nav-item.active { background: #eff6ff; color: #3b82f6; }
    
    .main-content { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
    .top-bar { height: 72px; background: white; border-bottom: 1px solid #e2e8f0; display: flex; align-items: center; justify-content: space-between; padding: 0 2rem; }
    .search-box input { border: none; background: #f1f5f9; padding: 0.6rem 1rem; border-radius: 8px; width: 300px; font-size: 0.9rem; }
    .search-box input:focus { outline: 1px solid #3b82f6; }

    .content-area { flex: 1; overflow-y: auto; }
  `]
})
export class OperatorComponent {
  authService = inject(AuthService);
  router = inject(Router);

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
