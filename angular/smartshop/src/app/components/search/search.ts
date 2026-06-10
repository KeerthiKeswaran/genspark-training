import { Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { ProductService, Product } from '../../services/product.service';
import { CartService } from '../../services/cart.service';
import { UpperCasePipe } from '@angular/common';
import { InrCurrencyPipe, usdToInr } from '../../pipes/inr-currency.pipe';

@Component({
  selector: 'app-search',
  imports: [RouterLink, UpperCasePipe, InrCurrencyPipe],
  templateUrl: './search.html',
  styleUrl: './search.css'
})
export class SearchComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  protected readonly cartService = inject(CartService);

  private queryParamsSub?: Subscription;

  // Query parameter states
  protected readonly searchQuery = signal<string>('');
  protected readonly selectedCategory = signal<string>('');
  protected readonly selectedBrands = signal<string[]>([]);
  protected readonly minPriceParam = signal<number | null>(null);
  protected readonly maxPriceParam = signal<number | null>(null);
  protected readonly activeSort = signal<string>('default');

  // API data states
  protected readonly rawResults = signal<Product[]>([]);
  protected readonly isLoading = signal<boolean>(true);
  protected readonly errorMessage = signal<string | null>(null);

  // Dynamic filter lists compiled from search results
  protected readonly availableCategories = computed(() => {
    const categories = this.rawResults().map(p => p.category);
    return Array.from(new Set(categories)).sort();
  });

  protected readonly availableBrands = computed(() => {
    const brands = this.rawResults().map(p => p.brand).filter(Boolean);
    return Array.from(new Set(brands)).sort();
  });

  // Calculate global min and max prices from raw results in INR
  protected readonly priceRange = computed(() => {
    const prices = this.rawResults().map(p => usdToInr(p.price));
    if (prices.length === 0) return { min: 0, max: 100000 };
    return {
      min: Math.floor(Math.min(...prices)),
      max: Math.ceil(Math.max(...prices))
    };
  });

  // Computed signal to filter and sort results on the frontend (comparing in INR)
  protected readonly filteredResults = computed(() => {
    let items = [...this.rawResults()];

    // 1. Filter by category
    const cat = this.selectedCategory();
    if (cat) {
      items = items.filter(item => item.category.toLowerCase() === cat.toLowerCase());
    }

    // 2. Filter by brand checklist
    const brands = this.selectedBrands();
    if (brands.length > 0) {
      items = items.filter(item => item.brand && brands.includes(item.brand.toLowerCase()));
    }

    // 3. Filter by min price (in INR)
    const minP = this.minPriceParam();
    if (minP !== null) {
      items = items.filter(item => usdToInr(item.price) >= minP);
    }

    // 4. Filter by max price (in INR)
    const maxP = this.maxPriceParam();
    if (maxP !== null) {
      items = items.filter(item => usdToInr(item.price) <= maxP);
    }

    // 5. Apply sorting (by price converted to INR or relative USD is equivalent)
    const sort = this.activeSort();
    if (sort === 'price-asc') {
      items.sort((a, b) => a.price - b.price);
    } else if (sort === 'price-desc') {
      items.sort((a, b) => b.price - a.price);
    } else if (sort === 'rating-desc') {
      items.sort((a, b) => b.rating - a.rating);
    }

    return items;
  });

  ngOnInit(): void {
    // Listen to query parameters reactively
    this.queryParamsSub = this.route.queryParams.subscribe(params => {
      const q = params['q'] || '';
      this.searchQuery.set(q);
      
      // Update filters and sort from URL
      this.selectedCategory.set(params['category'] || '');
      
      const brandParam = params['brand'] || '';
      this.selectedBrands.set(brandParam ? brandParam.split(',') : []);

      this.minPriceParam.set(params['minPrice'] ? Number(params['minPrice']) : null);
      this.maxPriceParam.set(params['maxPrice'] ? Number(params['maxPrice']) : null);
      this.activeSort.set(params['sort'] || 'default');

      // Fetch new search results if query changed
      this.fetchResults(q);
    });
  }

  ngOnDestroy(): void {
    if (this.queryParamsSub) {
      this.queryParamsSub.unsubscribe();
    }
  }

  private fetchResults(query: string): void {
    if (!query || !query.trim()) {
      this.rawResults.set([]);
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.productService.searchProducts(query).subscribe({
      next: (products) => {
        this.rawResults.set(products);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.errorMessage.set('Failed to fetch search results. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  // Set filter parameter in routing query params (syncs back to UI via subscripton)
  protected updateFilterParam(key: string, value: string | number | null): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { [key]: value || null },
      queryParamsHandling: 'merge'
    });
  }

  protected toggleBrandFilter(brand: string): void {
    const lowerBrand = brand.toLowerCase();
    const current = this.selectedBrands();
    let updated: string[];
    
    if (current.includes(lowerBrand)) {
      updated = current.filter(b => b !== lowerBrand);
    } else {
      updated = [...current, lowerBrand];
    }
    
    this.updateFilterParam('brand', updated.join(',') || null);
  }

  protected setPriceFilter(min: number | null, max: number | null): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        minPrice: min || null,
        maxPrice: max || null
      },
      queryParamsHandling: 'merge'
    });
  }

  protected clearAllFilters(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        q: this.searchQuery(),
        category: null,
        brand: null,
        minPrice: null,
        maxPrice: null,
        sort: 'default'
      }
    });
  }

  protected addToCart(event: Event, product: Product): void {
    event.stopPropagation(); // Avoid navigating to details page
    this.cartService.addToCart(product, 1);
  }

  protected getRatingStars(rating: number): number[] {
    const rounded = Math.round(rating);
    return Array(5).fill(0).map((_, i) => i < rounded ? 1 : 0);
  }
}
