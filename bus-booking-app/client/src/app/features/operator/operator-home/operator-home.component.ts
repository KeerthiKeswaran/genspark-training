import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OperatorService, OperatorStats } from '../../../core/services/operator.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-operator-home',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="operator-dashboard" *ngIf="stats() as data">
      <!-- Account Status Banner -->
      <div class="status-overlay" *ngIf="!authService.currentUser()?.isApproved">
        <div class="status-card" [class]="authService.currentUser()?.status?.toLowerCase()">
           <div class="status-icon">
             {{ authService.currentUser()?.status === 'Rejected' ? '❌' : '⏳' }}
           </div>
           <h2>Account {{ authService.currentUser()?.status || 'Pending' }}</h2>
           <p *ngIf="authService.currentUser()?.status === 'Rejected'">
             Reason: {{ authService.currentUser()?.rejectionReason || 'Please contact support.' }}
           </p>
           <p *ngIf="authService.currentUser()?.status !== 'Rejected'">
             Your account is currently under review by our administration team. You will be notified once you are approved.
           </p>
        </div>
      </div>

      <header class="header">
        <h1>Welcome, {{ authService.currentUser()?.fullName }}</h1>
        <p>Manage your fleet, schedules, and monitor performance.</p>
      </header>

      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-info">
            <h3>Total Buses</h3>
            <p class="value">{{ data.totalBuses }}</p>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-info">
            <h3>Active Trips</h3>
            <p class="value">{{ data.activeSchedules }}</p>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-info">
            <h3>Total Bookings</h3>
            <p class="value">{{ data.totalBookings }}</p>
          </div>
        </div>
        <div class="stat-card revenue">
          <div class="stat-info">
            <h3>Total Revenue</h3>
            <p class="value">₹{{ data.totalRevenue | number:'1.2-2' }}</p>
          </div>
        </div>
      </div>

      <section class="recent-activity">
        <h2>Recent Trip Activity</h2>
        <div class="table-container">
          <table class="activity-table">
            <thead>
              <tr>
                <th>Route</th>
                <th>Bus #</th>
                <th>Departure</th>
                <th>Occupancy</th>
                <th>Revenue</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let trip of data.recentTrips">
                <td>{{ trip.route }}</td>
                <td>{{ trip.busNumber }}</td>
                <td>{{ trip.departure | date:'short' }}</td>
                <td>
                  <div class="occupancy-bar">
                    <div class="fill" [style.width.%]="(trip.bookedSeats / trip.maxSeats) * 100"></div>
                  </div>
                  <span>{{ trip.bookedSeats }}/{{ trip.maxSeats }}</span>
                </td>
                <td>₹{{ trip.revenue | number:'1.2-2' }}</td>
                <td><span class="status-badge" [attr.data-status]="trip.status">{{ trip.status }}</span></td>
              </tr>
              <tr *ngIf="data.recentTrips.length === 0">
                <td colspan="6" class="empty">No recent activity found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .operator-dashboard { padding: 2rem; max-width: 1200px; margin: 0 auto; position: relative; }
    
    .status-overlay { position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: rgba(255,255,255,0.8); backdrop-filter: blur(4px); z-index: 100; display: flex; align-items: center; justify-content: center; border-radius: 16px; }
    .status-card { background: white; padding: 3rem; border-radius: 24px; box-shadow: 0 20px 25px -5px rgba(0,0,0,0.1); text-align: center; max-width: 450px; border: 1px solid #e2e8f0; }
    .status-icon { font-size: 3rem; margin-bottom: 1.5rem; }
    .status-card h2 { font-size: 1.5rem; color: #0f172a; margin-bottom: 1rem; }
    .status-card p { color: #64748b; line-height: 1.6; margin-bottom: 2rem; }
    .status-card.rejected { border-top: 5px solid #ef4444; }
    .btn-resubmit { background: #3b82f6; color: white; border: none; padding: 0.75rem 1.5rem; border-radius: 10px; font-weight: 600; cursor: pointer; transition: 0.2s; }
    .btn-resubmit:hover { background: #2563eb; transform: translateY(-2px); }

    .header { margin-bottom: 2.5rem; }
    .header h1 { font-size: 2rem; color: #1e293b; margin-bottom: 0.5rem; }
    .header p { color: #64748b; font-size: 1.1rem; }

    .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 1.5rem; margin-bottom: 3rem; }
    .stat-card { background: white; padding: 1.5rem; border-radius: 16px; display: flex; align-items: center; gap: 1.25rem; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); transition: transform 0.2s; }
    .stat-card:hover { transform: translateY(-4px); }
    .icon { font-size: 2.5rem; background: #f1f5f9; width: 64px; height: 64px; display: flex; align-items: center; justify-content: center; border-radius: 12px; }
    .stat-info h3 { font-size: 0.9rem; color: #64748b; margin-bottom: 0.25rem; text-transform: uppercase; letter-spacing: 0.05em; }
    .stat-info .value { font-size: 1.75rem; font-weight: 700; color: #0f172a; }
    .revenue .icon { background: #dcfce7; }
    .revenue .value { color: #166534; }

    .recent-activity { background: white; padding: 2rem; border-radius: 16px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }
    .recent-activity h2 { font-size: 1.25rem; margin-bottom: 1.5rem; color: #0f172a; }
    
    .table-container { overflow-x: auto; }
    .activity-table { width: 100%; border-collapse: collapse; min-width: 800px; }
    .activity-table th { text-align: left; padding: 1rem; color: #64748b; font-weight: 600; border-bottom: 1px solid #f1f5f9; font-size: 0.875rem; }
    .activity-table td { padding: 1.25rem 1rem; border-bottom: 1px solid #f1f5f9; color: #334155; }

    .occupancy-bar { width: 100px; height: 8px; background: #f1f5f9; border-radius: 4px; overflow: hidden; margin-bottom: 0.5rem; }
    .fill { height: 100%; background: linear-gradient(90deg, #3b82f6, #60a5fa); border-radius: 4px; }
    
    .status-badge { padding: 0.35rem 0.75rem; border-radius: 20px; font-size: 0.75rem; font-weight: 600; }
    .status-badge[data-status="Scheduled"] { background: #dbeafe; color: #1e40af; }
    .status-badge[data-status="Completed"] { background: #dcfce7; color: #166534; }
    .status-badge[data-status="Cancelled"] { background: #fee2e2; color: #991b1b; }

    .empty { text-align: center; color: #94a3b8; padding: 3rem !important; font-style: italic; }
  `]
})
export default class OperatorHomeComponent implements OnInit {
  operatorService = inject(OperatorService);
  authService = inject(AuthService);
  stats = signal<OperatorStats | null>(null);

  ngOnInit() {
    const operatorId = this.authService.currentUser()?.id;
    if (operatorId) {
      this.operatorService.getStats(operatorId).subscribe(data => {
        this.stats.set(data);
      });
    }
  }

}
