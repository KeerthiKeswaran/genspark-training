import { Component, OnInit, OnDestroy, Output, EventEmitter, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subscription, Subject, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { AppStoreService } from '../../../store/app-store.service';
import { AuthService } from '../../../services/auth.service';
import { RegionService } from '../../../services/region.service';
import { EventService } from '../../../services/event.service';
import { RegionModel } from '../../../models/region.model';
import { BrowsedEventResponse } from '../../../models/event.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class NavbarComponent implements OnInit, OnDestroy {
  @Output() openLocation = new EventEmitter<void>();

  public searchKeyword = signal('');
  public selectedRegionId = signal('REG01');
  public isProfileDropdownOpen = signal(false);
  public showRecommendations = signal(false);
  public recommendations = signal<BrowsedEventResponse[]>([]);

  private searchSubject = new Subject<string>();

  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    this.closeDropdowns();
  }

  public currentUser = signal<any>(null);
  public isLoggedIn = signal(false);
  public regions = signal<RegionModel[]>([]);

  private subscriptions: Subscription = new Subscription();

  constructor(
    private store: AppStoreService,
    private authService: AuthService,
    private regionService: RegionService,
    private eventService: EventService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => state.auth.user).subscribe(user => this.currentUser.set(user))
    );
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.items).subscribe(regs => this.regions.set(regs))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.currentRegionId).subscribe(regId => {
        this.selectedRegionId.set(regId || 'REG01');
      })
    );

    // Search recommendation subscription with RxJS switchMap, distinct, debounceTime querying backend
    this.subscriptions.add(
      this.searchSubject.pipe(
        debounceTime(200),
        distinctUntilChanged(),
        switchMap(keyword => {
          const kw = keyword.trim();
          if (!kw) {
            return of([]);
          }
          return this.eventService.searchEventsQuick(kw);
        })
      ).subscribe(matches => {
        this.recommendations.set(matches);
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public get currentLocationName(): string {
    const activeId = this.selectedRegionId();
    const found = this.regions().find(r => r.region_Id === activeId);
    return found ? found.name : 'Chennai';
  }

  public toggleProfileDropdown(event: Event): void {
    event.stopPropagation();
    this.isProfileDropdownOpen.update(v => !v);
  }

  public closeDropdowns(): void {
    this.isProfileDropdownOpen.set(false);
    this.showRecommendations.set(false);
  }

  public onSearchInput(val: string): void {
    this.searchKeyword.set(val);
    this.searchSubject.next(val);
  }

  public selectRecommendation(rec: any): void {
    this.searchKeyword.set(rec.title);
    this.showRecommendations.set(false);
    
    if (!this.isLoggedIn()) {
      const destination = `/booking?eventId=${rec.event_Id}`;
      this.router.navigate(['/login'], { queryParams: { returnUrl: destination } });
    } else {
      this.router.navigate(['/booking'], { queryParams: { eventId: rec.event_Id } });
    }
  }

  public onLocationPickerClick(event: Event): void {
    event.stopPropagation();
    this.openLocation.emit();
  }

  public onSearchSubmit(event?: Event): void {
    if (event) event.preventDefault();
    const isAlreadyOnBrowse = this.router.url.startsWith('/browse');
    this.router.navigate(['/browse'], {
      queryParams: {
        keyword: this.searchKeyword() || null,
        regionId: this.selectedRegionId() || null
      }
    }).then(() => {
      if (isAlreadyOnBrowse) {
        window.location.reload();
      }
    });
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
    this.isProfileDropdownOpen.set(false);
  }

  public triggerCreateEventAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/myevents/create']);
  }

  public triggerManageMyEventsAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/myevents']);
    this.isProfileDropdownOpen.set(false);
  }

  public triggerAccountSettingsAction(): void {
    this.router.navigate(['/settings']);
    this.isProfileDropdownOpen.set(false);
  }

  public triggerGetHelpAction(): void {
    this.router.navigate(['/help']);
    this.isProfileDropdownOpen.set(false);
  }
}
