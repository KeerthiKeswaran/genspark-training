import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { BookingHistory } from '../../../core/models/booking.models';
import { finalize } from 'rxjs';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-booking-history',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Toast Notification -->
    <div class="toast" [class.toast-visible]="showToast" [class.toast-hidden]="!showToast">
      <span class="toast-icon">✅</span>
      <div class="toast-content">
        <span class="toast-title">Booking Confirmed!</span>
        <span class="toast-sub">ID: #{{ shortId(successBookingId) }}</span>
      </div>
    </div>

    <div class="history-container">
      <div class="header">
        <h1>My Bookings</h1>
        <div class="tabs">
          <button [class.active]="activeTab === 'Upcoming'" (click)="activeTab = 'Upcoming'">Upcoming Trips</button>
          <button [class.active]="activeTab === 'Completed'" (click)="activeTab = 'Completed'">Completed</button>
          <button [class.active]="activeTab === 'Cancelled'" (click)="activeTab = 'Cancelled'">Cancelled</button>
        </div>
      </div>

      <div class="booking-list" *ngIf="!loading">
        <div class="empty-state" *ngIf="filteredBookings().length === 0">
          <div class="empty-icon">🎫</div>
          <p *ngIf="activeTab === 'Upcoming'">You have no upcoming trips scheduled.</p>
          <p *ngIf="activeTab === 'Completed'">You have no completed trips.</p>
          <p *ngIf="activeTab === 'Cancelled'">You have no cancelled bookings.</p>
        </div>

        <div class="booking-card" 
             *ngFor="let b of filteredBookings()" 
             [class.cancelled]="b.status === 'Cancelled'"
             [class.is-recent]="b.bookingId === successBookingId">
          
          <div class="card-top-bar">
            <span class="booking-id"># {{ shortId(b.bookingId) }}</span>
            <span class="status-badge" [class.status-confirmed]="b.status === 'Confirmed'" [class.status-cancelled]="b.status === 'Cancelled'">{{ b.status }}</span>
          </div>

          <div class="card-body">
            <div class="card-left">
              <div class="route">{{ b.source }} → {{ b.destination }}</div>
              <div class="date">
                <span class="time-block">🛫 {{ b.departureTime | date:'EEE, MMM dd' }} · {{ b.departureTime | date:'shortTime' }}</span>
                <span class="time-separator">→</span>
                <span class="time-block">🛬 {{ b.arrivalTime | date:'EEE, MMM dd' }} · {{ b.arrivalTime | date:'shortTime' }}</span>
              </div>
              <div class="bus-details">
                <span class="operator">{{ b.busName }}</span>
                <span class="bus-no">{{ b.busNumber }}</span>
              </div>
              <div class="points" *ngIf="b.boardingPoint && b.boardingPoint !== 'N/A'">
                <span class="hub">🚏 Boarding: {{ b.boardingPoint }}</span>
                <span class="hub">📍 Dropping: {{ b.droppingPoint }}</span>
              </div>
              <div class="passengers-row">
                <span class="seats">{{ b.seatNumbers.join(', ') }}</span>
                <span class="divider">·</span>
                <span class="passenger-names">{{ b.passengerNames.join(', ') }}</span>
              </div>
            </div>
            
            <div class="card-right">
              <div class="amount">₹{{ b.totalAmount }}</div>
              <button 
                *ngIf="canCancel(b)" 
                class="btn-cancel" 
                (click)="cancelBooking(b)"
                [disabled]="cancellingId === b.bookingId"
              >
                {{ cancellingId === b.bookingId ? 'Cancelling...' : 'Cancel' }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <div *ngIf="loading" class="loading-state">
        <div class="spinner"></div>
        <p>Loading your bookings...</p>
      </div>
    </div>
  `,
  styles: [`
    /* Toast */
    .toast {
      position: fixed;
      top: 2rem;
      right: 2rem;
      z-index: 9999;
      background: #fff;
      border: 1px solid #c8e6c9;
      border-radius: 16px;
      padding: 1rem 1.5rem;
      display: flex;
      align-items: center;
      gap: 1rem;
      box-shadow: 0 20px 50px rgba(76, 175, 80, 0.2);
      min-width: 280px;
      transition: all 0.5s ease;
      opacity: 0;
      transform: translateY(-20px);
      pointer-events: none;
    }
    .toast-visible {
      opacity: 1;
      transform: translateY(0);
    }
    .toast-hidden {
      opacity: 0;
      transform: translateY(-20px);
    }
    .toast-icon { font-size: 1.5rem; }
    .toast-content { display: flex; flex-direction: column; gap: 0.1rem; }
    .toast-title { font-weight: 800; font-size: 0.95rem; color: #111; }
    .toast-sub { font-size: 0.8rem; color: #4caf50; font-weight: 700; letter-spacing: 0.05em; }

    /* Layout */
    .history-container { max-width: 900px; margin: 4rem auto; padding: 2rem; font-family: 'Inter', sans-serif; }
    .header { border-bottom: 1px solid #eee; margin-bottom: 3rem; padding-bottom: 1rem; display: flex; justify-content: space-between; align-items: flex-end; }
    .header h1 { font-weight: 900; text-transform: uppercase; margin: 0; font-size: 1.75rem; }
    
    .tabs { display: flex; gap: 0.5rem; }
    .tabs button { background: none; border: none; font-weight: 800; text-transform: uppercase; padding: 0.5rem 1rem; cursor: pointer; color: #888; transition: all 0.2s; border-radius: 8px; font-size: 0.8rem; letter-spacing: 0.05em; }
    .tabs button.active { color: #000; background: #f0f0f0; }
    .tabs button:hover:not(.active) { color: #333; background: #f9f9f9; }

    /* Booking list */
    .booking-list { display: flex; flex-direction: column; gap: 1.5rem; }
    
    .booking-card { 
      background: #fff; 
      border: 1px solid #eee; 
      transition: all 0.3s ease;
      border-radius: 20px;
      box-shadow: 0 4px 20px rgba(0,0,0,0.03);
      overflow: hidden;
    }
    .booking-card:hover { transform: translateY(-4px); box-shadow: 0 15px 40px rgba(0,0,0,0.08); }
    .booking-card.cancelled { border-color: #f5f5f5; background: #fafafa; opacity: 0.6; }
    .booking-card.cancelled:hover { transform: none; box-shadow: 0 4px 20px rgba(0,0,0,0.03); }
    .booking-card.is-recent {
      border: 2px solid #4caf50;
      animation: glowPulse 2s ease-in-out 3;
    }
    @keyframes glowPulse {
      0%, 100% { box-shadow: 0 4px 20px rgba(76, 175, 80, 0.15); }
      50% { box-shadow: 0 8px 30px rgba(76, 175, 80, 0.35); }
    }

    .card-top-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem 1.75rem;
      background: #fafafa;
      border-bottom: 1px solid #eee;
    }
    .booking-id { font-size: 0.75rem; font-weight: 800; color: #999; letter-spacing: 0.1em; text-transform: uppercase; }
    .status-badge { font-size: 0.7rem; font-weight: 800; text-transform: uppercase; padding: 0.25rem 0.75rem; border-radius: 30px; letter-spacing: 0.08em; }
    .status-confirmed { background: #e8f5e9; color: #2e7d32; }
    .status-cancelled { background: #fff3f3; color: #c62828; }

    .card-body { display: grid; grid-template-columns: 1fr auto; gap: 2rem; padding: 1.75rem; align-items: center; }

    .card-left { display: flex; flex-direction: column; gap: 0.6rem; }
    .route { font-weight: 900; font-size: 1.35rem; letter-spacing: -0.02em; }
    .date { font-weight: 600; color: #666; font-size: 0.85rem; display: flex; align-items: center; gap: 0.75rem; }
    .time-block { display: flex; align-items: center; gap: 0.4rem; }
    .time-separator { color: #ccc; font-weight: 400; }
    .bus-details { display: flex; align-items: center; gap: 0.5rem; }
    .operator { font-weight: 800; background: #f0f0f0; padding: 0.2rem 0.65rem; border-radius: 6px; font-size: 0.8rem; color: #333; }
    .bus-no { font-weight: 600; color: #888; font-size: 0.8rem; }
    .points { display: flex; flex-direction: column; gap: 0.2rem; }
    .hub { font-size: 0.78rem; font-weight: 600; color: #888; }

    .passengers-row { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
    .seats { font-size: 0.85rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.06em; color: #111; }
    .divider { color: #ccc; }
    .passenger-names { font-size: 0.85rem; color: #555; font-weight: 600; }

    .card-right { text-align: right; display: flex; flex-direction: column; align-items: flex-end; gap: 1rem; }
    .amount { font-weight: 900; font-size: 1.75rem; letter-spacing: -0.03em; }

    .btn-cancel { 
      background: #fff5f5; 
      border: 1px solid #ffcfcf; 
      color: #ff5252; 
      padding: 0.6rem 1.25rem; 
      font-weight: 800; 
      text-transform: uppercase; 
      font-size: 0.72rem;
      letter-spacing: 0.05em;
      cursor: pointer; 
      transition: all 0.2s;
      border-radius: 30px;
    }
    .btn-cancel:hover:not(:disabled) { background: #ff5252; color: #fff; border-color: #ff5252; }
    .btn-cancel:disabled { opacity: 0.5; cursor: not-allowed; }

    .empty-state { text-align: center; padding: 5rem 2rem; border: 1px dashed #ddd; background: #fafafa; border-radius: 24px; color: #aaa; }
    .empty-icon { font-size: 3rem; margin-bottom: 1rem; }
    .empty-state p { font-size: 1rem; font-weight: 600; margin: 0; }

    .loading-state { text-align: center; padding: 5rem 2rem; display: flex; flex-direction: column; align-items: center; gap: 1.5rem; color: #888; font-weight: 600; }
    .spinner { width: 40px; height: 40px; border: 3px solid #f3f3f3; border-top: 3px solid #000; border-radius: 50%; animation: spin 1s linear infinite; }
    @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
  `]
})
export class HistoryComponent implements OnInit, OnDestroy {
  private bookingService = inject(BookingService);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  bookings: BookingHistory[] = [];
  loading = true;
  cancellingId: string | null = null;
  
  showToast = false;
  successBookingId = '';
  activeTab: 'Upcoming' | 'Completed' | 'Cancelled' = 'Upcoming';

  private toastTimer: any;

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['success'] && params['bookingId']) {
        this.successBookingId = params['bookingId'];
        this.triggerToast();
      }
    });
    this.loadHistory();
  }

  ngOnDestroy() {
    if (this.toastTimer) clearTimeout(this.toastTimer);
  }

  triggerToast() {
    this.showToast = true;
    this.cdr.detectChanges();
    this.toastTimer = setTimeout(() => {
      this.showToast = false;
      this.cdr.detectChanges();
    }, 5000);
  }

  shortId(id: string): string {
    return id ? id.replace(/-/g, '').substring(0, 8).toUpperCase() : '';
  }

  loadHistory() {
    this.loading = true;
    this.bookingService.getHistory()
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (data) => {
          this.bookings = data || [];
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Failed to load history:', err);
          this.bookings = [];
          this.cdr.detectChanges();
        }
      });
  }

  filteredBookings() {
    return this.bookings.filter(b => b.category === this.activeTab);
  }

  canCancel(booking: BookingHistory): boolean {
    if (booking.status === 'Cancelled') return false;
    const departure = new Date(booking.departureTime).getTime();
    const now = new Date().getTime();
    const diffHours = (departure - now) / (1000 * 60 * 60);
    return diffHours >= 24;
  }

  cancelBooking(booking: BookingHistory) {
    if (!confirm('Are you sure you want to cancel this booking? A refund will be initiated.')) return;
    this.cancellingId = booking.bookingId;
    this.bookingService.cancelBooking(booking.bookingId)
      .pipe(finalize(() => this.cancellingId = null))
      .subscribe({
        next: (res) => {
          alert(res.message || 'Booking cancelled successfully.');
          this.loadHistory();
        },
        error: (err) => {
          alert(err.error?.message || 'Failed to cancel booking.');
        }
      });
  }
}
