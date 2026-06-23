import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { BookingService } from '../../services/booking.service';
import { BookingModel, BookingDetail } from '../../models/booking.model';
import { NavbarComponent } from '../home/navbar/navbar';
import { FooterComponent } from '../home/footer/footer';

type FilterStatus = 'All' | 'Confirmed' | 'Pending' | 'Cancelled';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent],
  templateUrl: './bookings.html',
  styleUrl: './bookings.css'
})
export class BookingsComponent implements OnInit, OnDestroy {
  public bookings = signal<BookingModel[]>([]);
  public selectedFilter = signal<FilterStatus>('Confirmed'); // Default filter set to Confirmed
  public isLoading = signal(false);
  
  // Modal state
  public activeQrBooking = signal<BookingModel | null>(null);
  public showQrModal = signal(false);
  
  // Action cancellation state
  public isCancelling = signal(false);
  public cancelError = signal('');

  private subscriptions = new Subscription();

  // Computed signals for counts and filtering
  public filteredBookings = computed(() => {
    const list = this.bookings();
    const filter = this.selectedFilter();
    return list.filter(b => b.booking_Status === filter);
  });

  public confirmedCount = computed(() => {
    return this.bookings().filter(b => b.booking_Status === 'Confirmed').length;
  });

  public cancelledCount = computed(() => {
    return this.bookings().filter(b => b.booking_Status === 'Cancelled').length;
  });

  constructor(
    private bookingService: BookingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadBookings();
  }

  public loadBookings(): void {
    this.isLoading.set(true);
    this.subscriptions.add(
      this.bookingService.getMyBookings().subscribe({
        next: (data) => {
          this.bookings.set(data);
          this.isLoading.set(false);
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

  public openQrModal(booking: BookingModel): void {
    this.activeQrBooking.set(booking);
    this.showQrModal.set(true);
  }

  public closeQrModal(): void {
    this.showQrModal.set(false);
    this.activeQrBooking.set(null);
  }

  public cancelBooking(bookingId: number): void {
    if (!confirm('Are you sure you want to cancel this booking? This action cannot be undone.')) {
      return;
    }
    
    this.isCancelling.set(true);
    this.cancelError.set('');

    this.subscriptions.add(
      this.bookingService.cancelBooking(bookingId).subscribe({
        next: () => {
          const updated = this.bookings().map(b => {
            if (b.booking_Id === bookingId) {
              return { ...b, booking_Status: 'Cancelled' as const, checkIn_Status: 'Missed' as const };
            }
            return b;
          });
          this.bookings.set(updated);
          this.isCancelling.set(false);
        },
        error: () => {
          this.cancelError.set('Failed to cancel booking. Please try again later.');
          this.isCancelling.set(false);
        }
      })
    );
  }

  public joinMeeting(url?: string): void {
    if (url) {
      window.open(url, '_blank');
    } else {
      alert('Virtual meeting link is not available yet.');
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
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(amount);
  }

  public getDetailsSummary(details: BookingDetail[]): string {
    return details.map(d => `${d.tier_Name} × ${d.quantity}`).join(', ');
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
