import { Component, OnInit, signal, computed, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { StripeCardComponent, StripeService } from 'ngx-stripe';
import { StripeCardElementOptions, StripeElementsOptions } from '@stripe/stripe-js';
import { firstValueFrom, Observable, Subscription } from 'rxjs';
import { EventService } from '../../../services/event.service';
import { AuthService } from '../../../services/auth.service';
import { FooterComponent } from '../../home/footer/footer';
import { NavbarComponent } from '../../home/navbar/navbar';

@Component({
  selector: 'app-create-event',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, FormsModule, FooterComponent, NavbarComponent],
  templateUrl: './create-event.html',
  styleUrl: './create-event.css'
})
export class CreateEventComponent implements OnInit {
  private readonly STORAGE_KEY = 'createEventDraft';

  // Step 1: Form details, Step 2: Payment
  public currentStep = signal<'details' | 'payment'>('details');

  // Venues, Categories & Age Categories list
  public venuesList = signal<any[]>([]);
  public categoriesList = signal<string[]>([]);
  public ageCategoriesList = signal<{ key: string; display: string }[]>([]);

  // Form Model
  public title = '';
  public descriptionText = '';
  public imageFile: File | null = null;
  public imagePreviewUrl = signal<string | null>(null);
  public eventType = 'Physical';
  public category = 'Tech';
  public ageCategory = '';
  public dateTime = '';
  public durationHours = 2;
  public venueId: number | null = null;
  
  // Venues enriched with total capacity from SeatTiers
  public venuesWithCapacity = computed(() =>
    this.venuesList().map(v => ({
      ...v,
      capacity: (v.seatTiers ?? v.SeatTiers ?? [])
        .reduce((sum: number, t: any) => sum + (t.total_Seats ?? t.Total_Seats ?? t.totalSeats ?? 0), 0)
    }))
  );

  // Staff allocation details
  public requiresStaff = false;
  public estimatedStaffCount = signal(0);
  public estimatedStaffCost = signal(0);
  public isStaffCalculationLoading = signal(false);
  public staffErrorMsg = signal('');

  // Ticket Tiers configured dynamically based on selection
  public ticketTiers: any[] = [];

  // Platform settings (fees, rates)
  public platformSettings = signal<any | null>(null);

  // Event policy
  public policyDocument = signal<any | null>(null);
  public acceptPolicy = false;
  public acceptedPolicyTermsId = signal('');
  public showPolicyModal = signal(false);
  public policyModalContent = signal('');
  public isLoadingPolicy = signal(false);

  // Response after Step 1 create API
  public createdEventId = signal<number | null>(null);

  // Review Modal & Revert API states
  public showReviewModal = signal(false);
  public isInitiatingEvent = signal(false);
  public isPaymentConfirmed = false;
  public pendingEventPayload: any = null;

  // Stripe Payment Form Model
  @ViewChild(StripeCardComponent) card!: StripeCardComponent;
  
  public cardholderName = '';
  public isSubmittingPayment = signal(false);
  public showConfirmBackModal = signal(false);

  public cardOptions: StripeCardElementOptions = {
    style: {
      base: {
        color: '#121212',
        fontFamily: 'Outfit, sans-serif',
        fontSize: '16px',
        '::placeholder': {
          color: '#9ca3af'
        }
      }
    }
  };

  public elementsOptions: StripeElementsOptions = {
    locale: 'en'
  };

  // Virtual ticket price fetched from platform settings
  public virtualTicketPrice = computed(() => {
    const settings = this.platformSettings();
    return settings ? settings.virtual_Event_Activation_Fee ?? settings.Virtual_Event_Activation_Fee ?? null : null;
  });

