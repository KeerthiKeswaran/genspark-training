import { Component, OnInit, inject, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, AdminStats, AdminTrip } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="loading-bar" *ngIf="isLoading"></div>
    <div class="header">
      <h1>Dashboard Overview</h1>
      <p>Real-time traffic status and platform metrics.</p>
    </div>

    <div class="stats-grid" *ngIf="stats" [class.loading]="isLoading">
      <div class="stat-card">
        <span class="label">Total Bookings</span>
        <span class="value">{{ stats.totalBookings }}</span>
        <span class="trend">Confirmed & Paid</span>
      </div>
      <div class="stat-card">
        <span class="label">Gross Booking Revenue</span>
        <span class="value">₹{{ stats.grossBookingRevenue | number:'1.2-2' }}</span>
        <span class="trend success">User Payments</span>
      </div>
      <div class="stat-card">
        <span class="label">Net Revenue</span>
        <span class="value">₹{{ stats.netRevenue | number:'1.2-2' }}</span>
        <span class="trend success">Platform Share</span>
      </div>
      <div class="stat-card">
        <span class="label">Operator Payout</span>
        <span class="value">₹{{ stats.operatorPayout | number:'1.2-2' }}</span>
        <span class="trend">Due to Partners</span>
      </div>
    </div>

    <div class="trips-section">
      <div class="controls-bar">
        <div class="filter-group">
          <label>Start Date</label>
          <input type="date" [(ngModel)]="startDate" (change)="onFilterChange()" class="select-input">
        </div>
        <div class="filter-group">
          <label>End Date</label>
          <input type="date" [(ngModel)]="endDate" (change)="onFilterChange()" class="select-input">
        </div>
        <div class="filter-group">
          <label>Operator</label>
          <select [(ngModel)]="operatorId" (change)="onFilterChange()" class="select-input">
            <option value="">All Operators</option>
            <option *ngFor="let op of operators" [value]="op.id">{{ op.companyName }}</option>
          </select>
        </div>
        <div class="filter-group">
          <label>Category</label>
          <select (change)="filterByCategory($event)" class="select-input">
            <option value="All">All Trips</option>
            <option value="Confirmed Seats">Confirmed Seats (>0)</option>
            <option value="Zero Bookings">Zero Bookings (0)</option>
            <option value="Upcoming">Upcoming</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>
        <div class="filter-group">
          <label>Sort By</label>
          <select (change)="sortBy($event)" class="select-input">
            <option value="early">Early Departure</option>
            <option value="late">Late Departure</option>
            <option value="price-asc">Price (Low to High)</option>
            <option value="price-desc">Price (High to Low)</option>
            <option value="seats">Most Booked</option>
            <option value="cancellations">Most Cancelled</option>
          </select>
        </div>
      </div>

      <div class="section-header">
        <h2>Platform Trip Activity</h2>
      </div>

      <div class="card">
        <div class="table-responsive">
          <table class="table">
            <thead>
              <tr>
                <th>Route</th>
                <th>Operator</th>
                <th>Price</th>
                <th>Departure</th>
                <th>Status</th>
                <th>Occupancy</th>
                <th>Cancelled</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let trip of pagedTrips">
                <td><strong>{{ trip.route }}</strong></td>
                <td>{{ trip.operator }}</td>
                <td class="price">₹{{ trip.price }}</td>
                <td>{{ trip.departure | date:'MMM d, h:mm a' }}</td>
                <td>
                  <span class="badge" 
                    [class.completed]="trip.status === 'Completed'" 
                    [class.upcoming]="trip.status === 'Upcoming' || trip.status === 'Scheduled'" 
                    [class.cancelled]="trip.status === 'Cancelled'">
                    {{ trip.status }}
                  </span>
                </td>
                <td>
                  <div class="seat-progress-container">
                    <div class="seat-labels">
                      <span class="booked">{{ trip.bookedSeats }} booked</span>
                      <span class="total">{{ trip.maxSeats }} max</span>
                    </div>
                    <div class="progress-bar">
                      <div class="fill" [style.width]="(trip.bookedSeats / trip.maxSeats * 100) + '%'"></div>
                    </div>
                  </div>
                </td>
                <td>
                   <span class="badge cancelled" *ngIf="trip.cancelledSeats > 0">{{ trip.cancelledSeats }} seats</span>
                   <span class="text-muted" *ngIf="trip.cancelledSeats === 0">0</span>
                </td>
              </tr>
              <tr *ngIf="filteredTrips.length === 0">
                <td colspan="7" class="empty">No matching trip activity found.</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="pagination" *ngIf="filteredTrips.length > itemsPerPage">
          <button (click)="prevPage()" [disabled]="currentPage === 1" class="btn-page">← Previous</button>
          <span class="page-info">Page {{ currentPage }} of {{ totalPages }}</span>
          <button (click)="nextPage()" [disabled]="currentPage === totalPages" class="btn-page">Next →</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .loading-bar { height: 4px; background: #2563eb; position: fixed; top: 0; left: 0; width: 100%; animation: pulse 2s infinite; z-index: 1000; }
    @keyframes pulse { 0% { opacity: 0.3; } 50% { opacity: 1; } 100% { opacity: 0.3; } }
    .header h1 { font-size: 1.8rem; margin: 0 0 0.5rem 0; color: #0f172a; }
    .header p { color: #64748b; margin-top: 0; margin-bottom: 2rem; }
    
    .stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1.5rem; margin-bottom: 2.5rem; transition: opacity 0.3s ease; }
    .stats-grid.loading { opacity: 0.6; pointer-events: none; }
    .stat-card { background: white; padding: 1.5rem; border-radius: 16px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); display: flex; flex-direction: column; }
    .stat-card .label { font-size: 0.85rem; color: #64748b; font-weight: 600; text-transform: uppercase; margin-bottom: 0.5rem; }
    .stat-card .value { font-size: 1.8rem; font-weight: 800; color: #0f172a; margin-bottom: 0.5rem; }
    .stat-card .trend { font-size: 0.75rem; color: #94a3b8; font-weight: 500; }
    .stat-card .trend.success { color: #10b981; }

    .section-header { margin-bottom: 1rem; }
    .section-header h2 { font-size: 1.4rem; color: #0f172a; margin: 0; font-weight: 700; }
    
    .controls-bar { display: flex; gap: 1rem; flex-wrap: wrap; align-items: flex-end; background: white; padding: 1.25rem; border-radius: 16px; margin-bottom: 1.5rem; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); border: 1px solid #f1f5f9; }
    .filter-group { display: flex; flex-direction: column; gap: 0.4rem; }
    .filter-group label { font-size: 0.7rem; font-weight: 800; color: #64748b; text-transform: uppercase; letter-spacing: 0.025em; }
    .select-input { padding: 0.5rem 0.75rem; border: 1px solid #e2e8f0; border-radius: 8px; background: #f8fafc; font-size: 0.85rem; color: #0f172a; outline: none; min-width: 160px; font-weight: 500; }
    .select-input:focus { border-color: #2563eb; background: white; box-shadow: 0 0 0 3px rgba(37,99,235,0.1); }

    .card { background: white; padding: 1.5rem; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }
    .table-responsive { overflow-x: auto; }
    .table { width: 100%; border-collapse: collapse; }
    .table th, .table td { padding: 1rem; text-align: left; border-bottom: 1px solid #f1f5f9; }
    .table th { font-weight: 600; color: #475569; background: #f8fafc; font-size: 0.85rem; }
    .table td { font-size: 0.9rem; }
    .price { font-weight: 700; color: #0f172a; }
    
    .badge { padding: 0.25rem 0.75rem; border-radius: 20px; font-size: 0.75rem; font-weight: 600; }
    .badge.completed { background: #f1f5f9; color: #475569; }
    .badge.upcoming { background: #dcfce7; color: #166534; }
    .badge.cancelled { background: #fee2e2; color: #991b1b; }

    .seat-progress-container { display: flex; flex-direction: column; gap: 0.5rem; min-width: 140px; padding: 0.5rem 0; }
    .seat-labels { display: flex; justify-content: space-between; font-size: 0.75rem; font-weight: 800; }
    .seat-labels .booked { color: #2563eb; }
    .seat-labels .total { color: #94a3b8; }
    .progress-bar { height: 10px; background: #f1f5f9; border-radius: 5px; overflow: hidden; width: 100%; border: 1px solid #e2e8f0; }
    .progress-bar .fill { height: 100%; background: linear-gradient(90deg, #2563eb, #3b82f6); border-radius: 5px; transition: width 0.3s ease; }

    .pagination { display: flex; justify-content: center; align-items: center; gap: 1.5rem; margin-top: 1.5rem; padding-top: 1.5rem; border-top: 1px solid #f1f5f9; }
    .btn-page { background: #f8fafc; border: 1px solid #e2e8f0; padding: 0.5rem 1rem; border-radius: 6px; cursor: pointer; font-size: 0.9rem; font-weight: 600; color: #475569; }
    .btn-page:hover:not(:disabled) { background: #f1f5f9; }
    .btn-page:disabled { opacity: 0.5; cursor: not-allowed; }
    .page-info { font-size: 0.9rem; font-weight: 600; color: #64748b; }

    .empty { text-align: center; color: #94a3b8; font-style: italic; padding: 3rem !important; }
  `]
})
export default class AdminHomeComponent implements OnInit, OnDestroy {
  adminService = inject(AdminService);
  cdr = inject(ChangeDetectorRef);
  
  stats: AdminStats | null = null;
  filteredTrips: AdminTrip[] = [];
  operators: any[] = [];
  refreshInterval: any;
  currentCategory = 'All';
  currentSort = 'early';

  // Filters
  startDate: string = '';
  endDate: string = '';
  operatorId: string = '';

  // Pagination
  currentPage = 1;
  itemsPerPage = 10;

  isLoading = false;
  ngOnInit() {
    this.setDefaultDates();
    this.loadOperators();
    this.loadStats();
    this.refreshInterval = setInterval(() => this.loadStats(true), 15000);
  }

  setDefaultDates() {
    const today = new Date();
    const tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1);

    this.startDate = today.toISOString().split('T')[0];
    this.endDate = tomorrow.toISOString().split('T')[0];
  }

  loadOperators() {
    this.adminService.getOperators().subscribe(ops => {
      this.operators = ops.filter(o => o.isApproved);
      this.cdr.detectChanges();
    });
  }

  onFilterChange() {
    this.currentPage = 1; // Reset to page 1 on filter change
    this.loadStats();
  }

  ngOnDestroy() {
    if (this.refreshInterval) clearInterval(this.refreshInterval);
  }

  loadStats(isBackground = false) {
    if (!isBackground) this.isLoading = true;
    this.adminService.getStats(this.startDate, this.endDate, this.operatorId).subscribe({
      next: (data) => {
        this.stats = data;
        this.applyFiltersAndSorting();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  filterByCategory(event: any) {
    this.currentCategory = event.target.value;
    this.currentPage = 1;
    this.applyFiltersAndSorting();
  }

  sortBy(event: any) {
    this.currentSort = event.target.value;
    this.applyFiltersAndSorting();
  }

  get pagedTrips() {
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredTrips.slice(start, start + this.itemsPerPage);
  }

  get totalPages() {
    return Math.ceil(this.filteredTrips.length / this.itemsPerPage);
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

  applyFiltersAndSorting() {
    if (!this.stats) return;

    let trips = [...this.stats.recentTrips];

    // Filter by Category
    switch (this.currentCategory) {
      case 'Confirmed Seats':
        trips = trips.filter(t => t.bookedSeats > 0);
        break;
      case 'Zero Bookings':
        trips = trips.filter(t => t.bookedSeats === 0);
        break;
      case 'Upcoming':
        trips = trips.filter(t => t.status === 'Upcoming');
        break;
      case 'Completed':
        trips = trips.filter(t => t.status === 'Completed');
        break;
      case 'Cancelled':
        trips = trips.filter(t => t.status === 'Cancelled');
        break;
    }

    // Sort
    switch (this.currentSort) {
      case 'early':
        trips.sort((a, b) => new Date(a.departure).getTime() - new Date(b.departure).getTime());
        break;
      case 'late':
        trips.sort((a, b) => new Date(b.departure).getTime() - new Date(a.departure).getTime());
        break;
      case 'price-asc':
        trips.sort((a, b) => a.price - b.price);
        break;
      case 'price-desc':
        trips.sort((a, b) => b.price - a.price);
        break;
      case 'seats':
        trips.sort((a, b) => b.bookedSeats - a.bookedSeats);
        break;
      case 'cancellations':
        trips.sort((a, b) => b.cancelledSeats - a.cancelledSeats);
        break;
    }

    this.filteredTrips = trips;
  }
}
