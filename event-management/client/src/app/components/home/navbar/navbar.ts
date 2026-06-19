import { Component, OnInit, OnDestroy, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AppStoreService } from '../../../store/app-store.service';
import { AuthService } from '../../../services/auth.service';
import { RegionService } from '../../../services/region.service';
import { RegionModel } from '../../../models/region.model';

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

  public currentUser = signal<any>(null);
  public isLoggedIn = signal(false);
  public regions = signal<RegionModel[]>([]);

  private subscriptions: Subscription = new Subscription();

  constructor(
    private store: AppStoreService,
    private authService: AuthService,
    private regionService: RegionService,
    private router: Router
  ) {}

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
  }

  public onLocationPickerClick(event: Event): void {
    event.stopPropagation();
    this.openLocation.emit();
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

  public onLogout(): void {
    this.authService.logout();
    this.isProfileDropdownOpen.set(false);
  }

  public triggerBookingAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    alert('Navigating to bookings overview...');
  }

  public triggerCreateEventAction(): void {
    if (!this.isLoggedIn()) {
      this.router.navigate(['/login']);
      return;
    }
    alert('Navigating to event creation page...');
  }

  public triggerManageMyEventsAction(): void {
    alert('Navigating to manage my events...');
  }

  public triggerAccountSettingsAction(): void {
    alert('Navigating to account settings...');
  }

  public triggerGetHelpAction(): void {
    const aboutSection = document.getElementById('about-section');
    if (aboutSection) {
      aboutSection.scrollIntoView({ behavior: 'smooth' });
    } else {
      this.router.navigate(['/'], { fragment: 'about-section' });
    }
    this.isProfileDropdownOpen.set(false);
  }
}
