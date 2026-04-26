import { Component, OnInit, OnDestroy, inject, NgZone, ChangeDetectorRef } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { BookingService } from '../../../core/services/booking.service';
import { Subscription, interval, finalize } from 'rxjs';

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="payment-wrapper">
      <div class="payment-container" [class.processing-blur]="paymentStatus !== 'idle'">
        <div class="header">
        <h1>Checkout &amp; Payment</h1>
        <div class="timer" [class.urgent]="isExpiringSoon">
          <span class="timer-icon">⏱</span>
          <span class="timer-text">{{ remainingTime || 'Loading...' }}</span>
        </div>
      </div>

      <div class="summary-section">
        <h2>Order Summary</h2>

        <!-- Journey Details loaded -->
        <ng-container *ngIf="journeyDetails">
          <div class="route-banner">
            <div class="city">
              <span class="label">From</span>
              <span class="name">{{ journeyDetails.source || '—' }}</span>
            </div>
            <div class="arrow-wrap">
              <div class="arrow-line"></div>
              <span class="arrow-icon"><svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="transform: scaleX(1);"><path d="M8 6v6"/><path d="M15 6v6"/><path d="M2 12h19.6"/><path d="M18 18h3s.5-1.7.8-2.8c.1-.4.2-.8.2-1.2 0-.4-.1-.8-.2-1.2l-1.4-5C20.1 6.8 19.1 6 18 6H4a2 2 0 0 0-2 2v10h3"/><circle cx="7" cy="18" r="2"/><path d="M9 18h5"/><circle cx="16" cy="18" r="2"/></svg></span>
              <div class="arrow-line"></div>
            </div>
            <div class="city city-right">
              <span class="label">To</span>
              <span class="name">{{ journeyDetails.destination || '—' }}</span>
            </div>
          </div>

          <div class="meta-grid">
            <div class="meta-item">
              <span class="label">Date</span>
              <span class="val">{{ journeyDetails.departureTime | date:'MMM dd, yyyy' }}</span>
            </div>
            <div class="meta-item">
              <span class="label">Time</span>
              <span class="val">{{ journeyDetails.departureTime | date:'shortTime' }}</span>
            </div>
            <div class="meta-item">
              <span class="label">Operator</span>
              <span class="val">{{ journeyDetails.busName }}</span>
            </div>
            <div class="meta-item">
              <span class="label">Bus No.</span>
              <span class="val">{{ journeyDetails.busNumber }}</span>
            </div>
            <div class="meta-item">
              <span class="label">Seats</span>
              <span class="val">{{ selectedSeats.join(', ') }}</span>
            </div>
            <div class="meta-item" style="grid-column: span 2;">
              <span class="label">Boarding Point</span>
              <span class="val">{{ boardingPointName || 'N/A' }}</span>
            </div>
            <div class="meta-item" style="grid-column: span 2;">
              <span class="label">Dropping Point</span>
              <span class="val">{{ droppingPointName || 'N/A' }}</span>
            </div>
          </div>

          <!-- Passengers -->
          <div class="passengers-section" *ngIf="passengers.length">
            <h3 class="section-label">Passengers</h3>
            <div class="passenger-row" *ngFor="let p of passengers; let i = index">
              <div class="p-seat">{{ p.seatNumber }}</div>
              <div class="p-info">
                <span class="p-name">{{ p.name }}</span>
                <span class="p-meta">{{ p.age }} yrs · {{ p.gender }}</span>
              </div>
            </div>
          </div>

          <div class="price-breakdown">
            <div class="price-row">
              <span>Base Price ({{ selectedSeats.length }} seat{{ selectedSeats.length > 1 ? 's' : '' }})</span>
              <span>₹{{ journeyDetails.basePrice }}</span>
            </div>
            <div class="price-row">
              <span>Convenience Fee</span>
              <span>₹{{ journeyDetails.convenienceFee }}</span>
            </div>
            <div class="price-row total">
              <span>Total Amount</span>
              <span>₹{{ journeyDetails.totalPrice }}</span>
            </div>
          </div>
        </ng-container>

        <!-- Loading state -->
        <div class="loading-details" *ngIf="!journeyDetails && !fetchError">
          <div class="spinner-small"></div>
          <span>Loading journey details...</span>
        </div>

        <!-- Error state -->
        <div class="error-state" *ngIf="fetchError">
          <span>⚠ Could not load journey details.</span>
          <button (click)="fetchJourneyDetails()">Retry</button>
        </div>
      </div>

      <div class="payment-actions">
        <button class="btn-pay" (click)="processPayment()" [disabled]="loading || !passengers.length || !journeyDetails || fetchError">
          Pay Now →
        </button>
      </div>
    </div>

    <!-- Centered Payment Overlay -->
    <div id="payment-overlay" class="payment-overlay" *ngIf="paymentStatus !== 'idle'">
      <div class="overlay-content">
        <div *ngIf="paymentStatus === 'processing'" class="payment-processing">
          <div class="spinner"></div>
          <p>Processing Payment...</p>
        </div>

        <div *ngIf="paymentStatus === 'success'" class="payment-success">
          <svg class="checkmark" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 52 52">
            <circle class="checkmark__circle" cx="26" cy="26" r="25" fill="none"/>
            <path class="checkmark__check" fill="none" d="M14.1 27.2l7.1 7.2 16.7-16.8"/>
          </svg>
          <p>Payment Successful!</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .payment-container { max-width: 760px; margin: 3rem auto; padding: 2rem; font-family: 'Inter', sans-serif; }
    
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2.5rem; border-bottom: 1px solid #eee; padding-bottom: 1.25rem; }
    .header h1 { font-weight: 900; text-transform: uppercase; font-size: 1.5rem; letter-spacing: -0.02em; margin: 0; }
    
    .timer {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      background: #f0f0f0;
      color: #333;
      padding: 0.6rem 1.25rem;
      font-weight: 800;
      font-size: 1rem;
      border-radius: 30px;
      transition: all 0.3s;
    }
    .timer.urgent { background: #ff5252; color: #fff; animation: pulse 1s ease-in-out infinite; }
    .timer-icon { font-size: 1rem; }
    .timer-text { font-variant-numeric: tabular-nums; letter-spacing: 0.05em; min-width: 50px; }
    @keyframes pulse { 0%, 100% { transform: scale(1); } 50% { transform: scale(1.03); } }

    .summary-section {
      background: #fff;
      border: 1px solid #eee;
      padding: 2.5rem;
      margin-bottom: 2rem;
      box-shadow: 0 10px 35px rgba(0,0,0,0.05);
      border-radius: 20px;
    }
    .summary-section h2 {
      font-weight: 800;
      margin: 0 0 2rem 0;
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.15em;
      color: #aaa;
    }

    .route-banner {
      display: flex;
      align-items: center;
      justify-content: space-between;
      background: linear-gradient(135deg, #0f0f0f 0%, #2d2d2d 100%);
      color: #fff;
      padding: 1.75rem 2rem;
      border-radius: 16px;
      margin-bottom: 2rem;
      gap: 1rem;
    }
    .city { display: flex; flex-direction: column; flex: 1; }
    .city-right { align-items: flex-end; }
    .city .label { font-size: 0.6rem; text-transform: uppercase; opacity: 0.5; font-weight: 800; margin-bottom: 0.4rem; letter-spacing: 0.15em; }
    .city .name { font-size: 1.3rem; font-weight: 900; text-transform: uppercase; letter-spacing: -0.02em; }
    
    .arrow-wrap { display: flex; align-items: center; gap: 0.5rem; flex: 0.8; }
    .arrow-line { flex: 1; height: 1px; background: rgba(255,255,255,0.2); }
    .arrow-icon { font-size: 1.1rem; opacity: 0.6; }

    .meta-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1.5rem;
      margin-bottom: 2rem;
      padding: 1.5rem;
      background: #f9f9f9;
      border-radius: 12px;
      border: 1px solid #f0f0f0;
    }
    .meta-item { display: flex; flex-direction: column; gap: 0.3rem; }
    .meta-item .label { font-size: 0.6rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.1em; color: #bbb; }
    .meta-item .val { font-weight: 700; font-size: 0.9rem; color: #222; }

    .passengers-section { margin-bottom: 2rem; }
    .section-label { font-size: 0.7rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.12em; color: #bbb; margin: 0 0 1rem 0; }
    .passenger-row { display: flex; align-items: center; gap: 1rem; padding: 0.75rem 1rem; background: #f9f9f9; border: 1px solid #f0f0f0; border-radius: 10px; margin-bottom: 0.5rem; }
    .p-seat { background: #000; color: #fff; font-weight: 900; font-size: 0.75rem; padding: 0.35rem 0.65rem; border-radius: 6px; min-width: 36px; text-align: center; }
    .p-info { display: flex; flex-direction: column; gap: 0.15rem; }
    .p-name { font-weight: 700; font-size: 0.95rem; color: #222; }
    .p-meta { font-size: 0.75rem; font-weight: 600; color: #999; }

    .price-breakdown { border-top: 1px solid #eee; padding-top: 1.5rem; }
    .price-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.6rem 0;
      font-weight: 600;
      color: #666;
      font-size: 0.95rem;
      border-bottom: 1px dashed #f5f5f5;
    }
    .price-row:last-child { border-bottom: none; }
    .price-row.total {
      border-top: 2px solid #000;
      border-bottom: none;
      padding-top: 1.25rem;
      margin-top: 0.5rem;
      color: #000;
      font-size: 1.5rem;
      font-weight: 900;
    }

    .loading-details { display: flex; align-items: center; gap: 1rem; padding: 3rem; color: #aaa; font-weight: 600; justify-content: center; }
    .spinner-small { width: 22px; height: 22px; border: 2px solid #eee; border-top: 2px solid #000; border-radius: 50%; animation: spin 0.8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }

    .error-state { display: flex; align-items: center; justify-content: space-between; padding: 1.5rem; background: #fff8f8; border: 1px solid #ffdddd; border-radius: 12px; color: #c00; font-weight: 700; }
    .error-state button { background: #000; color: #fff; border: none; padding: 0.5rem 1.25rem; border-radius: 30px; font-weight: 700; cursor: pointer; }

    .payment-actions { text-align: right; margin-top: 2rem; }
    .btn-pay {
      background: #000;
      color: #fff;
      border: none;
      padding: 1.1rem 4rem;
      font-weight: 900;
      text-transform: uppercase;
      letter-spacing: 0.12em;
      cursor: pointer;
      font-size: 1rem;
      border-radius: 40px;
      transition: all 0.25s;
    }
    .btn-pay:hover:not(:disabled) { background: #222; transform: translateY(-2px); box-shadow: 0 8px 20px rgba(0,0,0,0.15); }
    .btn-pay:disabled { background: #eee; color: #aaa; cursor: not-allowed; transform: none; box-shadow: none; }
    
    .payment-wrapper { position: relative; }
    .processing-blur { opacity: 0.2; pointer-events: none; filter: blur(2px); transition: all 0.3s ease; }
    
    .payment-overlay {
      position: absolute;
      top: 0; left: 0; right: 0; bottom: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 100;
    }
    .overlay-content {
      background: white;
      padding: 3rem;
      border-radius: 24px;
      box-shadow: 0 20px 40px rgba(0,0,0,0.1);
      text-align: center;
      border: 1px solid #eee;
    }

    .payment-processing, .payment-success {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1.5rem;
    }
    .payment-processing p, .payment-success p { font-weight: 800; font-size: 1.3rem; color: #111; margin: 0; letter-spacing: -0.02em; }
    .spinner {
      width: 50px; height: 50px; border: 4px solid #f3f3f3; border-top: 4px solid #000;
      border-radius: 50%; animation: spin 1s linear infinite;
    }
    @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
    
    .checkmark { width: 64px; height: 64px; border-radius: 50%; display: block; stroke-width: 2; stroke: #fff; stroke-miterlimit: 10; margin: 0 auto; box-shadow: inset 0px 0px 0px #4caf50; animation: fill .4s ease-in-out .4s forwards, scale .3s ease-in-out .9s both; }
    .checkmark__circle { stroke-dasharray: 166; stroke-dashoffset: 166; stroke-width: 2; stroke-miterlimit: 10; stroke: #4caf50; fill: none; animation: stroke 0.6s cubic-bezier(0.65, 0, 0.45, 1) forwards; }
    .checkmark__check { transform-origin: 50% 50%; stroke-dasharray: 48; stroke-dashoffset: 48; animation: stroke 0.3s cubic-bezier(0.65, 0, 0.45, 1) 0.8s forwards; }
    @keyframes stroke { 100% { stroke-dashoffset: 0; } }
    @keyframes scale { 0%, 100% { transform: none; } 50% { transform: scale3d(1.1, 1.1, 1); } }
    @keyframes fill { 100% { box-shadow: inset 0px 0px 0px 40px #4caf50; } }
  `]
})
export class PaymentComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private location = inject(Location);
  private bookingService = inject(BookingService);
  private zone = inject(NgZone);
  private cdr = inject(ChangeDetectorRef);

  journeyId = '';
  selectedSeats: string[] = [];
  lockId = '';
  expiresAt = '';
  passengers: any[] = [];
  journeyDetails: any = null;
  pricePerSeat = 0;
  fetchError = false;
  
  boardingPointId = '';
  droppingPointId = '';
  boardingPointName = '';
  droppingPointName = '';

  loading = false;
  remainingTime = '';
  isExpiringSoon = false;
  private timerSub: Subscription | null = null;
  private PAYMENT_TIMEOUT_SECONDS = 300; // 5 minutes

  ngOnInit() {
    // Read passengers + journeyDetails passed via router state
    const state = this.location.getState() as any;
    if (state?.passengers) {
      this.passengers = state.passengers;
    }
    if (state?.journeyDetails) {
      this.journeyDetails = state.journeyDetails;
    }
    if (state?.boardingPointId) {
      this.boardingPointId = state.boardingPointId;
      this.droppingPointId = state.droppingPointId;
      this.boardingPointName = state.boardingPointName;
      this.droppingPointName = state.droppingPointName;
    }

    // Fallback: restore from localStorage if router state was lost (e.g. "Continue Booking")
    if (!this.passengers.length) {
      try {
        const stored = localStorage.getItem('pendingBooking');
        if (stored) {
          const data = JSON.parse(stored);
          this.passengers = data.passengers || [];
          this.journeyDetails = data.journeyDetails || null;
          this.boardingPointId = data.boardingPointId || '';
          this.droppingPointId = data.droppingPointId || '';
          this.boardingPointName = data.boardingPointName || '';
          this.droppingPointName = data.droppingPointName || '';
        }
      } catch { /* ignore parse errors */ }
    }

    this.route.queryParams.subscribe(params => {
      this.journeyId = params['journeyId'] || '';
      this.selectedSeats = (params['seats'] || '').split(',').filter(Boolean);
      this.lockId = params['lockId'] || '';
      this.pricePerSeat = +params['price'] || 0;

      // URL-decode expiresAt: Angular router encodes '+' as ' '
      const raw = params['expiresAt'] || '';
      this.expiresAt = raw.replace(/ /g, '+');
      if (this.expiresAt && !this.expiresAt.endsWith('Z')) {
        this.expiresAt += 'Z';
      }

      if (!this.passengers.length || !this.journeyId) {
        this.router.navigate(['/search']);
        return;
      }

      // Persist booking state to localStorage for 'Continue Booking' on home page
      this.saveBookingToStorage();

      // Fetch journey details fresh (ensures correct source/destination even after navigation)
      this.fetchJourneyDetails();

      // Start timer inside NgZone so Angular change detection fires each tick
      this.startTimer();
    });
  }

  startTimer() {
    // Determine remaining seconds: prioritize the server-provided expiresAt if available
    let deadlineMs: number;
    const deadlineKey = `paymentDeadline_${this.journeyId}`;

    if (this.expiresAt) {
      const serverExpiry = new Date(this.expiresAt).getTime();
      if (!isNaN(serverExpiry)) {
        if (serverExpiry <= Date.now()) {
          this.handleExpiry();
          return;
        }
        deadlineMs = serverExpiry;
        // Sync local storage with server expiry
        localStorage.setItem(deadlineKey, deadlineMs.toString());
      } else {
        // Invalid date format, fallback to default logic
        deadlineMs = this.getFallbackDeadline(deadlineKey);
      }
    } else {
      deadlineMs = this.getFallbackDeadline(deadlineKey);
    }

    if (!deadlineMs) return; // Should have been handled by handleExpiry if expired

    // RxJS interval runs inside Angular zone by default, but we wrap it in zone.run to be 100% sure
    if (this.timerSub) this.timerSub.unsubscribe();
    this.timerSub = interval(1000).subscribe(() => {
      this.zone.run(() => {
        const diff = deadlineMs - Date.now();

        if (diff <= 0) {
          this.handleExpiry();
          return;
        }

        const minutes = Math.floor(diff / 60000);
        const seconds = Math.floor((diff % 60000) / 1000);
        this.remainingTime = `${minutes}:${seconds.toString().padStart(2, '0')}`;
        this.isExpiringSoon = minutes < 1;
        this.cdr.markForCheck(); // Explicitly trigger check for timer
      });
    });
  }

  private getFallbackDeadline(deadlineKey: string): number {
    const storedDeadline = localStorage.getItem(deadlineKey);
    if (storedDeadline) {
      const ms = parseInt(storedDeadline, 10);
      if (!isNaN(ms) && ms > Date.now()) {
        return ms;
      }
    }
    // Final fallback: start a fresh 5-min countdown if nothing exists
    const freshMs = Date.now() + this.PAYMENT_TIMEOUT_SECONDS * 1000;
    localStorage.setItem(deadlineKey, freshMs.toString());
    return freshMs;
  }

  private handleExpiry() {
    if (this.timerSub) { this.timerSub.unsubscribe(); this.timerSub = null; }
    this.remainingTime = '0:00';
    this.isExpiringSoon = true;
    this.clearBookingFromStorage();
    localStorage.removeItem(`paymentDeadline_${this.journeyId}`);
    this.bookingService.releaseLocks(this.journeyId, this.selectedSeats).subscribe();
    alert('Your seat reservation has expired. Please select your seats again.');
    this.router.navigate(['/booking/seats', this.journeyId], {
      queryParams: { price: this.pricePerSeat }
    });
  }

  fetchJourneyDetails() {
    this.fetchError = false;
    this.bookingService.getLayout(this.journeyId).subscribe({
      next: (data) => {
        const base = this.pricePerSeat * this.selectedSeats.length;
        const fee = data.platformFeeType === 'Percentage' 
          ? Math.round(base * (data.platformFeeValue / 100)) 
          : data.platformFeeValue;
          
        this.journeyDetails = {
          source: data.source,
          destination: data.destination,
          departureTime: data.departureTime,
          busNumber: data.busNumber,
          busName: data.busName,
          basePrice: base,
          convenienceFee: fee,
          totalPrice: base + fee
        };
      },
      error: () => {
        this.fetchError = true;
        console.error('Failed to fetch journey details for journeyId:', this.journeyId);
      }
    });
  }

  paymentStatus: 'idle' | 'processing' | 'success' = 'idle';

  processPayment() {
    this.loading = true;
    this.paymentStatus = 'processing';
    
    // Use timeout to wait for the overlay to render before scrolling
    setTimeout(() => {
      document.getElementById('payment-overlay')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }, 100);

    // Simulate network delay for payment gateway
    setTimeout(() => {
      this.paymentStatus = 'success';
      
      // Keep the success animation visible for 1.5s before calling backend
      setTimeout(() => {
        // Map gender strings to backend enum integers: M=0, F=1, Other=2
        const genderMap: Record<string, number> = { 'Male': 0, 'Female': 1, 'Other': 2, 'M': 0, 'F': 1 };
        const mappedPassengers = this.passengers.map((p: any) => ({
          ...p,
          gender: genderMap[p.gender] ?? 0
        }));

        const request = {
          journeyId: this.journeyId,
          passengers: mappedPassengers,
          paymentToken: 'dummy-token-' + Date.now(),
          boardingPointId: this.boardingPointId || '00000000-0000-0000-0000-000000000000',
          droppingPointId: this.droppingPointId || '00000000-0000-0000-0000-000000000000'
        };

        this.bookingService.confirmBooking(request)
          .pipe(finalize(() => { 
            this.loading = false; 
            if (this.paymentStatus !== 'success') this.paymentStatus = 'idle';
          }))
          .subscribe({
            next: (res) => {
              if (this.timerSub) this.timerSub.unsubscribe();
              this.clearBookingFromStorage();
              localStorage.removeItem(`paymentDeadline_${this.journeyId}`);
              this.router.navigate(['/booking/history'], {
                queryParams: { success: true, bookingId: res.bookingId }
              });
            },
            error: (err) => {
              this.paymentStatus = 'idle';
              console.error('Payment confirmation failed:', err);
              const errorBody = err.error;
              let msg = 'An error occurred during payment processing.';
              
              if (typeof errorBody === 'string') {
                msg = errorBody;
              } else if (errorBody && typeof errorBody === 'object') {
                msg = errorBody.message || JSON.stringify(errorBody);
              }
              
              alert(msg);
            }
          });
      }, 1500);
    }, 1500);
  }

  ngOnDestroy() {
    if (this.timerSub) this.timerSub.unsubscribe();
  }

  private saveBookingToStorage() {
    const data = {
      journeyId: this.journeyId,
      seats: this.selectedSeats,
      lockId: this.lockId,
      expiresAt: this.expiresAt,
      pricePerSeat: this.pricePerSeat,
      passengers: this.passengers,
      boardingPointId: this.boardingPointId,
      droppingPointId: this.droppingPointId,
      boardingPointName: this.boardingPointName,
      droppingPointName: this.droppingPointName,
      journeyDetails: this.journeyDetails
    };
    
    let bookings: any[] = [];
    const raw = localStorage.getItem('pendingBookings');
    if (raw) {
      try { bookings = JSON.parse(raw); } catch { bookings = []; }
    }
    
    // Replace or Add
    const idx = bookings.findIndex(b => b.journeyId === this.journeyId);
    if (idx >= 0) bookings[idx] = data;
    else bookings.push(data);
    
    localStorage.setItem('pendingBookings', JSON.stringify(bookings));
  }

  private clearBookingFromStorage() {
    let bookings: any[] = [];
    const raw = localStorage.getItem('pendingBookings');
    if (raw) {
      try { 
        bookings = JSON.parse(raw); 
        bookings = bookings.filter(b => b.journeyId !== this.journeyId);
        localStorage.setItem('pendingBookings', JSON.stringify(bookings));
      } catch { }
    }
  }
}
