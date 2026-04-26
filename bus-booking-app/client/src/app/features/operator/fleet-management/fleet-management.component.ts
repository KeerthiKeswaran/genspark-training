import { Component, OnInit, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OperatorService } from '../../../core/services/operator.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-fleet-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="fleet-container">
      <header class="header">
        <h1>Fleet Management</h1>
        <button class="btn-primary" (click)="showAddForm.set(true)">+ Add New Bus</button>
      </header>

      <!-- Add Bus Form Modal -->
      <div class="modal" *ngIf="showAddForm()">
        <div class="modal-content">
          <h2>Register New Bus</h2>
          <form (submit)="addBus()">
            <div class="form-group">
              <label>Bus / Registration Number</label>
              <input type="text" [(ngModel)]="newBus.busNumber" name="busNumber" placeholder="e.g. MH 12 AB 1234" required>
            </div>
            <div class="form-group">
              <label>Bus Type</label>
              <select [(ngModel)]="newBus.busType" name="busType">
                <option value="AC Sleeper">AC Sleeper</option>
                <option value="Non-AC Sleeper">Non-AC Sleeper</option>
                <option value="AC Seater">AC Seater</option>
                <option value="Luxury Multi-Axle">Luxury Multi-Axle</option>
              </select>
            </div>
            <div class="form-group">
              <label>Total Capacity (Seats)</label>
              <input type="number" [(ngModel)]="newBus.totalSeats" name="totalSeats" min="10" max="60">
            </div>
            <div class="form-actions">
              <button type="button" class="btn-ghost" (click)="showAddForm.set(false)">Cancel</button>
              <button type="submit" class="btn-primary">Add Vehicle</button>
            </div>
          </form>
        </div>
      </div>

      <div class="bus-grid">
        <div class="bus-card" *ngFor="let bus of buses()">
          <div class="bus-header">
            <div class="bus-title">
              <h3>{{ bus.busNumber }}</h3>
              <span class="type">{{ bus.busType }}</span>
            </div>
            <div class="status-badge" [class]="bus.status?.toLowerCase() || 'pending'">
               {{ bus.status || 'Pending Approval' }}
            </div>
          </div>
          <div class="bus-details">
            <div class="detail">
              <span>Capacity</span>
              <strong>{{ bus.totalSeats || bus.TotalSeats || 0 }} Seats</strong>
            </div>
            <div class="detail">
              <span>Approval Status</span>
              <strong [class.status-approved]="bus.isApproved || bus.status === 'Approved'" 
                      [class.status-pending]="!bus.isApproved && bus.status !== 'Approved'">
                {{ bus.status || (bus.isApproved ? 'Approved' : 'Pending') }}
              </strong>
            </div>
          </div>
          <div class="rejection-box" *ngIf="bus.status === 'Rejected'">
            <span class="label">Rejection Reason:</span>
            <p>{{ bus.rejectionReason }}</p>
            <span class="status-pill rejected-final">Decision: Final Denial</span>
          </div>
          <div class="bus-actions" *ngIf="bus.status !== 'Rejected'">
            <button class="btn-revoke" (click)="revokeBus(bus.id)">
              {{ (bus.isApproved || bus.status === 'Approved') ? 'Remove Fleet' : 'Revoke Request' }}
            </button>
          </div>
        </div>
        <div *ngIf="buses().length === 0" class="empty-state">
          <p>No buses registered yet. Start by adding your first vehicle!</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .fleet-container { padding: 2rem; max-width: 1200px; margin: 0 auto; }
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem; }
    .header h1 { font-size: 1.75rem; color: #1e293b; }

    .bus-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(350px, 1fr)); gap: 1.5rem; }
    .bus-card { background: white; border-radius: 12px; padding: 1rem; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); border: 1px solid #f1f5f9; display: flex; flex-direction: column; }
    
    .bus-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem; }
    .bus-title h3 { margin: 0; font-size: 1rem; color: #0f172a; }
    .bus-title .type { font-size: 0.8rem; color: #64748b; }

    .status-badge { padding: 0.2rem 0.6rem; border-radius: 20px; font-size: 0.65rem; font-weight: 700; text-transform: uppercase; }
    .status-badge.approved { background: #dcfce7; color: #166534; }
    .status-badge.pending { background: #fef9c3; color: #854d0e; }
    .status-badge.rejected { background: #fee2e2; color: #b91c1c; }

    .status-approved { color: #16a34a; }
    .status-pending { color: #f59e0b; }

    .bus-details { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; padding: 0.75rem 0; border-top: 1px solid #f1f5f9; border-bottom: 1px solid #f1f5f9; margin-bottom: 0.75rem; }
    .detail { display: flex; flex-direction: column; gap: 0.15rem; }
    .detail span { font-size: 0.7rem; color: #94a3b8; text-transform: uppercase; }
    .detail strong { font-size: 0.85rem; color: #334155; }

    .rejection-box { background: #fff1f2; border: 1px solid #fecaca; border-radius: 10px; padding: 0.75rem; margin-bottom: 0.75rem; }
    .rejection-box .label { font-size: 0.7rem; font-weight: 800; color: #be123c; text-transform: uppercase; }
    .rejection-box p { margin: 0.25rem 0 0.5rem 0; font-size: 0.8rem; color: #9f1239; }
    .status-pill.rejected-final { display: inline-block; background: #be123c; color: white; padding: 0.3rem 0.6rem; border-radius: 4px; font-size: 0.7rem; font-weight: 600; }

    .bus-actions { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; margin-top: auto; }
    
    /* Forms & Modals */
    .modal { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 100; }
    .modal-content { background: white; padding: 2rem; border-radius: 20px; width: 100%; max-width: 450px; box-shadow: 0 20px 25px -5px rgba(0,0,0,0.1); }
    .modal-content h2 { margin-bottom: 1.5rem; color: #0f172a; }

    .form-group { margin-bottom: 1.25rem; }
    .form-group label { display: block; margin-bottom: 0.5rem; font-size: 0.9rem; color: #475569; font-weight: 500; }
    .form-group input, .form-group select { width: 100%; padding: 0.75rem; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 1rem; transition: border-color 0.2s; }
    .form-group input:focus { border-color: #3b82f6; outline: none; }

    .form-actions { display: flex; justify-content: flex-end; gap: 1rem; margin-top: 2rem; }

    .btn-primary { background: #3b82f6; color: white; border: none; padding: 0.75rem 1.5rem; border-radius: 8px; font-weight: 600; cursor: pointer; transition: background 0.2s; }
    .btn-primary:hover { background: #2563eb; }
    .btn-revoke { width: 100%; background: #fef2f2; color: #dc2626; border: 1px solid #fecaca; padding: 0.75rem; border-radius: 8px; font-weight: 600; cursor: pointer; transition: all 0.2s; }
    .btn-revoke:hover { background: #fee2e2; border-color: #fca5a5; }
    .btn-ghost { background: transparent; border: none; padding: 0.75rem 1rem; border-radius: 8px; cursor: pointer; color: #64748b; }
  `]
})
export default class FleetManagementComponent implements OnInit, OnDestroy {
  operatorService = inject(OperatorService);
  authService = inject(AuthService);
  
  buses = signal<any[]>([]);
  showAddForm = signal(false);
  refreshInterval: any;
  
  newBus = {
    busNumber: '',
    busType: 'AC Sleeper',
    totalSeats: 40
  };

  ngOnInit() {
    this.loadBuses();
    this.refreshInterval = setInterval(() => this.loadBuses(true), 20000);
  }

  ngOnDestroy() {
    if (this.refreshInterval) clearInterval(this.refreshInterval);
  }

  loadBuses(isBackground = false) {
    const opId = this.authService.currentUser()?.id;
    if (opId) {
      this.operatorService.getBuses(opId).subscribe({
        next: (data) => this.buses.set(data),
        error: (err) => {
          if (!isBackground) console.error('Error loading buses:', err);
        }
      });
    }
  }

  addBus() {
    const opId = this.authService.currentUser()?.id;
    if (!opId) {
      alert('Session expired. Please login again.');
      return;
    }

    if (!this.newBus.busNumber) {
      alert('Please enter a bus number.');
      return;
    }

    this.operatorService.addBus(opId, this.newBus).subscribe({
      next: (res) => {
        alert('Bus added successfully!');
        this.showAddForm.set(false);
        this.loadBuses();
        this.newBus = { busNumber: '', busType: 'AC Sleeper', totalSeats: 40 };
      },
      error: (err) => {
        console.error('Error adding bus:', err);
        let detailedMsg = err.error?.details || err.error?.message || err.error || 'Failed to add bus.';
        if (err.error?.innerError) {
          detailedMsg += '\n\nInner Error: ' + err.error.innerError;
        }
        alert(detailedMsg);
      }
    });
  }

  revokeBus(busId: string) {
    const opId = this.authService.currentUser()?.id;
    if (opId && confirm('Are you sure you want to revoke this request? This will remove the bus from your fleet.')) {
      this.operatorService.deleteBus(opId, busId).subscribe({
        next: () => {
          this.loadBuses();
        },
        error: (err) => {
          console.error('Error revoking bus:', err);
          alert('Failed to revoke request.');
        }
      });
    }
  }
}
