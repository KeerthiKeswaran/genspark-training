import { Component, inject, signal, OnInit, OnDestroy, HostListener } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';
import { ProductService, Product } from '../../services/product.service';
import { AsyncPipe } from '@angular/common';
import { InrCurrencyPipe } from '../../pipes/inr-currency.pipe';
import { Subject, Subscription, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-header',
  imports: [AsyncPipe, RouterLink, InrCurrencyPipe],
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class Header implements OnInit, OnDestroy {
  protected readonly authService = inject(AuthService);
  protected readonly cartService = inject(CartService);
  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);

  // Expose current user observable to template
  protected readonly currentUser$ = this.authService.currentUser$;

  // Dropdown state
  protected readonly isDropdownOpen = signal<boolean>(false);

  // Search autocomplete reactive states
  protected readonly searchQuery = signal<string>('');
  protected readonly recommendations = signal<Product[]>([]);
  protected readonly showRecommendations = signal<boolean>(false);

  private readonly searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  ngOnInit(): void {
    // Pipe input stream to trigger DummyJSON search service with debouncing
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (!query || query.trim().length < 2) {
          return of([]);
        }
        return this.productService.searchProducts(query);
      })
    ).subscribe({
      next: (products) => {
        // Expose top 5 matches
        this.recommendations.set(products.slice(0, 5));
      },
      error: (err) => console.error('Error fetching search recommendations', err)
    });
  }

  ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
  }

  // Close overlays when clicking outside
  @HostListener('document:click')
  protected onDocumentClick(): void {
    this.showRecommendations.set(false);
    this.isDropdownOpen.set(false);
  }

  protected onSearchInput(event: Event): void {
    const inputVal = (event.target as HTMLInputElement).value;
    this.searchQuery.set(inputVal);
    this.searchSubject.next(inputVal);
    this.showRecommendations.set(inputVal.trim().length >= 2);
  }

  protected onSearchFocus(): void {
    if (this.searchQuery().trim().length >= 2) {
      this.showRecommendations.set(true);
    }
  }

  protected selectRecommendation(productId: number): void {
    this.showRecommendations.set(false);
    this.router.navigate(['/products', productId]);
  }

  protected triggerSearch(): void {
    const query = this.searchQuery().trim();
    if (query) {
      this.showRecommendations.set(false);
      this.router.navigate(['/search'], { queryParams: { q: query } });
    }
  }

  protected toggleDropdown(event: MouseEvent): void {
    event.stopPropagation();
    this.isDropdownOpen.update(val => !val);
  }

  protected closeDropdown(): void {
    this.isDropdownOpen.set(false);
  }

  protected logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
