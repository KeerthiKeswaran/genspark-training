import { Component, OnInit, OnDestroy, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../store/app-store.service';
import { AuthService } from '../../services/auth.service';
import { EventService } from '../../services/event.service';
import { RegionService } from '../../services/region.service';
import { LocationGeoService } from '../../services/location-geo.service';
import { BrowsedEventResponse } from '../../models/event.model';
import { RegionModel } from '../../models/region.model';
import { LocationModalComponent } from '../home/location-modal/location-modal';
import { FooterComponent } from '../home/footer/footer';

@Component({
  selector: 'app-browse-events',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LocationModalComponent, FooterComponent],
  templateUrl: './browse-events.html',
  styleUrl: './browse-events.css'
})
export class BrowseEventsComponent implements OnInit, OnDestroy {
  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    this.closeDropdowns();
  }
  // Filters binded to inputs
  public filterKeyword = '';
  public filterRegionIds: string[] = [];
  public filterCategory = '';
  public filterFormat = '';
  public filterMaxPrice: number | null = null;
  public filterSortBy = '';
  public currentPage = 1;
  public pageSize = 6;
  public totalPages = 1;

  // Local signals
  public isProfileDropdownOpen = signal(false);
  public showNationwideRegions = signal(false);

  // Modal signals
  public isLocationModalOpen = signal(false);

  // Store select observables/signals
  public currentUser = signal<any>(null);
  public isLoggedIn = signal(false);
  public events = signal<BrowsedEventResponse[]>([]);
  public regions = signal<RegionModel[]>([]);
  public eventsLoading = signal(false);
  public activeUserRegionId = signal('REG01');
  public totalEvents = signal(0);

  private subscriptions: Subscription = new Subscription();

  constructor(
    private store: AppStoreService,
    private authService: AuthService,
    private eventService: EventService,
    private regionService: RegionService,
    private locationGeoService: LocationGeoService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Subscribe to store state
    this.subscriptions.add(
      this.store.select(state => state.auth.user).subscribe(user => this.currentUser.set(user))
    );
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );
    this.subscriptions.add(
      this.store.select(state => state.events.items).subscribe(evs => this.events.set(evs))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.items).subscribe(regs => this.regions.set(regs))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.currentRegionId).subscribe(regId => {
        this.activeUserRegionId.set(regId || 'REG01');
        this.fetchEvents();
      })
    );
    this.subscriptions.add(
      this.store.select(state => state.events.loading).subscribe(loading => this.eventsLoading.set(loading))
    );

    // Initial loads
    this.regionService.loadRegions().subscribe();

    // Listen to query parameters
    this.subscriptions.add(
      this.route.queryParams.subscribe(params => {
        this.filterKeyword = params['keyword'] || '';
        const regs = params['regions'] || '';
        this.filterRegionIds = regs ? regs.split(',') : [];
        this.filterCategory = params['category'] || '';
        this.filterFormat = params['format'] || '';
        const maxP = params['maxPrice'];
        this.filterMaxPrice = maxP ? +maxP : null;
        this.filterSortBy = params['sortBy'] || '';
        this.currentPage = +(params['page'] || 1);
        this.fetchEvents();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  // Active Location Name getter helper for Navbar
  public get currentLocationName(): string {
    const activeId = this.activeUserRegionId();
    const found = this.regions().find(r => r.region_Id === activeId);
    return found ? found.name : 'Chennai';
  }

  public fetchEvents(): void {
    this.eventService.browseEvents({
      keyword: this.filterKeyword,
      regionId: this.activeUserRegionId(), // For active sorting
      regionIds: this.filterRegionIds, // For checkbox filtering
      category: this.filterCategory,
      format: this.filterFormat,
      maxPrice: this.filterMaxPrice !== null ? this.filterMaxPrice : undefined,
      sortBy: this.filterSortBy,
      page: this.currentPage,
      size: this.pageSize
    }).subscribe(result => {
      this.totalPages = result.totalPages || 1;
      this.totalEvents.set(result.totalCount || 0);
    });
  }

  // Action Apply Filters
  public applyFilters(): void {
    this.currentPage = 1;
    this.updateQueryParams();
  }

  // Clear Filters
  public clearFilters(): void {
    this.filterKeyword = '';
    this.filterRegionIds = [];
    this.filterCategory = '';
    this.filterFormat = '';
    this.filterMaxPrice = null;
    this.filterSortBy = '';
    this.currentPage = 1;
    this.updateQueryParams();
  }

  // Checkbox region filters logic
  public toggleRegionFilter(regionId: string): void {
    const index = this.filterRegionIds.indexOf(regionId);
    if (index > -1) {
      this.filterRegionIds.splice(index, 1);
    } else {
      this.filterRegionIds.push(regionId);
    }
  }

  public isRegionFilterChecked(regionId: string): boolean {
    return this.filterRegionIds.includes(regionId);
  }

  // Chip removers for top filter summary
  public removeKeywordFilter(): void {
    this.filterKeyword = '';
    this.applyFilters();
  }

  public removeCategoryFilter(): void {
    this.filterCategory = '';
    this.applyFilters();
  }

  public removeFormatFilter(): void {
    this.filterFormat = '';
    this.applyFilters();
  }

  public removeMaxPriceFilter(): void {
    this.filterMaxPrice = null;
    this.applyFilters();
  }

  public removeRegionFilter(regionId: string): void {
    const index = this.filterRegionIds.indexOf(regionId);
    if (index > -1) {
      this.filterRegionIds.splice(index, 1);
      this.applyFilters();
    }
  }

  public getSelectedRegionName(regionId: string): string {
    const found = this.regions().find(r => r.region_Id === regionId);
    return found ? found.name : regionId;
  }

  private updateQueryParams(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        keyword: this.filterKeyword || null,
        regions: this.filterRegionIds.length > 0 ? this.filterRegionIds.join(',') : null,
        category: this.filterCategory || null,
        format: this.filterFormat || null,
        maxPrice: this.filterMaxPrice || null,
        sortBy: this.filterSortBy || null,
        page: this.currentPage
      },
      queryParamsHandling: 'merge'
    });
  }

  // Pagination triggers
  public nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updateQueryParams();
    }
  }

  public prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updateQueryParams();
    }
  }

  // Navigation handlers
  public toggleProfileDropdown(event: Event): void {
    event.stopPropagation();
    this.isProfileDropdownOpen.update(v => !v);
  }

  public closeDropdowns(): void {
    this.isProfileDropdownOpen.set(false);
  }

  // Modal open/close actions
  public openLocationModal(event?: Event): void {
    if (event) event.stopPropagation();
    this.isLocationModalOpen.set(true);
  }

  public closeLocationModal(): void {
    this.isLocationModalOpen.set(false);
  }

  public selectRegion(regionId: string, event?: Event): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    this.regionService.setLocalRegion(regionId);
    if (this.isLoggedIn()) {
      this.authService.selectRegion(regionId).subscribe();
    }
  }

  public onLogout(): void {
    this.authService.logout();
    this.isProfileDropdownOpen.set(false);
  }

  public triggerBookingAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/bookings']);
  }

  public triggerCreateEventAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/create-event']);
  }

  public navigateToBookingFlow(eventId: number): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/booking'], { queryParams: { eventId } });
  }

  public triggerManageMyEventsAction(): void {
    alert('Navigating to manage my events...');
  }

  public triggerAccountSettingsAction(): void {
    alert('Navigating to account settings...');
  }

  public triggerGetHelpAction(): void {
    this.router.navigate(['/help']);
    this.isProfileDropdownOpen.set(false);
  }
}
