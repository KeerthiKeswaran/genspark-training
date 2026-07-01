import { Component, OnInit, OnDestroy, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription, firstValueFrom } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { RegionService } from '../../../services/region.service';
import { AuthService } from '../../../services/auth.service';
import { PixabayService } from '../../../services/pixabay.service';
import { RegionPopularResponse } from '../../../models/event.model';

@Component({
  selector: 'app-popular-regions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './popular-regions.html',
  styleUrl: './popular-regions.css'
})
export class PopularRegionsComponent implements OnInit, OnDestroy {
  public isLoggedIn = signal(false);
  public homeRegions = signal<RegionPopularResponse[]>([]);
  public regionImages = signal<Map<string, string | null>>(new Map());
  public imageErrors = signal<Map<string, boolean>>(new Map());
  private subscriptions: Subscription = new Subscription();
  private currentLoadingRegionsString = '';
  private currentLimit = 4;

  constructor(
    private store: AppStoreService,
    private regionService: RegionService,
    private authService: AuthService,
    private pixabayService: PixabayService,
    private router: Router
  ) {}

  @HostListener('window:resize')
  public onResize(): void {
    this.updateRegionsLimit();
  }

  private calculateRegionsLimit(): number {
    if (typeof window === 'undefined') return 4;
    return window.innerWidth > 1024 ? 4 : 2; // minimum is 2
  }

  private updateRegionsLimit(): void {
    const newLimit = this.calculateRegionsLimit();
    if (newLimit !== this.currentLimit) {
      this.currentLimit = newLimit;
      this.regionService.getPopularRegions(newLimit).subscribe();
    }
  }

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );

    // Initial load based on current window width
    this.currentLimit = this.calculateRegionsLimit();
    this.regionService.getPopularRegions(this.currentLimit).subscribe();

    // Read popular regions from the dedicated popularItems slice of the store
    this.subscriptions.add(
      this.store.select(state => state.regions.popularItems).subscribe(popular => {
        const regionsKey = (popular || []).map(r => r.region_Id).join(',');
        if (regionsKey && regionsKey === this.currentLoadingRegionsString) {
          return;
        }
        this.currentLoadingRegionsString = regionsKey;
        this.homeRegions.set(popular || []);
        this.loadImages(popular || []);
      })
    );
  }

  private async loadImages(regions: RegionPopularResponse[]): Promise<void> {
    const currentImages = this.regionImages();
    const images = new Map<string, string | null>(currentImages);
    
    // Load images sequentially with a delay to prevent rate-limiting and stagger rendering
    for (const region of regions) {
      // Only fetch if we don't already have it loaded in memory
      if (images.has(region.region_Id)) {
        continue;
      }

      try {
        const imageObj = await firstValueFrom(this.pixabayService.searchRegionImage(region.region_Id, region.region_Name));
        const url = imageObj ? (imageObj.data || imageObj.webformatURL || imageObj.url) : null;
        images.set(region.region_Id, url);
        // Update the signal incrementally so each image displays as soon as it resolves
        this.regionImages.set(new Map(images));
        
        // Add a 450ms delay between image displays to stagger browser requests and avoid CDN rate-limiting
        await new Promise(resolve => setTimeout(resolve, 450));
      } catch (err) {
        console.error(`Error loading image for region ${region.region_Name}:`, err);
      }
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public getRegionImage(regionId: string): string | null {
    return this.regionImages().get(regionId) || null;
  }

  public onImageError(regionId: string): void {
    const errors = new Map(this.imageErrors());
    errors.set(regionId, true);
    this.imageErrors.set(errors);
  }

  public hasImageError(regionId: string): boolean {
    return !!this.imageErrors().get(regionId);
  }

  public onPopularRegionSelect(regionId: string): void {
    this.regionService.setLocalRegion(regionId);
    if (this.isLoggedIn()) {
      this.authService.selectRegion(regionId).subscribe();
    }
    setTimeout(() => {
      this.router.navigate(['/browse'], {
        queryParams: {
          regionId: regionId
        }
      });
    }, 100);
  }
}
