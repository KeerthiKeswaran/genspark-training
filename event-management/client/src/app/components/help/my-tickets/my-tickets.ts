import { Component, OnInit, OnDestroy, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { AppStoreService } from '../../../store/app-store.service';
import { SupportTicket } from '../help';

type StatusFilter = 'All' | 'Open' | 'In Progress' | 'Resolved';

@Component({
  selector: 'app-my-tickets',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NavbarComponent, FooterComponent],
  templateUrl: './my-tickets.html',
  styleUrl: './my-tickets.css'
})
export class MyTicketsComponent implements OnInit, OnDestroy {
  public currentTickets = signal<SupportTicket[]>([]);
  public selectedStatusFilter = signal<StatusFilter>('All');
  public isLoggedIn = signal(false);

  private subscriptions = new Subscription();

  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    // Navbar dropdown close handler
  }

  // Computed signal for filtered tickets
  public filteredTickets = computed(() => {
    const list = this.currentTickets();
    const filter = this.selectedStatusFilter();
    if (filter === 'All') return list;
    return list.filter(ticket => ticket.status === filter);
  });

  constructor(
    private store: AppStoreService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Check authentication
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => {
        this.isLoggedIn.set(logged);
      })
    );

    this.loadTickets();
  }

  public loadTickets(): void {
    const storedTickets = localStorage.getItem('raisedSupportTickets');
    if (storedTickets) {
      try {
        this.currentTickets.set(JSON.parse(storedTickets));
      } catch {
        this.loadDefaultTickets();
      }
    } else {
      this.loadDefaultTickets();
    }
  }

  private loadDefaultTickets(): void {
    const defaultTickets: SupportTicket[] = [
      {
        ticketId: 'TKT-78241',
        bookingId: '101',
        category: 'QR Code not downloading',
        subject: 'QR code display issue on mobile web',
        details: 'When loading the ticket on Safari, the QR grid cells overlap. Can you confirm if my check-in QR is registered?',
        status: 'Resolved',
        createdAt: new Date(Date.now() - 36 * 3600 * 1000).toLocaleString('en-IN')
      },
      {
        ticketId: 'TKT-90142',
        bookingId: '102',
        category: 'Payment Issue',
        subject: 'Double charge confirmation check',
        details: 'My card was charged twice but I only got one seat confirmation email. Booking reference: #102.',
        status: 'In Progress',
        createdAt: new Date(Date.now() - 4 * 3600 * 1000).toLocaleString('en-IN')
      }
    ];
    this.currentTickets.set(defaultTickets);
    localStorage.setItem('raisedSupportTickets', JSON.stringify(defaultTickets));
  }

  public setFilter(filter: StatusFilter): void {
    this.selectedStatusFilter.set(filter);
  }

  public backToSupport(): void {
    this.router.navigate(['/help']);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
