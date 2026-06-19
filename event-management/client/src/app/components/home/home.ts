import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../store/app-store.service';
import { RegionService } from '../../services/region.service';
import { EventService } from '../../services/event.service';
import { NavbarComponent } from './navbar/navbar';
import { HeroCarouselComponent } from './hero-carousel/hero-carousel';
import { EventsBrowsingComponent } from './events-browsing/events-browsing';
import { PopularRegionsComponent } from './popular-regions/popular-regions';
import { AboutFaqComponent } from './about-faq/about-faq';
import { FooterComponent } from './footer/footer';
import { LocationModalComponent } from './location-modal/location-modal';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    NavbarComponent,
    HeroCarouselComponent,
    EventsBrowsingComponent,
    PopularRegionsComponent,
    AboutFaqComponent,
    FooterComponent,
    LocationModalComponent
  ],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class HomeComponent implements OnInit, OnDestroy {
  public isLocationModalOpen = signal(false);
  private subscriptions = new Subscription();

  constructor(
    private store: AppStoreService,
    private regionService: RegionService,
    private eventService: EventService
  ) {}

  ngOnInit(): void {
    // Initial seeds
    this.regionService.loadRegions().subscribe();
    this.eventService.getTrendingEvents().subscribe();

    // Listen to changes in the active region to load the corresponding events list
    this.subscriptions.add(
      this.store.select(state => state.regions.currentRegionId).subscribe(regId => {
        const activeRegionId = regId || 'REG01';
        this.eventService.browseEvents({
          regionId: activeRegionId,
          page: 1,
          size: 24
        }).subscribe();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public openLocationModal(): void {
    this.isLocationModalOpen.set(true);
  }

  public closeLocationModal(): void {
    this.isLocationModalOpen.set(false);
  }
}