  // Activation fees — plain getters so they react to plain property changes (eventType, durationHours)
  get activationFee(): number {
    const settings = this.platformSettings();
    if (!settings) return 0;
    const virtualFee  = settings.virtual_Event_Activation_Fee  ?? settings.Virtual_Event_Activation_Fee  ?? 0;
    const physicalFee = settings.physical_Event_Activation_Fee ?? settings.Physical_Event_Activation_Fee ?? 0;
    if (this.eventType === 'Virtual') return virtualFee;          // ₹500
    if (this.eventType === 'Hybrid')  return virtualFee + physicalFee; // ₹500 + ₹2000 = ₹2500
    return physicalFee;                                           // ₹2000
  }

  get selectedVenuePrice(): number {
    if (!this.venueId) return 0;
    const venue = this.venuesList().find(v => (v.venue_Id ?? v.Venue_Id) === Number(this.venueId));
    return venue ? (venue.hourly_Price ?? venue.Hourly_Price ?? 0) : 0;
  }

  get selectedVenueName(): string {
    if (!this.venueId) return '';
    const venue = this.venuesList().find(v => (v.venue_Id ?? v.Venue_Id) === Number(this.venueId));
    return venue ? (venue.name ?? venue.Name ?? '') : '';
  }

  get venueRentalCost(): number {
    return this.selectedVenuePrice * this.durationHours;
  }

  get baseTotalFees(): number {
    if (this.eventType === 'Virtual') return this.activationFee;
    return this.activationFee + this.venueRentalCost + this.estimatedStaffCost();
  }

  get gstAmount(): number {
    return this.baseTotalFees * 0.18;
  }

  get totalFees(): number {
    return this.baseTotalFees + this.gstAmount;
  }

