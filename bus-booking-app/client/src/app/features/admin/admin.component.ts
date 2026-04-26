import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="admin-container">
      <aside class="sidebar">
        <h2>Admin Panel</h2>
        <nav>
          <a routerLink="home" routerLinkActive="active">Dashboard Home</a>
          <a routerLink="routes" routerLinkActive="active">Route Management</a>
          <a routerLink="operators" routerLinkActive="active">Operator Approvals</a>
          <a routerLink="settings" routerLinkActive="active">Platform Settings</a>
        </nav>
      </aside>
      <main class="content">
        <router-outlet></router-outlet>
      </main>

      <!-- Global Admin Toast -->
      <div class="toast-container" *ngIf="toastService.toast().visible" [class]="toastService.toast().type">
        <div class="toast-content">
          <span class="icon">{{ toastService.toast().type === 'success' ? '✅' : 'ℹ️' }}</span>
          <span class="message">{{ toastService.toast().message }}</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-container {
      display: flex;
      min-height: calc(100vh - 70px);
      background: #f8fafc;
      position: relative;
    }
    .sidebar {
      width: 250px;
      background: #ffffff;
      border-right: 1px solid #e2e8f0;
      padding: 2rem 1.5rem;
    }
    .sidebar h2 {
      margin-top: 0;
      margin-bottom: 2rem;
      font-size: 1.2rem;
      color: #0f172a;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    nav {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }
    nav a {
      text-decoration: none;
      color: #475569;
      padding: 0.75rem 1rem;
      border-radius: 8px;
      font-weight: 500;
      transition: all 0.2s ease;
    }
    nav a:hover {
      background: #f1f5f9;
      color: #0f172a;
    }
    nav a.active {
      background: #eff6ff;
      color: #2563eb;
      font-weight: 600;
    }
    .content {
      flex: 1;
      padding: 2rem;
      overflow-y: auto;
    }

    /* Toast Styles */
    .toast-container {
      position: fixed;
      bottom: 2rem;
      right: 2rem;
      background: #0f172a;
      color: white;
      padding: 1rem 1.5rem;
      border-radius: 12px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      z-index: 2000;
      animation: slideUp 0.3s ease-out;
    }
    .toast-container.success { border-left: 4px solid #22c55e; }
    .toast-container.info { border-left: 4px solid #3b82f6; }
    .toast-content { display: flex; align-items: center; gap: 0.75rem; }
    .icon { font-size: 1.2rem; }
    .message { font-weight: 600; font-size: 0.9rem; }

    @keyframes slideUp {
      from { transform: translateY(100%); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }
  `]
})
export class AdminComponent {
  toastService = inject(ToastService);
}
