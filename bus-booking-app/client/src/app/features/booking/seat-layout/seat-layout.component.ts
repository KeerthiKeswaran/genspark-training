import { Component, OnInit, OnDestroy, inject, signal, computed, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { BookingService } from '../../../core/services/booking.service';
import { SeatLayout } from '../../../core/models/booking.models';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-seat-layout',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Female Zone Advisory Toast -->
    <div class="female-toast" [class.toast-visible]="showFemaleToast()">
      <span class="toast-icon">🚺</span>
      <div class="toast-body">
        <strong>Female Travellers Only</strong>
        <span>This section is reserved for female passengers. Strict action will be taken against strangers found on boarding.</span>
      </div>
    </div>

    <div class="booking-container">
      <div class="layout-header">
        <div class="header-main">
          <h1>Select Your Seats</h1>
          <div class="bus-info" *ngIf="layout()">
            <span class="bus-name">{{ layout()!.busName }}</span>
            <span class="bus-number">Bus No: {{ layout()!.busNumber }}</span>
          </div>
        </div>
        
        <div class="legend">
          <div class="item"><span class="box available"></span> Available</div>
          <div class="item"><span class="box selected"></span> Selected</div>
          <div class="item"><span class="box blocked"></span> Blocked</div>
          <div class="item"><span class="box booked-female"></span> Female Booked</div>
          <div class="item"><span class="box booked-male"></span> Male Booked</div>
        </div>
      </div>

      <div class="main-content">
        <div class="bus-container">
          <div class="driver-cabin">
            <span class="steering">⊚</span>
          </div>
          
          <div class="seats-grid" *ngIf="!loading()">
            <div class="seat-row" *ngFor="let row of rows">
              <ng-container *ngFor="let col of cols">
                <!-- Aisle -->
                <div class="aisle" *ngIf="isAisle(col)"></div>
                
                <!-- Seat -->
                <div 
                  *ngIf="!isAisle(col)"
                  class="seat" 
                  [class.booked-female]="isGender(row, col, 'Female')"
                  [class.booked-male]="isGender(row, col, 'Male')"
                  [class.booked-other]="isStatus(row, col, 'Booked') && !isGender(row, col, 'Female') && !isGender(row, col, 'Male')"
                  [class.blocked]="isStatus(row, col, 'Blocked')"
                  [class.selected]="isSelected(row, col)"
                  (click)="toggleSeat(row, col)"
                >
                  <span class="seat-label">{{ getSeatLabel(row, col) }}</span>
                  <span class="seat-price" *ngIf="pricePerSeat > 0">₹{{ getSeatPrice(row) }}</span>
                </div>
              </ng-container>
            </div>
          </div>

          <div *ngIf="loading()" class="loading-overlay">
            <div class="spinner"></div>
          </div>
        </div>

        <div class="selection-summary">
          <h3>Booking Summary</h3>
          <div class="summary-details">
            <p><strong>Selected Seats:</strong> {{ selectedSeats().join(', ') || 'None' }}</p>
            <p><strong>Base Price:</strong> ₹{{ basePrice() }}</p>
            <p><strong>Convenience Fee:</strong> ₹{{ convenienceFee() }}</p>
            <p style="border-top: 1px solid #ccc; margin-top: 0.5rem; padding-top: 0.5rem; font-weight: 800;">
              <strong>Total Price:</strong> ₹{{ totalPrice() }}
            </p>
          </div>
          <button 
            class="btn-proceed" 
            [disabled]="selectedSeats().length === 0"
            (click)="proceedToDetails()"
          >
            Confirm Seats
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    /* Female advisory toast */
    .female-toast {
      position: fixed;
      top: 5rem;
      right: 2rem;
      z-index: 9999;
      background: #fff0f3;
      border: 1.5px solid #f48fb1;
      border-left: 5px solid #e91e8c;
      border-radius: 14px;
      padding: 1rem 1.5rem;
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      max-width: 340px;
      box-shadow: 0 20px 50px rgba(233, 30, 140, 0.15);
      opacity: 0;
      transform: translateX(30px);
      pointer-events: none;
      transition: opacity 0.4s ease, transform 0.4s ease;
    }
    .female-toast.toast-visible {
      opacity: 1;
      transform: translateX(0);
      pointer-events: auto;
    }
    .toast-icon { font-size: 1.6rem; flex-shrink: 0; line-height: 1; margin-top: 2px; }
    .toast-body { display: flex; flex-direction: column; gap: 0.3rem; }
    .toast-body strong { font-size: 0.9rem; font-weight: 800; color: #c2185b; }
    .toast-body span { font-size: 0.78rem; color: #880e4f; font-weight: 600; line-height: 1.4; }

    .booking-container { max-width: 1000px; margin: 4rem auto; padding: 2rem; font-family: 'Inter', sans-serif; }
    .layout-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 3rem; border-bottom: 3px solid #000; padding-bottom: 1rem; }
    .layout-header h1 { font-weight: 900; text-transform: uppercase; letter-spacing: -0.02em; }
    
    .legend { display: flex; gap: 1.5rem; }
    .legend .item { display: flex; align-items: center; gap: 0.5rem; font-size: 0.8rem; font-weight: 700; color: #555; text-transform: uppercase; }
    .box { width: 16px; height: 16px; border-radius: 4px; border: 1px solid #ccc; }
    .box.available { background: #fff; }
    .box.selected { background: #000; border-color: #000; }
    .box.blocked { background: #fff8e1; border-color: #ffe082; }
    .box.booked-female { background: #ffe4e1; border-color: #ffb6c1; }
    .box.booked-male { background: #e0f7fa; border-color: #81d4fa; }
    
    .duplicate-warning {
      background: #fff4e5;
      border: 1px solid #ffa117;
      padding: 1rem 1.25rem;
      border-radius: 12px;
      display: flex;
      gap: 0.75rem;
      align-items: center;
      width: 100%;
    }
    .warning-icon { font-size: 1.2rem; }
    .warning-text { display: flex; flex-direction: column; gap: 0.1rem; }
    .warning-text strong { color: #663c00; font-size: 0.9rem; font-weight: 800; }
    .warning-text p { color: #663c00; font-size: 0.75rem; font-weight: 600; margin: 0; }

    .bus-info { margin: 0.5rem 0 0; display: flex; align-items: center; gap: 1rem; }
    .bus-name { color: #000; font-weight: 800; font-size: 1rem; }
    .bus-number { color: #666; font-weight: 700; font-size: 0.85rem; background: #eee; padding: 0.2rem 0.6rem; border-radius: 4px; }

    .main-content { display: grid; grid-template-columns: 1fr 300px; gap: 4rem; }
    
    .bus-container { 
      background: #fff; 
      border: 1px solid #eee; 
      padding: 3rem; 
      position: relative;
      box-shadow: 0 15px 40px rgba(0,0,0,0.06);
      border-radius: 24px;
    }
    
    .driver-cabin { 
      border-bottom: 1px solid #eee; 
      padding-bottom: 1.5rem; 
      margin-bottom: 2rem; 
      text-align: right; 
    }
    .steering { font-size: 2rem; font-weight: 900; color: #ccc; }

    .seats-grid { display: flex; flex-direction: column; gap: 1rem; }
    .seat-row { display: flex; gap: 1rem; justify-content: center; }
    
    .seat {
      width: 54px;
      height: 54px;
      border: 1.5px solid #ddd;
      background: #ffffff;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      transition: background 0.15s ease, border-color 0.15s ease, transform 0.15s ease;
      border-radius: 10px;
      user-select: none;
    }
    .seat-label { font-weight: 800; font-size: 0.78rem; color: #333; line-height: 1; }
    .seat-price { font-size: 0.55rem; font-weight: 700; color: #999; margin-top: 2px; }
    
    .seat:hover:not(.booked-female):not(.booked-male):not(.booked-other):not(.blocked):not(.selected) {
      background: #e8e8e8;
      border-color: #999;
      transform: scale(1.08);
    }
    .seat:hover:not(.booked-female):not(.booked-male):not(.booked-other):not(.blocked):not(.selected) .seat-label { color: #000; }
    .seat:hover:not(.booked-female):not(.booked-male):not(.booked-other):not(.blocked):not(.selected) .seat-price { color: #666; }
    
    .seat.selected { background: #111; border-color: #111; transform: scale(1.05); }
    .seat.selected .seat-label { color: #fff; }
    .seat.selected .seat-price { color: rgba(255,255,255,0.6); }
    
    .seat.booked-female, .seat.booked-male { cursor: not-allowed; }
    
    .seat.booked-female { background: #ffe4e1; border-color: #ffb6c1; cursor: not-allowed; }
    .seat.booked-female .seat-label { color: #d81b60; }
    .seat.booked-female .seat-price { color: #f48fb1; }

    .seat.booked-male { background: #e0f7fa; border-color: #81d4fa; cursor: not-allowed; }
    .seat.booked-male .seat-label { color: #0277bd; }
    .seat.booked-male .seat-price { color: #4fc3f7; }

    /* Grey — booked but gender unknown (not shown in legend) */
    .seat.booked-other { background: #f0f0f0; border-color: #e0e0e0; cursor: not-allowed; }
    .seat.booked-other .seat-label { color: #aaa; }
    .seat.booked-other .seat-price { color: #ccc; }
    
    .seat.blocked { background: #fff8e1; border-color: #ffe082; cursor: not-allowed; }
    .seat.blocked .seat-label { color: #b8860b; }
    .seat.blocked .seat-price { color: #c9a227; }
    
    .aisle { width: 50px; }
 
    .selection-summary { 
      background: #fff; 
      border: 1px solid #eee; 
      padding: 2rem; 
      height: fit-content;
      box-shadow: 0 10px 30px rgba(0,0,0,0.05);
      border-radius: 16px;
    }
    .selection-summary h3 { font-weight: 900; text-transform: uppercase; margin-bottom: 1.5rem; border-bottom: 1px solid #eee; padding-bottom: 0.5rem; }
    .summary-details { margin-bottom: 2rem; }
    .summary-details p { margin-bottom: 1rem; font-size: 0.9rem; }
 
    .btn-proceed {
      width: 100%;
      padding: 1rem;
      background: #000;
      color: #fff;
      border: none;
      font-weight: 900;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      cursor: pointer;
      border-radius: 30px;
      transition: background 0.2s;
    }
    .btn-proceed:hover:not(:disabled) { background: #222; }
    .btn-proceed:disabled { background: #eee; color: #aaa; cursor: not-allowed; }

    .loading-overlay { display: flex; justify-content: center; align-items: center; height: 300px; }
    .spinner { width: 40px; height: 40px; border: 3px solid #f3f3f3; border-top: 3px solid #000; border-radius: 50%; animation: spin 1s linear infinite; }
    @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
  `]
})
export class SeatLayoutComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private bookingService = inject(BookingService);

  private cdr = inject(ChangeDetectorRef);

  journeyId = '';
  loading = signal(true);
  layout = signal<SeatLayout | null>(null);
  pricePerSeat = 0;
  showFemaleToast = signal(false);
  private toastTimer: any;

  rows = Array.from({ length: 10 }, (_, i) => i);
  cols = Array.from({ length: 5 }, (_, i) => i);
  aisleCol = 2;

  selectedSeats = signal<string[]>([]);
  basePrice = computed(() => {
    return this.selectedSeats().reduce((total, label) => {
      // Find row index from label (e.g. "A" -> 0, "B" -> 1)
      const row = label.charCodeAt(0) - 65;
      return total + this.getSeatPrice(row);
    }, 0);
  });
  
  convenienceFee = computed(() => {
    const layout = this.layout() as any;
    if (!layout || this.basePrice() === 0) return 0;
    
    const feeType = layout.platformFeeType || layout.PlatformFeeType || 'Fixed';
    const feeValue = layout.platformFeeValue ?? layout.PlatformFeeValue ?? 0;
    
    if (feeType === 'Percentage') {
      return Math.round(this.basePrice() * (feeValue / 100));
    }
    return feeValue;
  });
  
  totalPrice = computed(() => this.basePrice() + this.convenienceFee());

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.journeyId = params['id'];
      this.loadLayout();
    });

    this.route.queryParams.subscribe(params => {
      this.pricePerSeat = +params['price'] || 0;
    });
  }

  ngOnDestroy() {
    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
    }
  }

  loadLayout() {
    this.loading.set(true);
    this.bookingService.getLayout(this.journeyId)
      .pipe(finalize(() => {
        this.loading.set(false);
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (data) => {
          this.layout.set(data);
        },
        error: (err) => {
          console.error('Error loading layout', err);
          alert('Failed to load bus layout. Please try again.');
        }
      });
  }

  getSeatLabel(row: number, col: number): string {
    const char = String.fromCharCode(65 + row);
    const num = col < this.aisleCol ? col + 1 : col; // col 0,1 -> 1,2 | col 3,4 -> 3,4
    return `${char}${num}`;
  }

  getSeatPrice(row: number): number {
    if (this.pricePerSeat === 0) return 0;
    // Front and back rows get base price
    if (row <= 1 || row >= this.rows.length - 2) {
      return this.pricePerSeat;
    }
    // Middle comfort seats get a 25% premium
    return Math.round(this.pricePerSeat * 1.25);
  }

  isAisle(col: number): boolean {
    return col === this.aisleCol;
  }

  isStatus(row: number, col: number, status: 'Booked' | 'Blocked'): boolean {
    if (!this.layout()) return false;
    const label = this.getSeatLabel(row, col);
    return this.layout()!.unavailableSeats.some(s => s.seatNumber === label && s.status === status);
  }

  isGender(row: number, col: number, gender: 'Male' | 'Female'): boolean {
    if (!this.layout()) return false;
    const label = this.getSeatLabel(row, col);
    return this.layout()!.unavailableSeats.some(s => s.seatNumber === label && s.status === 'Booked' && s.gender === gender);
  }

  isSelected(row: number, col: number): boolean {
    return this.selectedSeats().includes(this.getSeatLabel(row, col));
  }

  toggleSeat(row: number, col: number) {
    if (this.isGender(row, col, 'Female') || this.isGender(row, col, 'Male') || this.isStatus(row, col, 'Booked') || this.isStatus(row, col, 'Blocked')) return;
    
    const label = this.getSeatLabel(row, col);
    const current = this.selectedSeats();

    // Check if adjacent seat is female-booked
    const adjLabel = this.getAdjacentLabel(label);
    if (adjLabel) {
      const adjFemale = this.layout()?.unavailableSeats.some(
        s => s.seatNumber === adjLabel && s.status === 'Booked' && s.gender === 'Female'
      );
      if (adjFemale && !current.includes(label)) {
        this.triggerFemaleToast();
      }
    }

    if (current.includes(label)) {
      this.selectedSeats.set(current.filter(s => s !== label));
    } else {
      this.selectedSeats.set([...current, label]);
    }
  }

  /** Returns the adjacent seat label using the same pair rules as the backend: 1↔2, 3↔4 */
  getAdjacentLabel(seatLabel: string): string | null {
    if (!seatLabel || seatLabel.length < 2) return null;
    const row = seatLabel[0];
    const num = parseInt(seatLabel.substring(1), 10);
    if (isNaN(num)) return null;
    if (num === 1) return `${row}2`;
    if (num === 2) return `${row}1`;
    if (num === 3) return `${row}4`;
    if (num === 4) return `${row}3`;
    return null;
  }

  triggerFemaleToast() {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.showFemaleToast.set(true);
    this.toastTimer = setTimeout(() => this.showFemaleToast.set(false), 5000);
  }

  proceedToDetails() {
    this.router.navigate(['/booking/passengers'], {
      queryParams: {
        journeyId: this.journeyId,
        seats: this.selectedSeats().join(','),
        price: this.pricePerSeat
      },
      state: { 
        journeyDetails: {
          source: this.layout()?.source,
          destination: this.layout()?.destination,
          departureTime: this.layout()?.departureTime,
          busNumber: this.layout()?.busNumber,
          busName: this.layout()?.busName,
          basePrice: this.basePrice(),
          convenienceFee: this.convenienceFee(),
          totalPrice: this.totalPrice()
        }
      }
    });
  }
}
