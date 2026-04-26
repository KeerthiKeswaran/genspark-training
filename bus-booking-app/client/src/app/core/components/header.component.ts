import { Component, inject, signal, HostListener, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { AuthResponse } from '../models/auth.models';
import { NotificationService } from '../services/notification.service';
import { OperatorService } from '../services/operator.service';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <header class="header">
      <div class="logo" routerLink="/">BUSBOOK</div>
      
      <div class="nav-actions">
        <ng-container *ngIf="user; else authButtons">
          <div class="nav-links">
            <a *ngIf="isCustomer()" routerLink="/booking/history" class="nav-link">Bookings</a>
            <span *ngIf="isOperator()" class="operator-panel-label">Operator Panel</span>
            <button *ngIf="isOperator() && (user.status === 'Rejected' || user.status === 'Re-Approval Pending')" 
                    (click)="requestReReview()" 
                    [disabled]="user.status === 'Re-Approval Pending'"
                    [class.btn-pending]="user.status === 'Re-Approval Pending'"
                    class="btn-re-request">
              {{ user.status === 'Re-Approval Pending' ? 'Re-Approval Pending' : 'Re-request Approval' }}
            </button>
            <a *ngIf="isAdmin()" routerLink="/admin" class="nav-link admin-link">Admin Dashboard</a>
          </div>

          <div class="notification-container">
            <div class="bell-icon" (click)="toggleNotifications($event)">
              <span class="icon">🔔</span>
              <span class="badge" *ngIf="notificationService.unreadCount() > 0">{{ notificationService.unreadCount() }}</span>
            </div>
            
            <div class="notification-dropdown" *ngIf="showNotifications()">
              <div class="notif-header">
                <h3>Notifications</h3>
                <button class="btn-clear-all" 
                        *ngIf="notificationService.notifications().length > 0"
                        (click)="clearAllNotifications($event)">
                  Clear All
                </button>
              </div>
              <div class="notif-list">
                <div *ngFor="let n of notificationService.notifications()" 
                     class="notif-item" 
                     [class.unread]="!n.isRead"
                     (click)="notificationService.markAsRead(n.id)">
                  <div class="notif-title">{{ n.title }}</div>
                  <div class="notif-msg">{{ n.message }}</div>
                  <div class="notif-time">{{ n.createdAt | date:'short' }}</div>
                </div>
                <div *ngIf="notificationService.notifications().length === 0" class="empty-notif">
                  No new notifications
                </div>
              </div>
            </div>
          </div>

          <div class="profile-container">
            <div class="profile-circle" (click)="toggleDropdown($event)">
              {{ getInitials() }}
            </div>
            
            <div class="dropdown" *ngIf="showDropdown()">
              <div class="user-info">
                <span class="name">{{ user.fullName }}</span>
                <span class="role">{{ getRoleName() }}</span>
              </div>
              <hr>
              <button (click)="logout()">Logout</button>
            </div>
          </div>
        </ng-container>
        
        <ng-template #authButtons>
          <div class="auth-buttons">
            <a routerLink="/login" class="btn-text">Login</a>
            <a routerLink="/register" class="btn-solid">Sign Up</a>
          </div>
        </ng-template>
      </div>
    </header>
  `,
  styles: [`
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 4rem;
      background: #fff;
      border-bottom: 1px solid #eee;
      position: sticky;
      top: 0;
      z-index: 1000;
      box-shadow: 0 2px 10px rgba(0,0,0,0.02);
    }
    .logo { font-size: 1.5rem; font-weight: 900; letter-spacing: -0.05em; cursor: pointer; }
    
    .nav-actions { display: flex; align-items: center; gap: 2rem; }
    .nav-links { display: flex; align-items: center; gap: 1.5rem; }
    .nav-link { font-weight: 700; color: #555; text-decoration: none; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.1em; transition: color 0.2s; }
    .nav-link:hover { color: #000; }
    .admin-link { color: #2563eb; }
    .admin-link:hover { color: #1d4ed8; }
    
    .operator-panel-label {
      font-weight: 700;
      color: #2563eb;
      font-size: 0.85rem;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      padding: 0.4rem 0.8rem;
      background: #eff6ff;
      border-radius: 6px;
    }

    .notification-container { position: relative; }
    .bell-icon { font-size: 1.2rem; cursor: pointer; position: relative; padding: 0.5rem; border-radius: 50%; transition: background 0.2s; }
    .bell-icon:hover { background: #f8fafc; }
    .badge { position: absolute; top: 0; right: 0; background: #ef4444; color: white; font-size: 0.65rem; padding: 0.1rem 0.4rem; border-radius: 10px; border: 2px solid white; }
    
    .notification-dropdown { position: absolute; top: 45px; right: -50px; background: white; width: 320px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); border-radius: 16px; border: 1px solid #f1f5f9; overflow: hidden; z-index: 1001; }
    .notif-header { padding: 1rem; border-bottom: 1px solid #f1f5f9; background: #f8fafc; display: flex; justify-content: space-between; align-items: center; }
    .notif-header h3 { margin: 0; font-size: 1rem; color: #0f172a; }
    .btn-clear-all { background: transparent; border: none; color: #3b82f6; font-size: 0.75rem; font-weight: 700; cursor: pointer; padding: 0.25rem 0.5rem; border-radius: 4px; }
    .btn-clear-all:hover { background: #eff6ff; }
    .notif-list { max-height: 400px; overflow-y: auto; }
    .notif-item { padding: 1rem; border-bottom: 1px solid #f8fafc; cursor: pointer; transition: background 0.2s; }
    .notif-item:hover { background: #f8fafc; }
    .notif-item.unread { background: #eff6ff; border-left: 3px solid #3b82f6; }
    .notif-title { font-weight: 700; font-size: 0.9rem; color: #1e293b; margin-bottom: 0.25rem; }
    .notif-msg { font-size: 0.85rem; color: #64748b; line-height: 1.4; }
    .notif-time { font-size: 0.75rem; color: #94a3b8; margin-top: 0.5rem; }
    .empty-notif { padding: 2rem; text-align: center; color: #94a3b8; font-size: 0.9rem; }

    .auth-buttons { display: flex; align-items: center; gap: 1.5rem; }
    .btn-text { text-decoration: none; color: #000; font-weight: 700; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.1em; }
    .btn-solid { background: #000; color: #fff; padding: 0.7rem 1.5rem; font-weight: 700; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.1em; text-decoration: none; border-radius: 30px; transition: background 0.2s; }
    .btn-solid:hover { background: #222; }

    .profile-container { position: relative; }
    .profile-circle { width: 36px; height: 36px; background: #000; color: white; border-radius: 50%; display: flex; justify-content: center; align-items: center; font-weight: 700; cursor: pointer; font-size: 0.8rem; }
    .dropdown { position: absolute; top: 45px; right: 0; background: white; border: 1px solid #eee; width: 200px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); z-index: 1000; padding: 0.5rem 0; border-radius: 12px; }
    .user-info { padding: 0.75rem 1rem; display: flex; flex-direction: column; }
    .name { font-weight: 700; font-size: 0.85rem; }
    .role { font-size: 0.7rem; color: #666; text-transform: uppercase; margin-top: 0.2rem; }
    hr { border: 0; border-top: 1px solid #eee; margin: 0.5rem 0; }
    .dropdown button { width: 100%; padding: 0.75rem 1rem; text-align: left; background: none; border: none; cursor: pointer; font-weight: 700; font-size: 0.8rem; text-transform: uppercase; border-radius: 0 0 12px 12px; }
    .dropdown button:hover { background: #f9f9f9; }

    .btn-re-request {
      background: #ef4444;
      color: white;
      padding: 0.4rem 0.8rem;
      border-radius: 6px;
      font-weight: 700;
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      border: none;
      cursor: pointer;
      transition: background 0.2s;
    }
    .btn-re-request:hover {
      background: #dc2626;
    }
    .btn-pending {
      background: #94a3b8 !important;
      cursor: not-allowed !important;
    }
  `]
})
export class HeaderComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private router = inject(Router);
  private operatorService = inject(OperatorService);
  private toastService = inject(ToastService);
  public notificationService = inject(NotificationService);

  get user(): AuthResponse | null {
    return this.authService.currentUser();
  }

  showDropdown = signal(false);
  showNotifications = signal(false);

  private notifInterval: any;

  ngOnInit() {
    if (this.user) {
      this.notificationService.loadNotifications(this.user.id);
      this.notifInterval = setInterval(() => {
        if (this.user) this.notificationService.loadNotifications(this.user.id);
      }, 10000);
    }
  }

  ngOnDestroy() {
    if (this.notifInterval) clearInterval(this.notifInterval);
  }

  toggleDropdown(event: MouseEvent) {
    event.stopPropagation();
    this.showNotifications.set(false);
    this.showDropdown.update(v => !v);
  }

  toggleNotifications(event: MouseEvent) {
    event.stopPropagation();
    this.showDropdown.set(false);
    this.showNotifications.update(v => !v);
  }

  @HostListener('document:click')
  closeDropdown() {
    this.showDropdown.set(false);
    this.showNotifications.set(false);
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  clearAllNotifications(event: MouseEvent) {
    event.stopPropagation();
    if (this.user) {
      this.notificationService.clearAll(this.user.id);
    }
  }

  isCustomer(): boolean {
    const role: any = this.user?.role;
    return role === 0 || role === 'Customer';
  }

  isOperator(): boolean {
    const role: any = this.user?.role;
    return role === 1 || role === 'Operator';
  }

  isAdmin(): boolean {
    const role: any = this.user?.role;
    return role === 2 || role === 'Admin';
  }

  getInitials(): string {
    if (!this.user?.fullName) return 'U';
    return this.user.fullName.split(' ').map((n: string) => n[0]).join('').toUpperCase();
  }

  getRoleName(): string {
    if (this.isAdmin()) return 'Administrator';
    if (this.isOperator()) return 'Operator';
    return 'Customer';
  }

  requestReReview() {
    if (!this.user) return;
    
    this.operatorService.requestAccountReview(this.user.id).subscribe({
      next: () => {
        this.toastService.show('Re-review request submitted successfully.');
        
        // Update local user status to 'Re-Approval Pending'
        const updatedUser = { ...this.user!, status: 'Re-Approval Pending' };
        // We use any because we are updating the signal which might be readonly or have specific types
        this.authService.currentUser.set(updatedUser);
        
        if (typeof window !== 'undefined') {
          localStorage.setItem('currentUser', JSON.stringify(updatedUser));
        }
      },
      error: () => {
        this.toastService.show('Failed to submit re-review request.', 'error');
      }
    });
  }
}
