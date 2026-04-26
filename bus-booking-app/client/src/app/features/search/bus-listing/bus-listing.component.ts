import { Component, OnInit, inject, ChangeDetectorRef, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { SearchService } from '../../../core/services/search.service';
import { BusSearchResult } from '../../../core/models/search.models';
import { finalize, Subject, debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-bus-listing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="listing-layout">
      <!-- Sidebar Filters -->
      <aside class="filters-sidebar">
        <div class="filter-group">
          <h3>Sort By</h3>
          <select [(ngModel)]="sortBy" (change)="applyFilters()">
            <option value="price-low">Price: Low to High</option>
            <option value="price-high">Price: High to Low</option>
            <option value="time-early">Departure: Early first</option>
            <option value="time-late">Departure: Late first</option>
          </select>
        </div>

        <div class="filter-group">
          <h3>Bus Type</h3>
          <div class="checkbox-list">
            <label *ngFor="let type of busTypes">
              <input type="checkbox" [checked]="selectedTypes.has(type)" (change)="toggleType(type)">
              {{ type }}
            </label>
          </div>
        </div>

        <div class="filter-group">
          <h3>Operators</h3>
          <div class="checkbox-list">
            <label *ngFor="let op of operators">
              <input type="checkbox" [checked]="selectedOperators.has(op)" (change)="toggleOperator(op)">
              {{ op }}
            </label>
          </div>
        </div>

        <div class="filter-group">
          <h3>Price Range</h3>
          <div class="price-inputs">
            <input type="number" [(ngModel)]="minPrice" (input)="applyFilters()" placeholder="Min">
            <span>-</span>
            <input type="number" [(ngModel)]="maxPrice" (input)="applyFilters()" placeholder="Max">
          </div>
        </div>

        <button class="btn-reset" (click)="resetFilters()">Reset All</button>
      </aside>

      <!-- Main Results Area -->
      <main class="results-main">
        <div class="header-summary" *ngIf="from">
          <div class="route-info" *ngIf="!showModifyForm">
            <h2>{{ from }} to {{ to }}</h2>
            <p>{{ date | date:'fullDate' }} • {{ filteredResults.length }} Buses</p>
          </div>

          <!-- Inline Modify Form -->
          <div class="modify-form-container" *ngIf="showModifyForm">
            <div class="modify-inputs">
              <div class="input-wrap">
                <input type="text" [(ngModel)]="from" (input)="fromQuery$.next(from)" placeholder="From">
                <ul class="suggestions" *ngIf="fromSuggestions.length > 0">
                  <li *ngFor="let s of fromSuggestions" (click)="selectFrom(s)">{{ s }}</li>
                </ul>
              </div>
              <div class="input-wrap">
                <input type="text" [(ngModel)]="to" (input)="toQuery$.next(to)" placeholder="To">
                <ul class="suggestions" *ngIf="toSuggestions.length > 0">
                  <li *ngFor="let s of toSuggestions" (click)="selectTo(s)">{{ s }}</li>
                </ul>
              </div>
              <div class="input-wrap">
                <input type="date" [(ngModel)]="date">
              </div>
              <button class="btn-update" (click)="updateSearch()">Search</button>
              <button class="btn-cancel" (click)="showModifyForm = false">Cancel</button>
            </div>
          </div>

          <button *ngIf="!showModifyForm" type="button" class="btn-modify" (click)="showModifyForm = true">Modify Search</button>
        </div>

        <div class="results-list">
          <div *ngIf="loading" class="loading-state">
            <div class="spinner"></div>
            <p>Finding the best buses for you...</p>
          </div>
          
          <div *ngIf="!loading && filteredResults.length === 0" class="empty-state">
            <h3>No matching buses</h3>
            <p>Try adjusting your filters or search for a different date.</p>
            <button (click)="resetFilters()">Clear Filters</button>
          </div>

          <div class="bus-card" *ngFor="let bus of filteredResults">
            <div class="operator-info">
              <div class="operator-header">
                <span class="operator-name">{{ bus.operatorName }}</span>
                <div class="info-trigger">
                  <span class="icon-i">i</span>
                  <div class="info-popup">
                    <strong>{{ bus.operatorName }}</strong>
                    <p>{{ bus.operatorAddress }}</p>
                  </div>
                </div>
              </div>
              <span class="bus-type">{{ bus.busType }}</span>
            </div>
            
            <div class="time-route">
              <div class="departure">
                <span class="time">{{ bus.departureTime | date:'HH:mm' }}</span>
                <span class="location">{{ bus.source }}</span>
              </div>
              <div class="duration-line">
                <span class="dot"></span>
                <span class="line"></span>
                <span class="dot"></span>
              </div>
              <div class="arrival">
                <span class="time">{{ bus.arrivalTime | date:'HH:mm' }}</span>
                <span class="location">{{ bus.destination }}</span>
              </div>
            </div>

            <div class="price-action">
              <div class="price-info">
                <span class="amount">₹{{ bus.price }}</span>
              </div>
              <div class="seats-info">
                {{ bus.availableSeats }} Seats left
              </div>
              <button class="btn-book" (click)="selectSeat(bus)">Select Seat</button>
            </div>
          </div>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .listing-layout {
      display: grid;
      grid-template-columns: 280px 1fr;
      gap: 3rem;
      max-width: 1400px;
      margin: 0 auto;
      padding: 4rem 2rem;
      font-family: 'Inter', sans-serif;
    }

    .filters-sidebar {
      background: #fff;
      border: 1px solid #eee;
      padding: 2rem;
      height: fit-content;
      max-height: calc(100vh - 120px);
      overflow-y: auto;
      position: sticky;
      top: 100px;
      box-shadow: 0 10px 30px rgba(0,0,0,0.05);
      border-radius: 20px;
    }
 
    .filters-sidebar::-webkit-scrollbar { width: 6px; }
    .filters-sidebar::-webkit-scrollbar-track { background: #fff; }
    .filters-sidebar::-webkit-scrollbar-thumb { background: #eee; border-radius: 10px; }
 
    .filter-group { margin-bottom: 2rem; }
    .filter-group h3 { font-weight: 800; font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.1em; margin-bottom: 1.2rem; border-bottom: 1px solid #eee; padding-bottom: 0.5rem; color: #888; }
    
    select { width: 100%; padding: 0.75rem; border: 1px solid #eee; border-radius: 10px; font-weight: 700; font-family: inherit; outline: none; cursor: pointer; background: #f9f9f9; }
    
    .checkbox-list { display: flex; flex-direction: column; gap: 0.75rem; }
    .checkbox-list label { display: flex; align-items: center; gap: 0.75rem; font-size: 0.85rem; font-weight: 700; cursor: pointer; color: #555; }
    .checkbox-list input[type="checkbox"] { width: 18px; height: 18px; accent-color: #000; cursor: pointer; }
 
    .price-inputs { display: flex; align-items: center; gap: 0.5rem; }
    .price-inputs input { width: 100%; padding: 0.6rem; border: 1px solid #eee; border-radius: 8px; font-weight: 700; font-size: 0.85rem; outline: none; background: #f9f9f9; }
 
    .btn-reset { width: 100%; padding: 0.8rem; background: #f5f5f5; border: none; font-weight: 800; text-transform: uppercase; cursor: pointer; border-radius: 30px; transition: background 0.2s; }
    .btn-reset:hover { background: #eee; }

    /* Header & Modify Form */
    .header-summary {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 3rem;
      border-bottom: 1px solid #eee;
      padding-bottom: 2rem;
      min-height: 100px;
    }
    .header-summary h2 { font-weight: 900; font-size: 2.75rem; letter-spacing: -0.05em; margin: 0; text-transform: capitalize; }
    
    .modify-form-container { flex: 1; margin-right: 2rem; }
    .modify-inputs { display: flex; gap: 1rem; align-items: center; }
    .input-wrap { position: relative; flex: 1; }
    .input-wrap input { width: 100%; padding: 0.75rem 1rem; border: 1px solid #eee; border-radius: 12px; font-weight: 700; outline: none; background: #f9f9f9; }
    .input-wrap input:focus { background: #fff; border-color: #000; }
    
    .suggestions {
      position: absolute;
      top: calc(100% + 5px);
      left: 0;
      right: 0;
      background: #fff;
      color: #000;
      z-index: 1000;
      list-style: none;
      padding: 0.5rem 0;
      margin: 0;
      border: 1px solid #eee;
      border-radius: 12px;
      box-shadow: 0 10px 25px rgba(0,0,0,0.1);
    }
    .suggestions li { padding: 0.75rem 1rem; font-weight: 700; cursor: pointer; }
    .suggestions li:hover { background: #f5f5f5; }
 
    .btn-update { background: #000; color: #fff; border: none; padding: 0.75rem 2rem; font-weight: 900; text-transform: uppercase; cursor: pointer; border-radius: 30px; transition: background 0.2s; }
    .btn-update:hover { background: #222; }
    .btn-cancel { background: #f5f5f5; border: none; padding: 0.75rem 1.5rem; font-weight: 900; text-transform: uppercase; cursor: pointer; border-radius: 30px; }
 
    .btn-modify { background: transparent; border: 1px solid #eee; padding: 0.8rem 1.5rem; font-weight: 800; text-transform: uppercase; font-size: 0.8rem; cursor: pointer; border-radius: 30px; transition: all 0.2s; }
    .btn-modify:hover { border-color: #000; background: #fafafa; }

    /* Bus Cards */
    .bus-card {
      display: grid;
      grid-template-columns: 1.2fr 2fr 1fr;
      border: 1px solid #eee;
      margin-bottom: 2rem;
      background: white;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      border-radius: 20px;
      overflow: hidden;
      box-shadow: 0 4px 20px rgba(0,0,0,0.03);
    }
    .bus-card:hover { transform: translateY(-5px); box-shadow: 0 15px 40px rgba(0,0,0,0.08); }

    .operator-info { padding: 2rem; border-right: 1px solid #f0f0f0; display: flex; flex-direction: column; justify-content: center; }
    .operator-header { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.25rem; }
    .operator-name { font-weight: 900; font-size: 1.25rem; }
    
    .info-trigger {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 18px;
      height: 18px;
      border: 1.5px solid #000;
      border-radius: 50%;
      font-size: 0.65rem;
      font-weight: 900;
      cursor: help;
      position: relative;
    }
    
    .info-popup {
      position: absolute;
      bottom: 100%;
      left: 50%;
      transform: translateX(-50%) translateY(-10px);
      background: #000;
      color: #fff;
      padding: 0.75rem 1rem;
      width: 200px;
      z-index: 100;
      visibility: hidden;
      opacity: 0;
      transition: all 0.2s;
      text-align: left;
    }
    .info-trigger:hover .info-popup {
      visibility: visible;
      opacity: 1;
      transform: translateX(-50%) translateY(-5px);
    }
    
    .bus-type { font-size: 0.7rem; color: #666; text-transform: uppercase; letter-spacing: 0.1em; font-weight: 700; }

    .time-route { padding: 2rem; display: flex; justify-content: space-around; align-items: center; text-align: center; }
    .time { display: block; font-weight: 900; font-size: 2rem; margin-bottom: 0.25rem; }
    
    .price-action { padding: 2rem; border-left: 1px solid #eee; text-align: right; display: flex; flex-direction: column; justify-content: center; background: #fcfcfc; }
    .price-info .amount { font-weight: 900; font-size: 2.2rem; color: #000; letter-spacing: -0.05em; }
    
    .btn-book { background: #000; color: #fff; border: none; padding: 1rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.15em; cursor: pointer; border-radius: 12px; transition: background 0.2s; }
    .btn-book:hover { background: #222; }
 
    .loading-state, .empty-state { text-align: center; padding: 6rem; border: 1px solid #eee; background: #fff; border-radius: 24px; box-shadow: 0 10px 30px rgba(0,0,0,0.05); }
    .spinner { width: 40px; height: 40px; border: 3px solid #f3f3f3; border-top: 3px solid #000; border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto 1.5rem; }
    @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }

    @media (max-width: 1024px) {
      .listing-layout { grid-template-columns: 1fr; }
      .modify-inputs { flex-direction: column; align-items: stretch; }
    }
  `]
})
export class BusListingComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private searchService = inject(SearchService);
  private cdr = inject(ChangeDetectorRef);

  from = '';
  to = '';
  date = '';
  loading = false;
  showModifyForm = false;

  allResults: BusSearchResult[] = [];
  filteredResults: BusSearchResult[] = [];

  // Autocomplete in modify form
  fromQuery$ = new Subject<string>();
  toQuery$ = new Subject<string>();
  fromSuggestions: string[] = [];
  toSuggestions: string[] = [];

  // Filter States
  sortBy = 'price-low';
  selectedTypes = new Set<string>();
  selectedOperators = new Set<string>();
  minPrice: number | null = null;
  maxPrice: number | null = null;

  // Filter Options
  busTypes: string[] = [];
  operators: string[] = [];

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.from = params['from'] || '';
      this.to = params['to'] || '';
      this.date = params['date'] || '';

      if (this.from && this.to && this.date) {
        this.performSearch();
      }
    });

    // High-performance autocomplete setup (100ms debounce)
    this.fromQuery$.pipe(
      debounceTime(100),
      distinctUntilChanged(),
      switchMap(q => q.length > 1 ? this.searchService.getCities(q) : of([]))
    ).subscribe(res => {
      this.fromSuggestions = res.filter(c => c.toLowerCase() !== this.to.toLowerCase());
      this.cdr.detectChanges();
    });

    this.toQuery$.pipe(
      debounceTime(100),
      distinctUntilChanged(),
      switchMap(q => q.length > 1 ? this.searchService.getCities(q) : of([]))
    ).subscribe(res => {
      this.toSuggestions = res.filter(c => c.toLowerCase() !== this.from.toLowerCase());
      this.cdr.detectChanges();
    });
  }

  selectFrom(city: string) {
    this.from = city;
    this.fromSuggestions = [];
  }

  selectTo(city: string) {
    this.to = city;
    this.toSuggestions = [];
  }

  updateSearch() {
    this.showModifyForm = false;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { from: this.from, to: this.to, date: this.date },
      queryParamsHandling: 'merge'
    });
  }

  performSearch() {
    this.loading = true;
    this.searchService.searchBuses(this.from, this.to, this.date)
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.detectChanges();
      }))
      .subscribe(data => {
        this.allResults = data || [];
        this.extractFilterOptions();
        this.applyFilters();
      });
  }

  extractFilterOptions() {
    this.busTypes = [...new Set(this.allResults.map(b => b.busType))].sort();
    this.operators = [...new Set(this.allResults.map(b => b.operatorName))].sort();
  }

  toggleType(type: string) {
    this.selectedTypes.has(type) ? this.selectedTypes.delete(type) : this.selectedTypes.add(type);
    this.applyFilters();
  }

  toggleOperator(op: string) {
    this.selectedOperators.has(op) ? this.selectedOperators.delete(op) : this.selectedOperators.add(op);
    this.applyFilters();
  }

  resetFilters() {
    this.selectedTypes.clear();
    this.selectedOperators.clear();
    this.minPrice = null;
    this.maxPrice = null;
    this.sortBy = 'price-low';
    this.applyFilters();
  }

  applyFilters() {
    let temp = [...this.allResults];

    if (this.selectedTypes.size > 0) {
      temp = temp.filter(b => this.selectedTypes.has(b.busType));
    }

    if (this.selectedOperators.size > 0) {
      temp = temp.filter(b => this.selectedOperators.has(b.operatorName));
    }

    if (this.minPrice !== null) temp = temp.filter(b => b.price >= (this.minPrice ?? 0));
    if (this.maxPrice !== null) temp = temp.filter(b => b.price <= (this.maxPrice ?? 99999));

    temp.sort((a, b) => {
      switch (this.sortBy) {
        case 'price-low': return a.price - b.price;
        case 'price-high': return b.price - a.price;
        case 'time-early': return new Date(a.departureTime).getTime() - new Date(b.departureTime).getTime();
        case 'time-late': return new Date(b.departureTime).getTime() - new Date(a.departureTime).getTime();
        default: return 0;
      }
    });

    this.filteredResults = temp;
    this.cdr.detectChanges();
  }

  selectSeat(bus: BusSearchResult) {
    this.router.navigate(['/booking/seats', bus.scheduleId], {
      queryParams: { price: bus.price }
    });
  }
}
