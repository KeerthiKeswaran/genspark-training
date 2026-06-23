import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { BrowsedEventResponse } from '../../../models/event.model';
import { RegionModel } from '../../../models/region.model';

@Component({
  selector: 'app-events-browsing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './events-browsing.html',
  styleUrl: './events-browsing.css'
})
export class EventsBrowsingComponent implements OnInit, OnDestroy {
  public searchKeyword = signal('');
  public selectedRegionId = signal('REG01');
  public isLoggedIn = signal(false);
  public events = signal<BrowsedEventResponse[]>([]);
  public localEvents = signal<BrowsedEventResponse[]>([]);
  public otherRegionEvents = signal<BrowsedEventResponse[]>([]);
  public regions = signal<RegionModel[]>([]);
  public eventsLoading = signal(false);

  private subscriptions: Subscription = new Subscription();

  constructor(
    private store: AppStoreService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );
    this.subscriptions.add(
      this.store.select(state => state.events.items).subscribe(evs => {
        this.events.set(evs);
        this.updateSplitEvents(evs, this.selectedRegionId());
      })
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.items).subscribe(regs => this.regions.set(regs))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.currentRegionId).subscribe(regId => {
        const activeRegionId = regId || 'REG01';
        this.selectedRegionId.set(activeRegionId);
        this.updateSplitEvents(this.events(), activeRegionId);
      })
    );
    this.subscriptions.add(
      this.store.select(state => state.events.loading).subscribe(loading => this.eventsLoading.set(loading))
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private updateSplitEvents(evs: BrowsedEventResponse[], activeRegionId: string): void {
    if (!evs) return;
    const local = evs.filter(e => e.region_Id === activeRegionId);
    this.localEvents.set(local.slice(0, 3));

    const other = evs.filter(e => e.region_Id !== activeRegionId);
    this.otherRegionEvents.set(other.slice(0, 3));
  }

  public get currentLocationName(): string {
    const activeId = this.selectedRegionId();
    const found = this.regions().find(r => r.region_Id === activeId);
    return found ? found.name : 'Chennai';
  }

  public triggerBookingAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/bookings']);
  }

  public navigateToBookingFlow(eventId: number): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    this.router.navigate(['/booking'], { queryParams: { eventId } });
  }

  public onSearchSubmit(event?: Event): void {
    if (event) event.preventDefault();
    this.router.navigate(['/browse'], {
      queryParams: {
        keyword: this.searchKeyword(),
        regionId: this.selectedRegionId()
      }
    });
  }

  public clearFilters(): void {
    this.searchKeyword.set('');
    this.onSearchSubmit();
  }
}
