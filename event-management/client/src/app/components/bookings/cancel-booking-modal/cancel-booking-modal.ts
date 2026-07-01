import {
  Component,
  Input,
  Output,
  EventEmitter,
  signal,
  OnInit,
  OnDestroy,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { of, delay } from 'rxjs';
import { Subscription } from 'rxjs';
import { BookingService } from '../../../services/booking.service';
import { BookingModel, BookingDetail } from '../../../models/booking.model';
import { CancellationPolicyDocComponent } from '../cancellation-policy-doc/cancellation-policy-doc';

@Component({
  selector: 'app-cancel-booking-modal',
  standalone: true,
  imports: [CommonModule, CancellationPolicyDocComponent],
  templateUrl: './cancel-booking-modal.html',
  styleUrl: './cancel-booking-modal.css'
})
export class CancelBookingModalComponent implements OnInit, OnDestroy {
  @Input({ required: true }) booking!: BookingModel;

  /** Emitted when modal requests close */
  @Output() closed = new EventEmitter<void>();

  /** Emitted after successful cancellation with the updated booking */
  @Output() cancelled = new EventEmitter<BookingModel>();

  // Refund estimate state
  public isLoadingRefund = signal(true);
  public estimatedRefundAmount = signal<number | null>(null);

  // Policy document dialog
  public showPolicyDoc = signal(false);

  // Cancellation action state
  public isCancelling = signal(false);
  public cancelError = signal('');

  private bookingService = inject(BookingService);
  private subscriptions = new Subscription();

  ngOnInit(): void {
    this.loadRefundEstimate();
  }

  /**
   * Loads the estimated refund amount for this booking.
   *
   * TODO: Replace the mock below with a real API call when the endpoint is ready:
   *   POST /api/booking/{bookingId}/refund-estimate  (no body needed)
   *   Returns: { estimatedRefund: number }
   *
   * Example implementation:
   *   this.subscriptions.add(
   *     this.http.post<{ estimatedRefund: number }>(
   *       `${environment.apiUrl}/api/booking/${this.booking.booking_Id}/refund-estimate`, {}
   *     ).subscribe({
   *       next: (res) => {
   *         this.estimatedRefundAmount.set(res.estimatedRefund);
   *         this.isLoadingRefund.set(false);
   *       },
   *       error: () => {
   *         this.estimatedRefundAmount.set(null);
   *         this.isLoadingRefund.set(false);
   *       }
   *     })
   *   );
   */
  private loadRefundEstimate(): void {
    this.isLoadingRefund.set(true);

    // ── MOCK REFUND CALCULATION (matches server-side policy) ──────────────────
    const now = new Date();
    const eventDate = new Date(this.booking.event_Date_Time);
    const hoursUntil = (eventDate.getTime() - now.getTime()) / (1000 * 60 * 60);
    const totalPaid = this.booking.total_Amount ?? 0;

    let refundAmount: number;
    if (hoursUntil > 48) {
      refundAmount = totalPaid * 0.90;
    } else if (hoursUntil >= 12) {
      refundAmount = totalPaid * 0.50;
    } else {
      refundAmount = 0;
    }
    // ─────────────────────────────────────────────────────────────────────────

    // Simulate a 1.2-second API round-trip so the skeleton is visible
    this.subscriptions.add(
      of(refundAmount).pipe(delay(1200)).subscribe(amount => {
        this.estimatedRefundAmount.set(amount);
        this.isLoadingRefund.set(false);
      })
    );
  }

  /** Returns the colour associated with the refund amount */
  public getRefundColor(): string {
    const amount = this.estimatedRefundAmount();
    if (amount === null) return '#9ca3af';
    if (amount > 0) {
      const totalPaid = this.booking.total_Amount ?? 1;
      const pct = (amount / totalPaid) * 100;
      return pct >= 80 ? '#10b981' : '#f59e0b';
    }
    return '#ef4444';
  }

  /** Returns a human-readable status label */
  public getRefundStatusLabel(): string {
    const amount = this.estimatedRefundAmount();
    if (amount === null) return '—';
    if (amount <= 0) return 'Non-Refundable';
    const totalPaid = this.booking.total_Amount ?? 1;
    const pct = Math.round((amount / totalPaid) * 100);
    return `${pct}% Refund Eligible`;
  }

  public openPolicyDoc(event: Event): void {
    event.preventDefault();
    this.showPolicyDoc.set(true);
  }

  public closePolicyDoc(event?: Event): void {
    event?.stopPropagation();
    this.showPolicyDoc.set(false);
  }

  public close(): void {
    this.closed.emit();
  }

  public confirmCancellation(): void {
    this.isCancelling.set(true);
    this.cancelError.set('');

    this.subscriptions.add(
      this.bookingService.cancelBooking(this.booking.booking_Id).subscribe({
        next: () => {
          const updatedBooking: BookingModel = {
            ...this.booking,
            booking_Status: 'Cancelled' as const,
            checkIn_Status: 'Missed' as const
          };
          this.isCancelling.set(false);
          this.cancelled.emit(updatedBooking);
          this.closed.emit();
        },
        error: () => {
          this.cancelError.set('Cancellation failed. Please try again or contact support.');
          this.isCancelling.set(false);
        }
      })
    );
  }

  public formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
    });
  }

  public formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency', currency: 'INR', maximumFractionDigits: 0
    }).format(amount);
  }

  public getDetailsSummary(details: BookingDetail[]): string {
    return details.map(d => `${d.tier_Name} × ${d.quantity}`).join(', ');
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
