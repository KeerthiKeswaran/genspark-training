import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { BookingService } from '../../services/booking.service';
import { BookingModel, BookingDetail } from '../../models/booking.model';
import { NavbarComponent } from '../home/navbar/navbar';
import { FooterComponent } from '../home/footer/footer';
import { CancelBookingModalComponent } from './cancel-booking-modal/cancel-booking-modal';

type FilterStatus = 'All' | 'Confirmed' | 'Pending' | 'Cancelled';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent, CancelBookingModalComponent],
  templateUrl: './bookings.html',
  styleUrl: './bookings.css'
})
export class BookingsComponent implements OnInit, OnDestroy {
  public bookings = signal<BookingModel[]>([]);
  public selectedFilter = signal<FilterStatus>('Confirmed');
  public isLoading = signal(false);

  // QR Modal state
  public activeQrBooking = signal<BookingModel | null>(null);
  public showQrModal = signal(false);

  // Cancellation modal state
  public showCancelModal = signal(false);
  public bookingToCancel = signal<BookingModel | null>(null);

  // Feedback modal state
  public showFeedbackModal = signal(false);
  public activeFeedbackBooking = signal<BookingModel | null>(null);
  public feedbackRating = signal<number>(0);
  public feedbackReview = signal<string>('');

  private subscriptions = new Subscription();

  // Computed signals for filtering
  public filteredBookings = computed(() => {
    const list = this.bookings();
    const filter = this.selectedFilter();
    return list.filter(b => b.booking_Status === filter);
  });

  public confirmedCount = computed(() =>
    this.bookings().filter(b => b.booking_Status === 'Confirmed').length
  );

  public cancelledCount = computed(() =>
    this.bookings().filter(b => b.booking_Status === 'Cancelled').length
  );

  constructor(
    private bookingService: BookingService,
    private router: Router
  ) {}

  private resolveImageUrl(url: string | null | undefined): string | undefined {
    if (!url) return undefined;
    if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:')) {
      return url;
    }
    const cleanUrl = url.startsWith('/') ? url : '/' + url;
    return `http://localhost:5106${cleanUrl}`;
  }

  ngOnInit(): void {
    this.loadBookings();
  }

  public loadBookings(): void {
    this.isLoading.set(true);
    this.subscriptions.add(
      this.bookingService.getMyBookings().subscribe({
        next: (data) => {
          this.bookingService.getActiveVirtualLinks().subscribe({
            next: (activeLinks) => {
              const updated = data.map(booking => {
                if (booking.event_Type === 'Virtual' || booking.event_Type === 'Hybrid') {
                  const linkObj = activeLinks.find(al => al.booking_Id === booking.booking_Id);
                  booking.virtual_Url = (linkObj && linkObj.virtual_Url !== 'Disabled') 
                    ? linkObj.virtual_Url 
                    : 'Disabled';
                }
                booking.event_Image_Url = this.resolveImageUrl(booking.event_Image_Url);
                booking.qr_Code_Path = this.resolveImageUrl(booking.qr_Code_Path);
                return booking;
              });
              this.bookings.set(updated);
              this.isLoading.set(false);
            },
            error: () => {
              const fallbackMapped = data.map(booking => {
                booking.event_Image_Url = this.resolveImageUrl(booking.event_Image_Url);
                booking.qr_Code_Path = this.resolveImageUrl(booking.qr_Code_Path);
                return booking;
              });
              this.bookings.set(fallbackMapped);
              this.isLoading.set(false);
            }
          });
        },
        error: () => {
          this.isLoading.set(false);
        }
      })
    );
  }

  public setFilter(filter: FilterStatus): void {
    this.selectedFilter.set(filter);
  }

  // ── QR Modal ────────────────────────────────────────────

  public openQrModal(booking: BookingModel): void {
    this.activeQrBooking.set(booking);
    this.showQrModal.set(true);
  }

  public closeQrModal(): void {
    this.showQrModal.set(false);
    this.activeQrBooking.set(null);
  }

  // ── Cancellation Modal ───────────────────────────────────

  public openCancelModal(booking: BookingModel): void {
    this.bookingToCancel.set(booking);
    this.showCancelModal.set(true);
  }

  public closeCancelModal(): void {
    this.showCancelModal.set(false);
    this.bookingToCancel.set(null);
  }

  /** Called by CancelBookingModalComponent (cancelled) output */
  public onBookingCancelled(updatedBooking: BookingModel): void {
    const updated = this.bookings().map(b =>
      b.booking_Id === updatedBooking.booking_Id ? updatedBooking : b
    );
    this.bookings.set(updated);
  }

  // ── Feedback Modal ───────────────────────────────────────

  public openFeedbackModal(booking: BookingModel): void {
    this.activeFeedbackBooking.set(booking);
    this.feedbackRating.set(0);
    this.feedbackReview.set('');
    this.showFeedbackModal.set(true);
  }

  public closeFeedbackModal(): void {
    this.showFeedbackModal.set(false);
    this.activeFeedbackBooking.set(null);
    this.feedbackRating.set(0);
    this.feedbackReview.set('');
  }

  public setFeedbackRating(star: number): void {
    this.feedbackRating.set(star);
  }

  public submitFeedback(): void {
    const booking = this.activeFeedbackBooking();
    if (!booking) return;

    console.log('Submitting feedback for event ID:', booking.event_Id, {
      rating: this.feedbackRating(),
      review: this.feedbackReview()
    });

    // API call to POST api/event/{eventId}/feedback:
    this.bookingService.submitEventFeedback(booking.event_Id, {
      rating: this.feedbackRating(),
      review: this.feedbackReview()
    }).subscribe({
      next: () => {
        alert('Thank you for your feedback!');
        this.closeFeedbackModal();
      },
      error: (err) => {
        console.error('Failed to submit feedback', err);
        alert(err?.error?.message || 'Failed to submit feedback.');
      }
    });
  }

  // ── Utilities ────────────────────────────────────────────

  public joinMeeting(url?: string): void {
    if (url && url !== 'Disabled') {
      window.open(url, '_blank');
    } else {
      alert('Virtual meeting link is not active yet (only enabled during event timing) or the event has ended.');
    }
  }

  public formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      weekday: 'long', year: 'numeric', month: 'long', day: 'numeric'
    });
  }

  public formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('en-IN', {
      hour: '2-digit', minute: '2-digit', hour12: true
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
