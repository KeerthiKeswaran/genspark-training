import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { TicketTierSelection } from '../../models/booking.model';
import { BrowsedEventResponse } from '../../models/event.model';
import { mockAllEvents } from '../../data/event.mock';
import { NavbarComponent } from '../home/navbar/navbar';
import { FooterComponent } from '../home/footer/footer';

import { ResolveDescriptionPipe } from '../../pipes/resolve-description.pipe';
import { EventService } from '../../services/event.service';
import { BookingService } from '../../services/booking.service';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent, ResolveDescriptionPipe],
  templateUrl: './booking.html',
  styleUrl: './booking.css'
})
export class BookingComponent implements OnInit, OnDestroy {
  public event = signal<BrowsedEventResponse | null>(null);
  public tiers = signal<TicketTierSelection[]>([]);
  public relatedEvents = signal<BrowsedEventResponse[]>([]);

  public isLoading = signal(false);

  private subscriptions = new Subscription();

  public subtotalAmount = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.price * t.quantity, 0)
  );

  public gstAmount = computed(() => this.subtotalAmount() * 0.18);

  public totalAmount = computed(() => this.subtotalAmount() + this.gstAmount());

  public totalTickets = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.quantity, 0)
  );

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private eventService: EventService,
    private bookingService: BookingService
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        const eventId = Number(params['eventId']);
        if (!eventId) {
          this.router.navigate(['/browse']);
          return;
        }

        this.isLoading.set(true);

        // 1. Try loading from history navigation state (all existing data from clicked card)
        const stateEvent = history.state?.event as BrowsedEventResponse;
        if (stateEvent && Number(stateEvent.event_Id) === eventId) {
          this.initializeEvent(stateEvent);
          this.isLoading.set(false);
          return;
        }

        // 2. Try loading from local mock events list
        const found = mockAllEvents.find(e => e.event_Id === eventId);
        if (found) {
          this.initializeEvent(found);
          this.isLoading.set(false);
          return;
        }

        // 3. Fall back to loading from the backend API directly using EventService
        this.eventService.getEventById(eventId).subscribe({
          next: (ev) => {
            this.isLoading.set(false);
            if (ev) {
              this.initializeEvent(ev);
            } else {
              this.router.navigate(['/browse']);
            }
          },
          error: () => {
            this.isLoading.set(false);
            this.router.navigate(['/browse']);
          }
        });
      })
    );
  }

  private initializeEvent(found: BrowsedEventResponse): void {
    this.event.set(found);

    // Build tier selection list from the event's actual ticketTiers
    const tierData: TicketTierSelection[] = (found.ticketTiers ?? []).map(t => ({
      tierName: t.tier_Name,
      price: t.price,
      quantity: 0,
      totalSeats: 200,
      availableSeats: Math.max(0, 200 - t.tickets_Sold)
    }));
    this.tiers.set(tierData);

    // Load related events (matching similar categories via browseEvents API)
    const category = found.category || 'General';
    this.eventService.browseEvents({ category, page: 1, size: 24 }).subscribe({
      next: (result) => {
        let list = (result.items || []).filter(e => e.event_Id !== found.event_Id);
        
        // Ensure at least 2 events by combining with all events if same-category count is too low
        if (list.length < 2) {
          this.eventService.browseEvents({ page: 1, size: 24 }).subscribe({
            next: (allResult) => {
              const extra = (allResult.items || []).filter(
                e => e.event_Id !== found.event_Id && !list.some(l => l.event_Id === e.event_Id)
              );
              list = [...list, ...extra].slice(0, 4);
              this.relatedEvents.set(list);
            },
            error: () => {
              this.relatedEvents.set(this.getMockRelatedEvents(found));
            }
          });
        } else {
          this.relatedEvents.set(list.slice(0, 4));
        }
      },
      error: () => {
        this.relatedEvents.set(this.getMockRelatedEvents(found));
      }
    });
  }

  private getMockRelatedEvents(found: BrowsedEventResponse): BrowsedEventResponse[] {
    return mockAllEvents
      .filter(e => e.event_Id !== found.event_Id && (e.event_Type === found.event_Type || e.region_Id === found.region_Id))
      .slice(0, 4);
  }

  public increaseQty(tierName: string): void {
    const tiers = this.tiers();
    const tier = tiers.find(t => t.tierName === tierName);
    if (!tier) return;
    if (tier.quantity >= Math.min(tier.availableSeats, 10)) return;
    tier.quantity++;
    this.tiers.set([...tiers]);
  }

  public decreaseQty(tierName: string): void {
    const tiers = this.tiers();
    const tier = tiers.find(t => t.tierName === tierName);
    if (!tier || tier.quantity === 0) return;
    tier.quantity--;
    this.tiers.set([...tiers]);
  }

  public getSeatFillPercent(tier: TicketTierSelection): number {
    return Math.round(((tier.totalSeats - tier.availableSeats) / tier.totalSeats) * 100);
  }

  public showReviewModal = signal(false);
  public isInitiatingBooking = signal(false);

  public proceedToCheckout(): void {
    if (this.totalTickets() === 0) return;
    this.showReviewModal.set(true);
  }

  public onCancelReview(): void {
    this.showReviewModal.set(false);
  }

  public onConfirmReview(): void {
    const tierQuantities: Record<string, number> = {};
    this.tiers().filter(t => t.quantity > 0).forEach(t => {
      tierQuantities[t.tierName] = t.quantity;
    });

    const event = this.event();
    const eventId = event ? event.event_Id : 0;

    this.isInitiatingBooking.set(true);

    this.subscriptions.add(
      this.bookingService.initiateBooking({
        eventId: eventId,
        tierQuantities: tierQuantities
      }).subscribe({
        next: (res) => {
          const pendingBookingId = res.booking_Id;
          const successUrl = `http://localhost:4200/checkout?eventId=${eventId}&session_id={CHECKOUT_SESSION_ID}&bookingId=${pendingBookingId}`;
          const cancelUrl = `http://localhost:4200/booking?eventId=${eventId}`;

          this.bookingService.createCheckoutSession(pendingBookingId, successUrl, cancelUrl).subscribe({
            next: (stripeRes) => {
              window.location.href = stripeRes.sessionUrl;
            },
            error: (err) => {
              this.isInitiatingBooking.set(false);
              console.error('Failed to create checkout session', err);
            }
          });
        },
        error: (err) => {
          this.isInitiatingBooking.set(false);
          console.error('Failed to initiate booking', err);
        }
      })
    );
  }

  public navigateToRelatedEvent(eventObj: any): void {
    this.router.navigate(['/booking'], { 
      queryParams: { eventId: eventObj.event_Id },
      state: { event: eventObj }
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
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

  public onContactOrg(): void {
    const orgEmail = this.event()?.organizer_Email;
    if (orgEmail) {
      window.open(`https://mail.google.com/mail/?view=cm&fs=1&tf=1&to=${orgEmail}`, '_blank');
    } else {
      alert('Contact functionality will be operational post organizers verification.');
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}

