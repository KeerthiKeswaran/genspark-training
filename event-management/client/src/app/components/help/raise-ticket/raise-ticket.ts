import { Component, OnInit, OnDestroy, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { BookingService } from '../../../services/booking.service';
import { AuthService } from '../../../services/auth.service';
import { AppStoreService } from '../../../store/app-store.service';
import { BookingModel } from '../../../models/booking.model';
import { SupportTicket } from '../help';

@Component({
  selector: 'app-raise-ticket',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent],
  templateUrl: './raise-ticket.html',
  styleUrl: './raise-ticket.css'
})
export class RaiseTicketComponent implements OnInit, OnDestroy {
  public bookings = signal<BookingModel[]>([]);
  public isLoggedIn = signal(false);

  // Form State
  public ticketBookingId = '';
  public ticketCategory = '';
  public ticketSubject = '';
  public ticketDetails = '';
  
  // File upload state
  public selectedFileName = signal('');
  
  // Alert & Animation State
  public ticketErrorMessage = signal('');
  public isSuccessAnimating = signal(false);
  public successTicketId = signal('');
  
  private redirectTimer: any = null;
  private subscriptions = new Subscription();

  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    // Navbar dropdown outside click handling
  }

  constructor(
    private bookingService: BookingService,
    private authService: AuthService,
    private store: AppStoreService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check authentication
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => {
        this.isLoggedIn.set(logged);
        if (logged) {
          this.loadUserBookings();
        }
      })
    );
  }

  private loadUserBookings(): void {
    this.bookingService.getMyBookings().subscribe(res => {
      this.bookings.set(res || []);
    });
  }

  public onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFileName.set(input.files[0].name);
    } else {
      this.selectedFileName.set('');
    }
  }

  public submitSupportTicket(): void {
    this.ticketErrorMessage.set('');

    if (!this.ticketCategory) {
      this.ticketErrorMessage.set('Please select a support category.');
      return;
    }
    if (!this.ticketSubject.trim()) {
      this.ticketErrorMessage.set('Please enter a ticket subject.');
      return;
    }
    if (!this.ticketDetails.trim()) {
      this.ticketErrorMessage.set('Please provide a description of your issue.');
      return;
    }

    const relatedIdVal = this.ticketBookingId && this.ticketBookingId !== 'General' 
      ? Number(this.ticketBookingId) 
      : undefined;

    const payload = {
      subject: this.ticketSubject.trim(),
      message: this.ticketDetails.trim(),
      requestType: this.ticketCategory.toLowerCase() || 'general',
      relatedId: relatedIdVal
    };

    this.bookingService.submitSupportTicket(payload).subscribe({
      next: (res) => {
        // Save in LocalStorage too
        const newTicket: SupportTicket = {
          ticketId: String(res?.ticket_Id || res?.Ticket_Id || `TKT-${Math.floor(Math.random() * 90000) + 10000}`),
          bookingId: this.ticketBookingId || 'General',
          category: this.ticketCategory,
          subject: this.ticketSubject.trim(),
          details: this.ticketDetails.trim(),
          status: 'Open',
          createdAt: new Date().toLocaleString('en-IN')
        };

        let raisedTickets: SupportTicket[] = [];
        const storedTickets = localStorage.getItem('raisedSupportTickets');
        if (storedTickets) {
          try {
            raisedTickets = JSON.parse(storedTickets);
          } catch {
            raisedTickets = [];
          }
        }

        raisedTickets = [newTicket, ...raisedTickets];
        localStorage.setItem('raisedSupportTickets', JSON.stringify(raisedTickets));

        // Show tick animation
        this.successTicketId.set(newTicket.ticketId);
        this.isSuccessAnimating.set(true);

        this.redirectTimer = setTimeout(() => {
          this.backToSupport();
        }, 5000);
      },
      error: (err) => {
        this.ticketErrorMessage.set(err?.error?.message || 'Failed to submit support ticket.');
      }
    });
  }

  public backToSupport(): void {
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
      this.redirectTimer = null;
    }
    this.router.navigate(['/help']);
  }

  ngOnDestroy(): void {
    if (this.redirectTimer) {
      clearTimeout(this.redirectTimer);
    }
    this.subscriptions.unsubscribe();
  }
}
