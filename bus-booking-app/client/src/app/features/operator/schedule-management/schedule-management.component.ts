import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OperatorService } from '../../../core/services/operator.service';
import { AuthService } from '../../../core/services/auth.service';
import { LocationService, Hub } from '../../../core/services/location.service';

@Component({
  selector: 'app-schedule-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="schedule-container">
      <header class="header">
        <h1>Schedules & Trips</h1>
        <button class="btn-primary" (click)="showAddForm.set(true)">+ Create New Trip</button>
      </header>

      <!-- Create Trip Form Modal -->
      <div class="modal" *ngIf="showAddForm()">
        <div class="modal-content">
          <h2>Schedule New Journey</h2>
          <form (submit)="createSchedule()">
            <div class="form-group">
              <label>Select Bus</label>
              <select [(ngModel)]="newSchedule.busId" name="busId" required>
                <option *ngFor="let bus of approvedBuses()" [value]="bus.id">{{ bus.busNumber }} ({{ bus.totalSeats || bus.TotalSeats || 0 }} seats)</option>
              </select>
            </div>
            <div class="form-group">
              <label>Select Route</label>
              <select [(ngModel)]="newSchedule.routeId" name="routeId" (change)="onRouteSelect()" required>
                <option *ngFor="let route of routes" [value]="route.id">{{ route.source }} → {{ route.destination }}</option>
              </select>
            </div>

            <!-- Hubs Preview (Read Only) -->
            <div class="hubs-preview" *ngIf="newSchedule.routeId && (availableBoardingHubs.length > 0 || availableDroppingHubs.length > 0)">
              <div class="hub-preview-col">
                <span class="label">Boarding Stops</span>
                <ul>
                  <li *ngFor="let h of availableBoardingHubs">{{ h.name }}</li>
                </ul>
              </div>
              <div class="hub-preview-col">
                <span class="label">Dropping Stops</span>
                <ul>
                  <li *ngFor="let h of availableDroppingHubs">{{ h.name }}</li>
                </ul>
              </div>
            </div>


            <div class="form-row">
              <div class="form-group">
                <label>Departure Time</label>
                <input type="datetime-local" [(ngModel)]="newSchedule.departureTime" name="departureTime" required>
              </div>
              <div class="form-group">
                <label>Arrival Time</label>
                <input type="datetime-local" [(ngModel)]="newSchedule.arrivalTime" name="arrivalTime" required>
              </div>
            </div>
            <div class="form-group">
              <label>Ticket Price (₹)</label>
              <input type="number" [(ngModel)]="newSchedule.price" name="price" placeholder="850.00" required>
            </div>
            <div class="form-actions">
              <button type="button" class="btn-ghost" (click)="closeModal()">Cancel</button>
              <button type="submit" class="btn-primary">Launch Trip</button>
            </div>
          </form>
        </div>
      </div>

      <div class="schedule-list">
        <div class="schedule-item" *ngFor="let s of schedules()">
          <div class="trip-main">
            <div class="route-info">
              <h3>{{ s.route?.source }} → {{ s.route?.destination }}</h3>
              <span class="bus-ref">Bus: {{ s.bus?.busNumber }}</span>
            </div>
            <div class="time-info">
              <div class="time">
                <span class="label">DEP</span>
                <strong>{{ s.departureTime | date:'short' }}</strong>
              </div>
              <div class="time">
                <span class="label">ARR</span>
                <strong>{{ s.arrivalTime | date:'short' }}</strong>
              </div>
            </div>
          </div>
          <div class="trip-meta">
            <div class="price-info">
              <span class="label">Price</span>
              <strong>₹{{ s.price }}</strong>
            </div>
            <div class="status-info">
              <span class="status-badge" [attr.data-status]="s.status">{{ s.status }}</span>
            </div>
            <div class="actions">
              <button *ngIf="s.status !== 'Cancelled'" (click)="cancelTrip(s.id)" class="btn-danger-text">Cancel Trip</button>
            </div>
          </div>
        </div>
        
        <div *ngIf="schedules().length === 0" class="empty-state">
          <p>No trips scheduled. Start by launching your first journey!</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .schedule-container { padding: 2rem; max-width: 1000px; margin: 0 auto; }
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem; }
    .header h1 { font-size: 1.75rem; color: #1e293b; }

    .schedule-list { display: flex; flex-direction: column; gap: 1rem; }
    .schedule-item { background: white; border-radius: 12px; padding: 1.5rem; display: flex; flex-direction: column; gap: 1.25rem; box-shadow: 0 1px 3px rgba(0,0,0,0.1); border: 1px solid #f1f5f9; }
    
    .trip-main { display: flex; justify-content: space-between; align-items: flex-start; }
    .route-info h3 { margin: 0 0 0.25rem 0; font-size: 1.1rem; color: #0f172a; }
    .bus-ref { font-size: 0.85rem; color: #64748b; font-weight: 500; }

    .time-info { display: flex; gap: 2rem; }
    .time { display: flex; flex-direction: column; gap: 0.25rem; }
    .time .label { font-size: 0.7rem; color: #94a3b8; font-weight: 700; }
    .time strong { font-size: 0.95rem; color: #334155; }

    .trip-meta { display: flex; justify-content: space-between; align-items: center; padding-top: 1rem; border-top: 1px solid #f8fafc; }
    .price-info { display: flex; flex-direction: column; }
    .price-info .label { font-size: 0.75rem; color: #94a3b8; }
    .price-info strong { font-size: 1.1rem; color: #166534; }

    .status-badge { padding: 0.25rem 0.75rem; border-radius: 20px; font-size: 0.75rem; font-weight: 600; }
    .status-badge[data-status="Scheduled"] { background: #dbeafe; color: #1e40af; }
    .status-badge[data-status="Cancelled"] { background: #fee2e2; color: #991b1b; }

    .btn-danger-text { background: transparent; border: none; color: #dc2626; font-size: 0.85rem; font-weight: 600; cursor: pointer; }
    .btn-danger-text:hover { text-decoration: underline; }

    /* Modal Styles */
    .modal { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; padding: 1rem; }
    .modal-content { background: white; padding: 2rem; border-radius: 20px; width: 100%; max-width: 550px; max-height: 90vh; overflow-y: auto; box-shadow: 0 25px 50px -12px rgba(0,0,0,0.25); }
    .modal-content h2 { margin-bottom: 1.5rem; color: #0f172a; font-size: 1.5rem; }
    
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; margin-bottom: 0.5rem; }
    .form-group { margin-bottom: 1.5rem; }
    .form-group label { display: block; margin-bottom: 0.6rem; font-size: 0.9rem; color: #475569; font-weight: 600; }
    .form-group select, .form-group input { width: 100%; padding: 0.8rem; border: 1px solid #e2e8f0; border-radius: 10px; font-size: 0.95rem; background: #f8fafc; transition: all 0.2s; }
    .form-group select:focus, .form-group input:focus { border-color: #3b82f6; background: white; outline: none; box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1); }
    .form-actions { display: flex; justify-content: flex-end; gap: 1rem; margin-top: 1rem; padding-top: 1.5rem; border-top: 1px solid #f1f5f9; }

    .btn-primary { background: #3b82f6; color: white; border: none; padding: 0.75rem 1.5rem; border-radius: 8px; font-weight: 600; cursor: pointer; }
    .btn-ghost { background: transparent; border: none; padding: 0.75rem 1rem; border-radius: 8px; cursor: pointer; color: #64748b; }

    .hubs-preview { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; background: #f8fafc; padding: 1rem; border-radius: 10px; margin-bottom: 1.5rem; border: 1px dashed #e2e8f0; }
    .hub-preview-col .label { font-size: 0.65rem; font-weight: 800; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.05em; display: block; margin-bottom: 0.5rem; }
    .hub-preview-col ul { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.25rem; }
    .hub-preview-col li { font-size: 0.8rem; color: #475569; font-weight: 600; }

  `]
})
export default class ScheduleManagementComponent implements OnInit {
  operatorService = inject(OperatorService);
  authService = inject(AuthService);
  locationService = inject(LocationService);

  schedules = signal<any[]>([]);
  buses = signal<any[]>([]);
  routes: any[] = [];
  showAddForm = signal(false);

  availableBoardingHubs: Hub[] = [];
  availableDroppingHubs: Hub[] = [];

  approvedBuses = () => this.buses().filter(b => b.isApproved || b.status === 'Approved' || b.status === 1);

  newSchedule: any = {
    busId: '',
    routeId: '',
    departureTime: '',
    arrivalTime: '',
    price: 0,
    boardingHubIds: [],
    droppingHubIds: []
  };

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    const opId = this.authService.currentUser()?.id;
    if (opId) {
      this.operatorService.getSchedules(opId).subscribe({
        next: (data) => this.schedules.set(data),
        error: (err) => console.error('Error loading schedules:', err)
      });
      this.operatorService.getBuses(opId).subscribe({
        next: (data) => this.buses.set(data),
        error: (err) => console.error('Error loading buses:', err)
      });
      this.operatorService.getRoutes().subscribe({
        next: (data) => this.routes = data,
        error: (err) => console.error('Error loading routes:', err)
      });
    }
  }

  onRouteSelect() {
    const route = this.routes.find(r => r.id === this.newSchedule.routeId);
    if (!route) {
      this.availableBoardingHubs = [];
      this.availableDroppingHubs = [];
      return;
    }

    this.locationService.getCities().subscribe(cities => {
      const sourceCity = cities.find(c => c.name.toLowerCase() === route.source.toLowerCase());
      const destCity = cities.find(c => c.name.toLowerCase() === route.destination.toLowerCase());

      if (sourceCity) {
        this.locationService.getHubs(sourceCity.id).subscribe(hubs => {
          this.availableBoardingHubs = hubs.filter(h => h.type === 'Boarding' || h.type === 'Both').slice(0, 2);
        });
      } else {
        this.availableBoardingHubs = [];
      }

      if (destCity) {
        this.locationService.getHubs(destCity.id).subscribe(hubs => {
          this.availableDroppingHubs = hubs.filter(h => h.type === 'Dropping' || h.type === 'Both').slice(0, 2);
        });
      } else {
        this.availableDroppingHubs = [];
      }
    });
  }


  closeModal() {
    this.showAddForm.set(false);
    this.resetForm();
  }

  resetForm() {
    this.newSchedule = {
      busId: '',
      routeId: '',
      departureTime: '',
      arrivalTime: '',
      price: 0,
      boardingHubIds: [],
      droppingHubIds: []
    };
    this.availableBoardingHubs = [];
    this.availableDroppingHubs = [];
  }

  createSchedule() {
    const opId = this.authService.currentUser()?.id;
    if (!opId) {
      alert('Session expired. Please login again.');
      return;
    }

    if (!this.newSchedule.busId || !this.newSchedule.routeId || !this.newSchedule.departureTime || !this.newSchedule.arrivalTime) {
      alert('Please fill in all required fields.');
      return;
    }

    if (new Date(this.newSchedule.departureTime) >= new Date(this.newSchedule.arrivalTime)) {
      alert('Arrival time must be after departure time.');
      return;
    }

    this.operatorService.createSchedule(opId, this.newSchedule).subscribe({
      next: () => {
        alert('Trip launched successfully!');
        this.showAddForm.set(false);
        this.loadData();
        this.resetForm();
      },
      error: (err) => {
        console.error('Error launching trip:', err);
        alert(err.error?.message || err.error || 'Failed to launch trip. Please try again.');
      }
    });
  }

  cancelTrip(id: string) {
    const opId = this.authService.currentUser()?.id;
    if (opId && confirm('Are you sure you want to cancel this trip? All passengers will be notified and refunded.')) {
      this.operatorService.cancelSchedule(opId, id).subscribe({
        next: (res) => {
          alert(res.message || 'Trip cancelled successfully.');
          this.loadData();
        },
        error: (err) => {
          console.error('Error cancelling trip:', err);
          alert('Failed to cancel trip.');
        }
      });
    }
  }
}
