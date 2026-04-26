import { Component, inject, OnInit, OnDestroy, HostListener, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { SearchService } from '../../core/services/search.service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, switchMap, Subscription, of } from 'rxjs';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main>
      <!-- Continue Booking Banners -->
      <section class="continue-banner" *ngFor="let b of pendingBookings">
        <div class="banner-content">
          <div class="banner-left">
            <div class="banner-badge">⏳ Booking in Progress</div>
            <div class="banner-route">{{ b.journeyDetails?.source }} → {{ b.journeyDetails?.destination }}</div>
            <div class="banner-meta">
              <span>Seats: {{ b.seats?.join(', ') }}</span>
              <span>•</span>
              <span>{{ b.passengers?.length }} Passenger{{ b.passengers?.length > 1 ? 's' : '' }}</span>
            </div>
          </div>
          <div class="banner-actions">
            <button class="btn-continue" (click)="continuePendingBooking(b)">Continue Payment →</button>
          </div>
        </div>
      </section>

      <section class="booking-search-section">
        <div class="search-card">
          <h1>Find Your Next Journey</h1>
          <div class="search-form-container">
            <div class="search-form">
              <div class="search-field">
                <label>From</label>
                <input type="text" [(ngModel)]="from" 
                       (input)="onFromChange()" 
                       (focus)="onFromChange()"
                       (keydown)="onKeydown($event, 'from')"
                       placeholder="Departure City" autocomplete="off">
                <ul class="suggestions" *ngIf="fromSuggestions.length > 0">
                  <li *ngFor="let city of fromSuggestions; let idx = index" 
                      [class.active]="idx === activeIndex"
                      (mousedown)="selectFrom(city)">{{ city }}</li>
                </ul>
              </div>
              
              <div class="swap-container" (click)="swapCities()">
                <div class="swap-icon">⇌</div>
              </div>

              <div class="search-field">
                <label>To</label>
                <input type="text" [(ngModel)]="to" 
                       (input)="onToChange()" 
                       (focus)="onToChange()"
                       (keydown)="onKeydown($event, 'to')"
                       placeholder="Destination City" autocomplete="off">
                <ul class="suggestions" *ngIf="toSuggestions.length > 0">
                  <li *ngFor="let city of toSuggestions; let idx = index" 
                      [class.active]="idx === activeIndex"
                      (mousedown)="selectTo(city)">{{ city }}</li>
                </ul>
              </div>
              <div class="search-field">
                <label>Date</label>
                <input type="date" [(ngModel)]="date" [min]="minDate">
              </div>
              <button class="btn-search" [disabled]="isSearching" (click)="onSearch()">
                {{ isSearching ? 'Searching...' : 'Search' }}
              </button>
            </div>
          </div>
          <div *ngIf="validationError" class="validation-error">
            {{ validationError }}
          </div>
          
          <div class="recent-searches" *ngIf="recentSearches.length > 0">
            <span class="recent-label">Recent:</span>
            <div class="recent-chips">
              <div class="chip" *ngFor="let search of recentSearches" (click)="loadSearch(search)">
                {{ search.from }} → {{ search.to }}
              </div>
            </div>
          </div>
        </div>
      </section>

      <section class="features">
        <div class="feature-card">
          <div class="icon">★</div>
          <h3>Premium Quality</h3>
          <p>Curated fleet of operators ensuring the highest standards of safety and comfort.</p>
        </div>
        <div class="feature-card">
          <div class="icon">⟳</div>
          <h3>Real-time Tracking</h3>
          <p>Always know where your bus is with our live GPS tracking system.</p>
        </div>
        <div class="feature-card">
          <div class="icon">💳</div>
          <h3>Secure Payments</h3>
          <p>Multiple payment options with industry-leading encryption and security.</p>
        </div>
      </section>
    </main>
  `,
  styles: [`
    :host {
      display: block;
      font-family: 'Inter', sans-serif;
      color: #000;
      background: #fff;
    }
    
    .booking-search-section { padding: 3rem 4rem 6rem; background: #fcfcfc; border-bottom: 1px solid #eee; display: flex; justify-content: center; }
    .search-card { width: 100%; max-width: 1100px; text-align: center; }
    .search-card h1 { font-size: 3rem; font-weight: 900; letter-spacing: -0.04em; margin-bottom: 2rem; }
    
    .search-form-container { position: relative; margin-top: -1rem; }
    .search-form { display: flex; background: white; border: 1px solid #eee; box-shadow: 0 15px 40px rgba(0,0,0,0.08); align-items: stretch; border-radius: 50px; overflow: hidden; }
    .search-field { flex: 1; padding: 1rem 1.5rem; border-right: 1px solid #eee; text-align: left; position: relative; min-width: 180px; }
    .search-field:last-of-type { border-right: none; }
    .search-field label { display: block; font-size: 0.7rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.1em; margin-bottom: 0.4rem; color: #888; }
    .search-field input { width: 100%; border: none; font-size: 1.1rem; font-weight: 700; outline: none; font-family: inherit; background: transparent; }
    
    .swap-container { display: flex; align-items: center; justify-content: center; background: #fff; cursor: pointer; padding: 0 0.5rem; border-right: 1px solid #eee; transition: background 0.2s; }
    .swap-container:hover { background: #f9f9f9; }
    .swap-icon { font-size: 1.5rem; font-weight: 900; transform: rotate(0deg); }

    .suggestions { position: absolute; top: calc(100% + 10px); left: 0; width: 100%; background: white; border: 1px solid #eee; list-style: none; padding: 0.5rem 0; margin: 0; z-index: 100; box-shadow: 0 10px 30px rgba(0,0,0,0.1); border-radius: 12px; }
    .suggestions li { padding: 0.75rem 1.5rem; cursor: pointer; font-weight: 700; font-size: 0.95rem; }
    .suggestions li.active { background: #f0f0f0; }
    .suggestions li:hover { background: #f5f5f5; }

    .validation-error { color: #ff5252; background: #fff1f1; padding: 0.5rem 1rem; margin-top: 1rem; font-weight: 700; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.05em; display: inline-block; border-radius: 20px; border: 1px solid #ffcfcf; }

    .btn-search { background: #000; color: #fff; border: none; padding: 0 3rem; font-weight: 900; font-size: 0.95rem; text-transform: uppercase; letter-spacing: 0.1em; cursor: pointer; transition: all 0.2s; border-radius: 0 50px 50px 0; }
    .btn-search:hover:not(:disabled) { background: #222; }
    .btn-search:disabled { background: #ddd; cursor: not-allowed; }

    .recent-searches { margin-top: 2rem; display: flex; align-items: center; justify-content: center; gap: 1rem; flex-wrap: wrap; }
    .recent-label { font-weight: 700; font-size: 0.8rem; text-transform: uppercase; color: #999; }
    .recent-chips { display: flex; gap: 0.75rem; flex-wrap: wrap; }
    .chip { background: #f5f5f5; border: 1px solid #eee; padding: 0.5rem 1.25rem; font-size: 0.85rem; font-weight: 700; cursor: pointer; transition: all 0.2s; border-radius: 30px; }
    .chip:hover { background: #eee; }

    .continue-banner {
      background: linear-gradient(135deg, #0f0f0f 0%, #2d2d2d 100%);
      color: #fff;
      padding: 1.5rem 4rem;
      border-bottom: 1px solid #333;
    }
    .banner-content { max-width: 1100px; margin: 0 auto; display: flex; justify-content: space-between; align-items: center; gap: 2rem; }
    .banner-left { display: flex; flex-direction: column; gap: 0.4rem; }
    .banner-badge { font-size: 0.7rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.1em; color: #ffc107; }
    .banner-route { font-size: 1.3rem; font-weight: 900; }
    .banner-meta { display: flex; gap: 0.75rem; font-size: 0.8rem; font-weight: 600; opacity: 0.7; align-items: center; }
    .timer-tag { color: #ffc107; font-weight: 800; font-family: 'JetBrains Mono', monospace; }
    .banner-actions { display: flex; gap: 0.75rem; flex-shrink: 0; }
    .btn-continue { background: #fff; color: #000; border: none; padding: 0.75rem 2rem; font-weight: 900; text-transform: uppercase; font-size: 0.8rem; letter-spacing: 0.05em; border-radius: 30px; cursor: pointer; transition: all 0.2s; }
    .btn-continue:hover { background: #f0f0f0; transform: translateY(-1px); }

    .features { display: grid; grid-template-columns: repeat(3, 1fr); border-bottom: 1px solid #eee; }
    .feature-card { padding: 4rem; border-right: 1px solid #eee; }
    .feature-card:last-child { border-right: none; }
    .icon { font-size: 2rem; margin-bottom: 1.5rem; }
    .feature-card h3 { font-weight: 800; font-size: 1.25rem; margin-bottom: 1rem; }
    .feature-card p { color: #666; font-size: 0.95rem; line-height: 1.6; }

    @media (max-width: 1024px) {
      .search-form { flex-direction: column; }
      .search-field { border-right: none; border-bottom: 2px solid #000; }
      .swap-container { border-right: none; border-bottom: 2px solid #000; padding: 1rem; }
      .swap-icon { transform: rotate(90deg); }
      .btn-search { padding: 2rem; }
      .features { grid-template-columns: 1fr; }
      .feature-card { border-right: none; border-bottom: 1px solid #000; }
    }
  `]
})
export class HomeComponent implements OnInit, OnDestroy {
  private searchService = inject(SearchService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  isSearching = false;

  from = '';
  to = '';
  date = new Date().toISOString().split('T')[0];
  minDate = new Date().toISOString().split('T')[0];
  validationError = '';

  fromSuggestions: string[] = [];
  toSuggestions: string[] = [];
  
  recentSearches: { from: string, to: string, date: string }[] = [];
  
  pendingBookings: any[] = [];
  private timerHandle: any;

  private fromQuery$ = new Subject<string>();
  private toQuery$ = new Subject<string>();
  private subs = new Subscription();

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (isPlatformBrowser(this.platformId)) {
      const target = event.target as HTMLElement;
      if (!target.closest('.search-field')) {
        this.fromSuggestions = [];
        this.toSuggestions = [];
        this.activeIndex = -1;
      }
    }
  }

  activeIndex = -1;

  onKeydown(event: KeyboardEvent, field: 'from' | 'to') {
    const suggestions = field === 'from' ? this.fromSuggestions : this.toSuggestions;
    if (suggestions.length === 0) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.activeIndex = (this.activeIndex + 1) % suggestions.length;
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.activeIndex = (this.activeIndex - 1 + suggestions.length) % suggestions.length;
    } else if (event.key === 'Enter') {
      if (this.activeIndex >= 0) {
        event.preventDefault();
        const city = suggestions[this.activeIndex];
        if (field === 'from') this.selectFrom(city);
        else this.selectTo(city);
      }
    } else if (event.key === 'Escape') {
      this.fromSuggestions = [];
      this.toSuggestions = [];
      this.activeIndex = -1;
    }
  }

  ngOnInit() {
    this.subs.add(
      this.fromQuery$.pipe(
        debounceTime(150),
        distinctUntilChanged(),
        switchMap(q => q.length > 1 ? this.searchService.getCities(q) : of([]))
      ).subscribe(res => {
        this.fromSuggestions = res.filter(city => city.toLowerCase() !== this.to.toLowerCase());
      })
    );

    this.subs.add(
      this.toQuery$.pipe(
        debounceTime(150),
        distinctUntilChanged(),
        switchMap(q => q.length > 1 ? this.searchService.getCities(q) : of([]))
      ).subscribe(res => {
        this.toSuggestions = res.filter(city => city.toLowerCase() !== this.from.toLowerCase());
      })
    );

    if (isPlatformBrowser(this.platformId)) {
      const stored = localStorage.getItem('recentSearches');
      if (stored) {
        try {
          this.recentSearches = JSON.parse(stored);
        } catch (e) {
          console.error('Failed to parse recent searches');
        }
      }

      // Check for pending booking
      this.loadPendingBooking();
    }
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
    if (this.timerHandle) clearInterval(this.timerHandle);
  }

  onFromChange() {
    this.validationError = '';
    this.fromQuery$.next(this.from);
  }

  onToChange() {
    this.validationError = '';
    this.toQuery$.next(this.to);
  }

  swapCities() {
    const temp = this.from;
    this.from = this.to;
    this.to = temp;
    this.checkSameCity();
  }

  selectFrom(city: string) {
    this.from = city;
    this.fromSuggestions = [];
    this.checkSameCity();
  }

  selectTo(city: string) {
    this.to = city;
    this.toSuggestions = [];
    this.checkSameCity();
  }

  private checkSameCity() {
    if (this.from && this.to && this.from.toLowerCase().trim() === this.to.toLowerCase().trim()) {
      this.validationError = "Source and Destination cannot be the same city.";
    } else {
      this.validationError = '';
    }
  }

  onSearch() {
    this.checkSameCity();
    if (this.validationError) return;

    if (!this.from || !this.to || !this.date) {
      alert('Please fill in all search fields.');
      return;
    }

    if (isPlatformBrowser(this.platformId)) {
      const searchData = { from: this.from, to: this.to, date: this.date };
      this.recentSearches = [searchData, ...this.recentSearches.filter(s => s.from !== this.from || s.to !== this.to)].slice(0, 3);
      localStorage.setItem('recentSearches', JSON.stringify(this.recentSearches));
    }
    
    this.isSearching = true;
    this.router.navigate(['/search'], {
      queryParams: { from: this.from, to: this.to, date: this.date }
    });
  }

  loadSearch(search: { from: string, to: string, date: string }) {
    this.from = search.from;
    this.to = search.to;
    if (search.date >= this.minDate) {
      this.date = search.date;
    } else {
      this.date = this.minDate;
    }
    this.onSearch();
  }

  private loadPendingBooking() {
    try {
      const raw = localStorage.getItem('pendingBookings');
      if (!raw) return;
      let bookings: any[] = JSON.parse(raw);
      
      // Filter expired bookings
      bookings = bookings.filter(b => {
        let expiry = b.expiresAt;
        if (!expiry.endsWith('Z')) expiry += 'Z';
        return new Date(expiry).getTime() > Date.now();
      });

      this.pendingBookings = bookings;
      localStorage.setItem('pendingBookings', JSON.stringify(this.pendingBookings));

      if (this.pendingBookings.length > 0) {
        this.startBannersTimer();
      }
    } catch {
      localStorage.removeItem('pendingBookings');
    }
  }

  private startBannersTimer() {
    if (this.timerHandle) clearInterval(this.timerHandle);
    this.timerHandle = setInterval(() => {
      let changed = false;
      this.pendingBookings = this.pendingBookings.filter(b => {
        const deadlineKey = `paymentDeadline_${b.journeyId}`;
        const storedDeadline = localStorage.getItem(deadlineKey);
        const deadlineMs = storedDeadline ? parseInt(storedDeadline, 10) : new Date(b.expiresAt + (b.expiresAt.endsWith('Z') ? '' : 'Z')).getTime();

        const diff = deadlineMs - Date.now();
        if (diff <= 0) {
          changed = true;
          localStorage.removeItem(deadlineKey);
          return false;
        }

        const m = Math.floor(diff / 60000);
        const s = Math.floor((diff % 60000) / 1000);
        b.timeLeft = `${m}:${s.toString().padStart(2, '0')}`;
        return true;
      });

      if (changed) {
        localStorage.setItem('pendingBookings', JSON.stringify(this.pendingBookings));
      }

      if (this.pendingBookings.length === 0) {
        clearInterval(this.timerHandle);
      }
    }, 1000);
  }

  continuePendingBooking(b: any) {
    this.router.navigate(['/booking/payment'], {
      queryParams: {
        journeyId: b.journeyId,
        seats: b.seats?.join(','),
        lockId: b.lockId,
        expiresAt: b.expiresAt,
        price: b.pricePerSeat
      },
      state: {
        passengers: b.passengers,
        boardingPointId: b.boardingPointId,
        droppingPointId: b.droppingPointId,
        boardingPointName: b.boardingPointName,
        droppingPointName: b.droppingPointName,
        journeyDetails: b.journeyDetails
      }
    });
  }
}
