import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, City, Hub } from '../../../core/services/admin.service';
import { LocationService } from '../../../core/services/location.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-location-master',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="header">
      <h1>Master Data: Routes</h1>
      <p>Define available travel routes across the platform.</p>
    </div>

    <div class="content-card">
      <div class="add-form">
        <div class="form-group">
          <label>Source City</label>
          <input [(ngModel)]="newRoute.source" placeholder="e.g. Mumbai" class="input" />
        </div>
        <div class="form-group">
          <label>Destination City</label>
          <input [(ngModel)]="newRoute.destination" placeholder="e.g. Pune" class="input" />
        </div>
        <div class="form-group">
          <label>Distance (km)</label>
          <input type="number" [(ngModel)]="newRoute.distanceKm" placeholder="150" class="input" />
        </div>
        <button (click)="addRoute()" class="btn-primary">Create Route</button>
      </div>

      <div class="table-container">
        <table class="route-table">
          <thead>
            <tr>
              <th>Source</th>
              <th>Destination</th>
              <th>Distance</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let route of pagedRoutes">
              <td><strong>{{ route.source }}</strong></td>
              <td><strong>{{ route.destination }}</strong></td>
              <td>{{ route.distanceKm }} km</td>
              <td><span class="badge active">Active</span></td>
            </tr>
            <tr *ngIf="routes.length === 0">
              <td colspan="4" class="empty">No routes defined yet.</td>
            </tr>
          </tbody>
        </table>

        <!-- Pagination Controls -->
        <div class="pagination" *ngIf="routes.length > itemsPerPage">
          <button (click)="prevPage()" [disabled]="currentPage === 1" class="btn-page">← Previous</button>
          <span class="page-info">Page {{ currentPage }} of {{ totalPages }}</span>
          <button (click)="nextPage()" [disabled]="currentPage === totalPages" class="btn-page">Next →</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .header h1 { font-size: 1.8rem; margin: 0 0 0.5rem 0; color: #0f172a; }
    .header p { color: #64748b; margin-top: 0; margin-bottom: 2rem; }
    .content-card { background: white; padding: 2rem; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }
    .add-form { display: flex; align-items: flex-end; gap: 1.5rem; margin-bottom: 2.5rem; padding-bottom: 2rem; border-bottom: 1px solid #f1f5f9; }
    .form-group { display: flex; flex-direction: column; gap: 0.5rem; flex: 1; }
    .form-group label { font-size: 0.85rem; font-weight: 600; color: #475569; }
    .input { padding: 0.75rem; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 0.95rem; }
    .btn-primary { background: #2563eb; color: white; border: none; padding: 0.75rem 1.5rem; border-radius: 8px; cursor: pointer; font-weight: 600; height: 44px; }
    .btn-primary:hover { background: #1d4ed8; }
    
    .route-table { width: 100%; border-collapse: collapse; }
    .route-table th { text-align: left; padding: 1rem; border-bottom: 2px solid #f1f5f9; color: #64748b; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.05em; }
    .route-table td { padding: 1rem; border-bottom: 1px solid #f1f5f9; color: #334155; }
    .route-table tr:hover { background: #f8fafc; }
    
    .badge { font-size: 0.75rem; padding: 0.25rem 0.75rem; border-radius: 20px; font-weight: 600; }
    .badge.active { background: #dcfce7; color: #166534; }
    .empty { text-align: center; color: #94a3b8; padding: 3rem !important; font-style: italic; }

    .pagination { display: flex; justify-content: center; align-items: center; gap: 1.5rem; margin-top: 2rem; padding-top: 1.5rem; border-top: 1px solid #f1f5f9; }
    .btn-page { background: #f8fafc; border: 1px solid #e2e8f0; padding: 0.5rem 1rem; border-radius: 6px; cursor: pointer; font-size: 0.9rem; font-weight: 600; color: #475569; transition: all 0.2s; }
    .btn-page:hover:not(:disabled) { background: #f1f5f9; border-color: #cbd5e1; color: #0f172a; }
    .btn-page:disabled { opacity: 0.5; cursor: not-allowed; }
    .page-info { font-size: 0.9rem; font-weight: 600; color: #64748b; }
  `]
})
export default class LocationMasterComponent implements OnInit {
  adminService = inject(AdminService);
  toastService = inject(ToastService);
  cdr = inject(ChangeDetectorRef);

  routes: any[] = [];
  newRoute: any = { source: '', destination: '', distanceKm: 0 };

  // Pagination
  currentPage = 1;
  itemsPerPage = 10;

  ngOnInit() {
    this.loadRoutes();
  }

  loadRoutes() {
    this.adminService.getRoutes().subscribe(data => {
      this.routes = data;
      this.cdr.detectChanges();
    });
  }

  get pagedRoutes() {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.routes.slice(start, start + this.itemsPerPage);
  }

  get totalPages() {
    return Math.ceil(this.routes.length / this.itemsPerPage);
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  addRoute() {
    if (!this.newRoute.source || !this.newRoute.destination) {
      this.toastService.show('Please fill in source and destination.');
      return;
    }
    this.adminService.addRoute(this.newRoute).subscribe(() => {
      this.toastService.show('Route created successfully.');
      this.loadRoutes();
      this.newRoute = { source: '', destination: '', distanceKm: 0 };
    });
  }
}