  constructor(
    private eventService: EventService,
    private authService: AuthService,
    private http: HttpClient,
    private stripeService: StripeService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  private subscriptions = new Subscription();
  public isSuccessTickAnimating = signal(false);

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        const sessionId = params['session_id'];
        const eventId = Number(params['eventId']);
        
        if (sessionId && eventId) {
          this.confirmStripeEvent(sessionId, eventId);
          return;
        }
      })
    );

    this.loadDraft();
    this.loadVenues();
    this.loadCategories();
    this.loadAgeCategories();
    this.loadPlatformSettings();
    this.loadPolicy();
  }

  private confirmStripeEvent(sessionId: string, eventId: number): void {
    this.isInitiatingEvent.set(true);
    this.currentStep.set('payment'); // Go to confirmation page
    this.isSuccessTickAnimating.set(true); // Show animation while verifying

    this.eventService.confirmEvent(eventId, sessionId, 'stripe_checkout').subscribe({
      next: (res) => {
        this.isPaymentConfirmed = true;
        this.clearDraft();
        this.isInitiatingEvent.set(false);
        
        // Remove params from URL
        this.router.navigate([], {
          queryParams: { session_id: null, eventId: null },
          queryParamsHandling: 'merge',
          replaceUrl: true
        });

        // After a small delay to let animation play a bit if API was too fast
        setTimeout(() => {
          this.isSuccessTickAnimating.set(false);
        }, 1500);
      },
      error: (err) => {
        this.isInitiatingEvent.set(false);
        this.isSuccessTickAnimating.set(false);
        this.currentStep.set('details');
        const msg = err?.error?.Message || err?.error?.message || err.message || '';
        if (msg.includes('already') || msg.includes('Live')) {
          this.router.navigate(['/myevents']);
        } else {
          alert(msg || 'Payment confirmation failed.');
        }
      }
    });
  }

  public draftStatus = signal('Saved to draft');
  private saveTimeout: any = null;

  public saveDraft(): void {
    this.draftStatus.set('Saving...');
    const draft = {
      title: this.title,
      descriptionText: this.descriptionText,
      eventType: this.eventType,
      category: this.category,
      ageCategory: this.ageCategory,
      dateTime: this.dateTime,
      durationHours: this.durationHours,
      venueId: this.venueId,
      requiresStaff: this.requiresStaff,
      acceptPolicy: this.acceptPolicy,
      ticketTiers: this.ticketTiers.map(t => ({ ...t })),
      imagePreviewUrl: this.imagePreviewUrl(),
      estimatedStaffCount: this.estimatedStaffCount(),
      estimatedStaffCost: this.estimatedStaffCost()
    };
    sessionStorage.setItem(this.STORAGE_KEY, JSON.stringify(draft));

    if (this.saveTimeout) {
      clearTimeout(this.saveTimeout);
    }
    this.saveTimeout = setTimeout(() => {
      this.draftStatus.set('Saved to draft');
    }, 500);
  }

  private loadDraft(): void {
    try {
      const saved = sessionStorage.getItem(this.STORAGE_KEY);
      if (saved) {
        const draft = JSON.parse(saved);
        this.title = draft.title ?? '';
        this.descriptionText = draft.descriptionText ?? '';
        this.eventType = draft.eventType ?? 'Physical';
        this.category = draft.category ?? 'Tech';
        this.ageCategory = draft.ageCategory ?? '';
        this.dateTime = draft.dateTime ?? '';
        this.durationHours = draft.durationHours ?? 2;
        this.venueId = draft.venueId ?? null;
        this.requiresStaff = draft.requiresStaff ?? false;
        this.acceptPolicy = draft.acceptPolicy ?? false;
        this.ticketTiers = draft.ticketTiers ?? [];
        if (draft.estimatedStaffCount) this.estimatedStaffCount.set(draft.estimatedStaffCount);
        if (draft.estimatedStaffCost) this.estimatedStaffCost.set(draft.estimatedStaffCost);
        if (draft.imagePreviewUrl) {
          this.imagePreviewUrl.set(draft.imagePreviewUrl);
          fetch(draft.imagePreviewUrl)
            .then(res => res.blob())
            .then(blob => {
              this.imageFile = new File([blob], 'draft_image.png', { type: blob.type });
            })
            .catch(err => console.error('Failed to reconstruct image file from draft', err));
        }
      }
    } catch {
      sessionStorage.removeItem(this.STORAGE_KEY);
    }
  }

  private clearDraft(): void {
    sessionStorage.removeItem(this.STORAGE_KEY);
  }

  private loadPlatformSettings(): void {
    this.eventService.getPlatformSettings().subscribe({
      next: (settings) => {
        this.platformSettings.set(settings);
      },
      error: () => {
        console.error('Failed to load platform settings');
      }
    });
  }

  private loadPolicy(): void {
    this.authService.getConsentDocument('EventCreation').subscribe({
      next: (doc) => {
        this.policyDocument.set(doc);
        if (doc?.termsId) {
          this.acceptedPolicyTermsId.set(doc.termsId);
        }
      },
      error: (err) => {
        console.error('Failed to load event creation policy', err);
      }
    });
  }

  public openPolicyModal(event: Event): void {
    event.preventDefault();
    const doc = this.policyDocument();
    if (!doc?.filePath) {
      this.policyModalContent.set('<p>No policy document available.</p>');
      this.showPolicyModal.set(true);
      return;
    }

    this.isLoadingPolicy.set(true);
    this.showPolicyModal.set(true);

    const fileUrl = doc.filePath.startsWith('http') ? doc.filePath : `http://localhost:5106${doc.filePath}`;
    this.http.get(fileUrl, { responseType: 'text' }).subscribe({
      next: (content) => {
        let lines = content.split('\n');
        lines = lines.filter(line => {
          const trimmed = line.trim().toLowerCase();
          return !trimmed.startsWith('**version:**') &&
                 !trimmed.startsWith('**policy id:**') &&
                 !trimmed.startsWith('version:') &&
                 !trimmed.startsWith('policy id:');
        });
        const filteredContent = lines.join('\n');
        const formatted = filteredContent
          .replace(/^### (.+)$/gm, '<h3>$1</h3>')
          .replace(/^## (.+)$/gm, '<h2>$1</h2>')
          .replace(/^# (.+)$/gm, '<h1>$1</h1>')
          .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
          .replace(/^- (.+)$/gm, '<li>$1</li>')
          .replace(/(<li>.*<\/li>\n?)+/g, '<ul>$&</ul>')
          .replace(/\n\n/g, '</p><p>')
          .replace(/\n/g, '<br/>');
        this.policyModalContent.set(`<p>${formatted}</p>`);
        this.isLoadingPolicy.set(false);
      },
      error: () => {
        this.policyModalContent.set('<p>Unable to load the policy document contents.</p>');
        this.isLoadingPolicy.set(false);
      }
    });
  }

  public closePolicyModal(): void {
    this.showPolicyModal.set(false);
    this.policyModalContent.set('');
  }

  private loadVenues(): void {
    this.eventService.getVenues().subscribe({
      next: (venues) => {
        this.venuesList.set(venues);
      }
    });
  }

  private loadCategories(): void {
    this.eventService.getCategories().subscribe({
      next: (cats) => {
        this.categoriesList.set(cats);
        if (cats.length > 0 && !this.category) {
          this.category = cats[0];
        }
      },
      error: () => {
        const fallbacks = ['Tech', 'Conference', 'Music', 'Sports', 'Workshop', 'Education', 'Arts', 'Food', 'Wellness'];
        this.categoriesList.set(fallbacks);
        this.category = fallbacks[0];
      }
    });
  }

  private loadAgeCategories(): void {
    this.eventService.getAgeCategories().subscribe({
      next: (list) => {
        this.ageCategoriesList.set(list);
        if (list.length > 0) {
          this.ageCategory = list[0].key;
        }
      },
      error: () => {
        const fallbacks = [
          { key: 'ALL', display: 'Unrestricted' },
          { key: 'KID', display: '5 years +' },
          { key: 'ADL', display: '18+' }
        ];
        this.ageCategoriesList.set(fallbacks);
        this.ageCategory = fallbacks[0].key;
      }
    });
  }

  public onEventTypeChange(): void {
    if (this.eventType === 'Virtual') {
      this.venueId = null;
      this.requiresStaff = false;
      this.ticketTiers = [];
    } else {
      this.onVenueChange();
    }
    this.saveDraft();
  }

  public onVenueChange(): void {
    // 1. Calculate staff if toggled on
    if (this.requiresStaff) {
      if (this.venueId) {
        this.calculateStaffEstimation();
      } else {
        this.requiresStaff = false;
      }
    }

    // 2. Set dynamic ticket tiers from the selected venue
    const selectedVenue = this.venuesList().find(
      (v: any) => (v.venue_Id ?? v.Venue_Id) === Number(this.venueId)
    );

    if (selectedVenue) {
      const seatTiers = selectedVenue.seatTiers ?? selectedVenue.SeatTiers ?? [];
      this.ticketTiers = seatTiers.map((t: any) => ({
        tierName: t.tier_Name ?? t.Tier_Name ?? t.tierName ?? '',
        price: 0, // Default price in INR
        capacity: t.total_Seats ?? t.Total_Seats ?? t.totalSeats ?? 0,
        enabled: true
      }));
    } else {
      this.ticketTiers = [];
    }
    this.saveDraft();
  }

  public onDateTimeChange(): void {
    if (this.requiresStaff) {
      this.calculateStaffEstimation();
    }
    this.saveDraft();
  }

  public onStaffToggleChange(): void {
    if (!this.venueId || !this.dateTime || !this.durationHours) {
      this.requiresStaff = false;
      alert('Please specify Date & Time, Duration, and a Physical Venue before requesting staff.');
      return;
    }

    if (this.requiresStaff) {
      this.calculateStaffEstimation();
    } else {
      this.estimatedStaffCount.set(0);
      this.estimatedStaffCost.set(0);
      this.staffErrorMsg.set('');
    }
    this.saveDraft();
  }

  public calculateStaffEstimation(): void {
    if (!this.venueId || !this.dateTime || !this.durationHours) {
      this.staffErrorMsg.set('Please select Venue, Date/Time, and Duration first before estimating staff.');
      this.requiresStaff = false;
      return;
    }

    this.staffErrorMsg.set('');
    this.isStaffCalculationLoading.set(true);

    const dateStr = new Date(this.dateTime).toISOString();
    console.log('checkStaff payload:', { venueId: Number(this.venueId), dateTime: dateStr, duration: this.durationHours });
    this.eventService.checkStaffAvailability(Number(this.venueId), dateStr, this.durationHours).subscribe({
      next: (res) => {
        console.log('checkStaff response:', res);
        this.estimatedStaffCount.set(res.requiredStaffCount ?? res.RequiredStaffCount ?? 0);
        this.estimatedStaffCost.set(res.staffingCost ?? res.StaffingCost ?? 0);
        this.isStaffCalculationLoading.set(false);
        this.saveDraft();
      },
      error: (err) => {
        console.error('checkStaff error:', err);
        this.staffErrorMsg.set(err?.error?.message || err?.message || 'Staff availability calculation failed.');
        this.isStaffCalculationLoading.set(false);
        this.requiresStaff = false;
        this.saveDraft();
      }
    });
  }

  public isDragOver = signal(false);

  public onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  public onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  public onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const file = event.dataTransfer.files[0];
      if (file.type === 'image/jpeg' || file.type === 'image/png') {
        this.imageFile = file;
        const reader = new FileReader();
        reader.onload = (e) => this.imagePreviewUrl.set(e.target?.result as string);
        reader.readAsDataURL(file);
        this.saveDraft();
      }
    }
  }

  public onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.imageFile = input.files[0];
      const reader = new FileReader();
      reader.onload = (e) => this.imagePreviewUrl.set(e.target?.result as string);
      reader.readAsDataURL(this.imageFile);
      this.saveDraft();
    }
  }

  public async onSubmitDetails(): Promise<void> {
    if (!this.title || !this.dateTime || !this.category) {
      alert('Please fill out all mandatory fields.');
      return;
    }

    if (this.eventType !== 'Virtual' && !this.venueId) {
      alert('Please select a Venue for physical/hybrid event.');
      return;
    }

    if (this.requiresStaff && this.estimatedStaffCount() === 0) {
      alert('Please wait for staff estimation calculation to complete.');
      return;
    }

    if (this.eventType !== 'Virtual') {
      const activeTiers = this.ticketTiers.filter(t => t.enabled);
      if (activeTiers.length === 0) {
        alert('Please enable at least one ticket tier.');
        return;
      }
      if (activeTiers.some(t => !t.price || t.price <= 0)) {
        alert('All enabled ticket tiers must have a price greater than 0.');
        return;
      }
    }

    if (!this.acceptPolicy) {
      alert('You must accept the Event Creation Policy before proceeding.');
      return;
    }

    // Step 1: Upload description text as .txt file
    let descriptionUrl = '';
    try {
      const descResult = await firstValueFrom(this.eventService.uploadDescription(this.descriptionText));
      descriptionUrl = descResult.url;
    } catch {
      alert('Failed to save description file.');
      return;
    }

    // Step 2: Upload image if selected
    let imageUrl = '';
    if (this.imageFile) {
      try {
        const imgResult = await firstValueFrom(this.eventService.uploadImage(this.imageFile));
        imageUrl = imgResult.url;
      } catch {
        alert('Failed to upload event image.');
        return;
      }
    }

    // Filter enabled tiers (only for physical/hybrid)
    const tiersPayload = this.eventType === 'Virtual'
      ? []
      : this.ticketTiers
          .filter(t => t.enabled)
          .map(t => ({
            tierName: t.tierName,
            price: t.price
          }));

    if (this.eventType !== 'Virtual' && tiersPayload.length === 0) {
      alert('Please enable and configure at least one ticket tier.');
      return;
    }

    const payload = {
      eventType: this.eventType,
      title: this.title,
      category: this.category,
      ageCategory: this.ageCategory,
      descriptionUrl,
      imageUrl,
      dateTime: new Date(this.dateTime).toISOString(),
      durationHours: this.durationHours,
      requiresStaff: this.requiresStaff,
      venueId: this.eventType === 'Virtual' ? null : Number(this.venueId),
      acceptedPolicyId: this.acceptedPolicyTermsId(),
      ticketTiers: tiersPayload
    };

    this.pendingEventPayload = payload;
    this.showReviewModal.set(true);
  }

  public onConfirmReview(): void {
    if (!this.pendingEventPayload) return;

    this.isInitiatingEvent.set(true);
    this.eventService.createEvent(this.pendingEventPayload).subscribe({
      next: (res) => {
        const pendingEventId = res.event_Id;
        this.createdEventId.set(pendingEventId);
        
        const successUrl = `http://localhost:4200/myevents/create?session_id={CHECKOUT_SESSION_ID}&eventId=${pendingEventId}`;
        const cancelUrl = `http://localhost:4200/myevents/create`;
        
        this.eventService.createCheckoutSession(pendingEventId, successUrl, cancelUrl).subscribe({
          next: (stripeRes) => {
            window.location.href = stripeRes.sessionUrl;
          },
          error: (err) => {
            this.isInitiatingEvent.set(false);
            console.error('Failed to create checkout session', err);
            alert('Failed to initialize payment gateway.');
          }
        });
      },
      error: (err) => {
        this.isInitiatingEvent.set(false);
        alert(err?.error?.message || 'Failed to initialize event listing.');
      }
    });
  }

  public onCancelReview(): void {
    this.showReviewModal.set(false);
  }

  public canDeactivate(): Observable<boolean> | boolean {
    const id = this.createdEventId();
    if (id && this.currentStep() === 'payment' && !this.isPaymentConfirmed) {
      return new Observable<boolean>(observer => {
        this.eventService.revertEvent(id).subscribe({
          next: () => {
            observer.next(true);
            observer.complete();
          },
          error: () => {
            // Permit navigation even if API fails to revert
            observer.next(true);
            observer.complete();
          }
        });
      });
    }
    return true;
  }

  public onBackToDetails(): void {
    this.showConfirmBackModal.set(true);
  }

  public confirmRevertAndBack(): void {
    const id = this.createdEventId();
    if (id) {
      this.eventService.revertEvent(id).subscribe({
        next: () => {
          this.currentStep.set('details');
          this.showConfirmBackModal.set(false);
        },
        error: () => {
          alert('Could not revert the pending event registration.');
          this.showConfirmBackModal.set(false);
        }
      });
    } else {
      this.currentStep.set('details');
      this.showConfirmBackModal.set(false);
    }
  }

  public onSubmitPayment(): void {
    if (!this.cardholderName) {
      alert('Please enter the cardholder name.');
      return;
    }

    this.isSubmittingPayment.set(true);

    this.stripeService.createToken(this.card.element, { name: this.cardholderName }).subscribe({
      next: (result) => {
        if (result.token) {
          const id = this.createdEventId();
          if (id) {
            this.eventService.confirmEvent(id, result.token.id, 'card').subscribe({
              next: () => {
                this.isPaymentConfirmed = true;
                this.clearDraft();
                this.isSubmittingPayment.set(false);
                alert('Payment Successful! Your event is now live.');
                this.router.navigate(['/myevents']);
              },
              error: (err) => {
                this.isSubmittingPayment.set(false);
                alert(err?.error?.message || 'Payment confirmation failed.');
              }
            });
          }
        } else if (result.error) {
          this.isSubmittingPayment.set(false);
          alert(result.error.message || 'Stripe card tokenization failed.');
        }
      },
      error: (err) => {
        this.isSubmittingPayment.set(false);
        alert(err?.message || 'Payment token generation failed.');
      }
    });
  }
}
