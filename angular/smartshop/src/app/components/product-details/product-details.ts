import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { ProductService, Product } from '../../services/product.service';
import { CartService } from '../../services/cart.service';
import { UpperCasePipe } from '@angular/common';
import { InrCurrencyPipe } from '../../pipes/inr-currency.pipe';

@Component({
  selector: 'app-product-details',
  imports: [RouterLink, UpperCasePipe, InrCurrencyPipe],
  templateUrl: './product-details.html',
  styleUrl: './product-details.css'
})
export class ProductDetails implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private routeSub?: Subscription;

  // Reactive state signals
  protected readonly product = signal<Product | null>(null);
  protected readonly selectedImage = signal<string>('');
  protected readonly isLoading = signal<boolean>(true);
  protected readonly errorMessage = signal<string | null>(null);

  // Cart / Buy actions
  protected readonly quantity = signal<number>(1);

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.pipe(
      switchMap(params => {
        this.isLoading.set(true);
        this.errorMessage.set(null);
        const id = Number(params.get('id'));
        return this.productService.getProductById(id);
      })
    ).subscribe({
      next: (data) => {
        this.product.set(data);
        this.selectedImage.set(data.thumbnail || (data.images && data.images[0]) || '');
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.errorMessage.set('Failed to load product details. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    if (this.routeSub) {
      this.routeSub.unsubscribe();
    }
  }

  protected changeImage(img: string): void {
    this.selectedImage.set(img);
  }

  protected incrementQty(): void {
    const stock = this.product()?.stock || 0;
    if (this.quantity() < stock) {
      this.quantity.update(q => q + 1);
    }
  }

  protected decrementQty(): void {
    if (this.quantity() > 1) {
      this.quantity.update(q => q - 1);
    }
  }

  protected addToCart(): void {
    const prod = this.product();
    if (prod) {
      this.cartService.addToCart(prod, this.quantity());
    }
  }

  protected buyNow(): void {
    const prod = this.product();
    if (prod) {
      this.cartService.addToCart(prod, this.quantity());
      this.cartService.openCart();
    }
  }

  protected getRatingStars(rating: number): number[] {
    const rounded = Math.round(rating);
    return Array(5).fill(0).map((_, i) => i < rounded ? 1 : 0);
  }
}
