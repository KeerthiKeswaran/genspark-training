import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription, firstValueFrom } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { RegionService } from '../../../services/region.service';
import { AuthService } from '../../../services/auth.service';
import { PixabayService } from '../../../services/pixabay.service';
import { RegionModel } from '../../../models/region.model';

@Component({
  selector: 'app-popular-regions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './popular-regions.html',
  styleUrl: './popular-regions.css'
})
export class PopularRegionsComponent implements OnInit, OnDestroy {
  public isLoggedIn = signal(false);
  public homeRegions = signal<RegionModel[]>([]);
  public regionImages = signal<Map<string, string>>(new Map());
  private subscriptions: Subscription = new Subscription();

  constructor(
    private store: AppStoreService,
    private regionService: RegionService,
    private authService: AuthService,
    private pixabayService: PixabayService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.store.select(state => !!state.auth.token).subscribe(logged => this.isLoggedIn.set(logged))
    );
    this.subscriptions.add(
      this.store.select(state => state.regions.items).subscribe(regs => {
        const filtered = (regs || []).filter(r => 
          r.region_Id === 'REG01' || 
          r.region_Id === 'REG02' || 
          r.region_Id === 'REG03' || 
          r.region_Id === 'REG04'
        );
        this.homeRegions.set(filtered);
        this.loadImages(filtered);
      })
    );
  }

  private async loadImages(regions: RegionModel[]): Promise<void> {
    const images = new Map<string, string>();
    for (const region of regions) {
      const url = await firstValueFrom(this.pixabayService.searchRegionImage(region.name));
      images.set(region.region_Id, url);
    }
    this.regionImages.set(images);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public getRegionImage(regionId: string): string {
    return this.regionImages().get(regionId) || 'https://images.unsplash.com/photo-1501281668745-f7f57925c3b4?w=600&auto=format&fit=crop&q=80';
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
