import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProductService, Product } from '../../services/product.service';
import { switchMap } from 'rxjs/operators';
import { Subscription } from 'rxjs';

import { InrCurrencyPipe } from '../../pipes/inr-currency.pipe';

@Component({
  selector: 'app-product-list',
  imports: [RouterLink, InrCurrencyPipe],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css'
})
export class ProductList {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private querySub?: Subscription;

  // Reactive state signals
  protected readonly products = signal<Product[]>([]);
  protected readonly isLoading = signal<boolean>(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly currentCategory = signal<string | null>(null);

  constructor() {
    // Listen to query parameters to fetch products dynamically
    this.querySub = this.route.queryParamMap.pipe(
      switchMap(params => {
        this.isLoading.set(true);
        this.errorMessage.set(null);
        const category = params.get('category');
        this.currentCategory.set(category);

        // Fetch products based on category slug (no delay)
        return category
          ? this.productService.getProductsByCategory(category)
          : this.productService.getMixedProducts();
      })
    ).subscribe({
      next: (data) => {
        this.products.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.errorMessage.set('Failed to load products. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    if (this.querySub) {
      this.querySub.unsubscribe();
    }
  }

  // Format category name for display (e.g., "mobile-phones" to "Mobile Phones")
  protected getDisplayName(slug: string | null): string {
    if (!slug) return 'Deals of the Day';
    return slug
      .split('-')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }
}
