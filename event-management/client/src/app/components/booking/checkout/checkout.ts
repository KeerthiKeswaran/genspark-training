import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import { BookingService } from '../../../services/booking.service';
import { TicketTierSelection, BookingModel } from '../../../models/booking.model';
import { BrowsedEventResponse } from '../../../models/event.model';
import { mockAllEvents } from '../../../data/event.mock';

type CheckoutStep = 'payment' | 'confirmation';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, NavbarComponent, FooterComponent],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css'
})
export class CheckoutComponent implements OnInit, OnDestroy {
  public currentStep = signal<CheckoutStep>('payment');
  public event = signal<BrowsedEventResponse | null>(null);
  public tiers = signal<TicketTierSelection[]>([]);
  public isProcessing = signal(false);
  public confirmedBooking = signal<BookingModel | null>(null);
  public showQrModal = signal(false);

  // Success animation state
  public isSuccessTickAnimating = signal(false);

  // Payment form state
  public cardNumber = '';
  public cardName = '';
  public cardExpiry = '';
  public cardCvv = '';
  
  // Specific input validation errors
  public paymentError = signal('');
  public cardNumberError = signal('');
  public cardNameError = signal('');
  public cardExpiryError = signal('');
  public cardCvvError = signal('');

  // Ticket Fee states
  public ticketFee = signal(0);
  public isFeeLoading = signal(false);

  private subscriptions = new Subscription();

  public subtotalAmount = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.price * t.quantity, 0)
  );

  public totalAmount = computed(() =>
    this.subtotalAmount() + this.ticketFee()
  );

  public totalTickets = computed(() =>
    this.tiers().reduce((sum, t) => sum + t.quantity, 0)
  );

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private bookingService: BookingService
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        const eventId = Number(params['eventId']);
        const qtyStr = params['quantities'] || '{}';
        
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

        try {
          const quantities = JSON.parse(qtyStr);
          const selectedTiers: TicketTierSelection[] = [];
          
          Object.keys(quantities).forEach(tierName => {
            const qty = quantities[tierName];
            if (qty > 0) {
              let price = found.minPrice || 250;
              if (tierName === 'VIP') price = price * 2.5;
              if (tierName === 'Backstage') price = price * 5;
              
              selectedTiers.push({
                tierName,
                price,
                quantity: qty,
                availableSeats: 50,
                totalSeats: 50
              });
            }
          });

          this.tiers.set(selectedTiers);
          
          if (selectedTiers.length === 0) {
            this.router.navigate(['/booking'], { queryParams: { eventId } });
            return;
          }

          this.calculateFee(eventId, quantities);
        } catch {
          this.router.navigate(['/booking'], { queryParams: { eventId } });
        }
      })
    );
  }

  private calculateFee(eventId: number, tierQuantities: Record<string, number>): void {
    this.isFeeLoading.set(true);
    this.ticketFee.set(0);

    this.subscriptions.add(
      this.bookingService.calculateTicketFee(eventId, tierQuantities).subscribe({
        next: (res) => {
          this.ticketFee.set(res.fee);
          setTimeout(() => {
            this.isFeeLoading.set(false);
          }, 1000);
        },
        error: () => {
          this.ticketFee.set(0);
          this.isFeeLoading.set(false);
        }
      })
    );
  }

  public onCardNumberInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const raw = input.value.replace(/\D/g, '').substring(0, 16);
    this.cardNumber = raw.replace(/(.{4})/g, '$1 ').trim();
    input.value = this.cardNumber;
    this.cardNumberError.set('');
  }

  public onNameInput(): void {
    this.cardNameError.set('');
  }

  public onExpiryInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let raw = input.value.replace(/\D/g, '').substring(0, 4);
    if (raw.length >= 2) {
      raw = raw.substring(0, 2) + '/' + raw.substring(2);
    }
    this.cardExpiry = raw;
    input.value = this.cardExpiry;
    this.cardExpiryError.set('');
  }

  public onCvvInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.cardCvv = input.value.replace(/\D/g, '').substring(0, 4);
    input.value = this.cardCvv;
    this.cardCvvError.set('');
  }

  public async confirmPayment(): Promise<void> {
    this.paymentError.set('');
    this.cardNumberError.set('');
    this.cardNameError.set('');
    this.cardExpiryError.set('');
    this.cardCvvError.set('');

    let hasError = false;

    const cleanCardNum = this.cardNumber.replace(/\s/g, '');
    if (!cleanCardNum) {
      this.cardNumberError.set('Card number is required.');
      hasError = true;
    } else if (cleanCardNum.length < 16) {
      this.cardNumberError.set('Card number must be 16 digits.');
      hasError = true;
    }

    if (!this.cardName.trim()) {
      this.cardNameError.set('Cardholder name is required.');
      hasError = true;
    }

    if (!this.cardExpiry.trim()) {
      this.cardExpiryError.set('Expiry date is required.');
      hasError = true;
    } else if (this.cardExpiry.length < 5 || !/^\d{2}\/\d{2}$/.test(this.cardExpiry)) {
      this.cardExpiryError.set('Expiry must be MM/YY.');
      hasError = true;
    }

    if (!this.cardCvv.trim()) {
      this.cardCvvError.set('CVV is required.');
      hasError = true;
    } else if (this.cardCvv.length < 3) {
      this.cardCvvError.set('CVV must be 3 or 4 digits.');
      hasError = true;
    }

    if (hasError) return;

    this.isProcessing.set(true);
    const event = this.event();
    if (!event) return;

    const tierQuantities: Record<string, number> = {};
    this.tiers().forEach(t => {
      tierQuantities[t.tierName] = t.quantity;
    });

    try {
      // Step 1: Initiate booking
      const initiateResult = await new Promise<{ booking_Id: number }>((resolve, reject) => {
        this.bookingService.initiateBooking({
          eventId: event.event_Id,
          tierQuantities
        }).subscribe({
          next: resolve,
          error: reject
        });
      });

      // Simulate payment delay
      await new Promise(r => setTimeout(r, 1200));

      // Step 2: Confirm booking
      const confirmed = await new Promise<BookingModel>((resolve, reject) => {
        this.bookingService.confirmBooking(initiateResult.booking_Id, {
          stripeChargeId: `ch_mock_${Date.now()}`,
          paymentMethod: 'card'
        }).subscribe({
          next: (booking) => {
            booking.event_Id = event.event_Id;
            booking.event_Title = event.title;
            booking.event_Type = event.eventType as any;
            booking.event_Date_Time = event.dateTime;
            booking.event_Image_Url = event.imageUrl;
            booking.event_Venue = event.venue_Name;
            booking.event_Region = event.region_Name;
            booking.total_Amount = this.totalAmount();
            booking.details = this.tiers().map(t => ({
              tier_Name: t.tierName,
              quantity: t.quantity,
              price: t.price
            }));
            resolve(booking);
          },
          error: reject
        });
      });

      this.confirmedBooking.set(confirmed);
      this.currentStep.set('confirmation');
      
      // Ensure page scrolls to top for checkmark animation
      window.scrollTo({ top: 0 });
      this.isSuccessTickAnimating.set(true);
      
      setTimeout(() => {
        this.isSuccessTickAnimating.set(false);
      }, 1500);

    } catch {
      this.paymentError.set('Payment failed. Please verify details or try a different card.');
    } finally {
      this.isProcessing.set(false);
    }
  }

  public openQrModal(): void {
    this.showQrModal.set(true);
  }

  public closeQrModal(): void {
    this.showQrModal.set(false);
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

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
