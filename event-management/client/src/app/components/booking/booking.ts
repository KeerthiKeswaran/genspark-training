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

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, FooterComponent],
  templateUrl: './booking.html',
  styleUrl: './booking.css'
})
export class BookingComponent implements OnInit, OnDestroy {
  public event = signal<BrowsedEventResponse | null>(null);
  public tiers = signal<TicketTierSelection[]>([]);
  public relatedEvents = signal<BrowsedEventResponse[]>([]);

  private subscriptions = new Subscription();

  public subtotalAmount = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.price * t.quantity, 0)
  );

  public totalTickets = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.quantity, 0)
  );

  constructor(
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        const eventId = Number(params['eventId']);
        if (!eventId) {
          this.router.navigate(['/browse']);
          return;
        }

        // Load event from mock data
        const found = mockAllEvents.find(e => e.event_Id === eventId);
        if (!found) {
          this.router.navigate(['/browse']);
          return;
        }
        this.event.set(found);

        // Build tier selection list with mock seat data
        const mockTierData: TicketTierSelection[] = this.buildMockTiers(found);
        this.tiers.set(mockTierData);

        // Load related events (matching type/region but excluding current event)
        const related = mockAllEvents
          .filter(e => e.event_Id !== found.event_Id && (e.eventType === found.eventType || e.region_Id === found.region_Id))
          .slice(0, 3);
        this.relatedEvents.set(related);
      })
    );
  }

  private buildMockTiers(event: BrowsedEventResponse): TicketTierSelection[] {
    const baseTiers: TicketTierSelection[] = [];

    if (event.minPrice !== undefined) {
      baseTiers.push({
        tierName: 'General',
        price: event.minPrice,
        quantity: 0,
        totalSeats: 200,
        availableSeats: 142
      });

      if (event.eventType !== 'Virtual') {
        baseTiers.push({
          tierName: 'VIP',
          price: event.minPrice * 2.5,
          quantity: 0,
          totalSeats: 50,
          availableSeats: 18
        });

        if (event.eventType === 'Hybrid' || event.eventType === 'Physical') {
          baseTiers.push({
            tierName: 'Backstage',
            price: event.minPrice * 5,
            quantity: 0,
            totalSeats: 10,
            availableSeats: 4
          });
        }
      }
    }

    return baseTiers;
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

  public proceedToCheckout(): void {
    if (this.totalTickets() === 0) return;

    const tierQuantities: Record<string, number> = {};
    this.tiers().filter(t => t.quantity > 0).forEach(t => {
      tierQuantities[t.tierName] = t.quantity;
    });

    const event = this.event();
    const eventId = event ? event.event_Id : 0;

    this.router.navigate(['/checkout'], {
      queryParams: {
        eventId,
        quantities: JSON.stringify(tierQuantities)
      }
    });
  }

  public navigateToRelatedEvent(eventId: number): void {
    this.router.navigate(['/booking'], { queryParams: { eventId } });
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
    alert('Contact functionality will be operational post organizers verification.');
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}

