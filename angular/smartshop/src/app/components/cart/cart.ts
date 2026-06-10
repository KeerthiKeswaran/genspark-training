import { Component, inject } from '@angular/core';
import { UpperCasePipe } from '@angular/common';
import { CartService, CartItem } from '../../services/cart.service';
import { InrCurrencyPipe, formatInrValue } from '../../pipes/inr-currency.pipe';

@Component({
  selector: 'app-cart',
  imports: [UpperCasePipe, InrCurrencyPipe],
  templateUrl: './cart.html',
  styleUrl: './cart.css'
})
export class Cart {
  protected readonly cartService = inject(CartService);

  protected close(): void {
    this.cartService.closeCart();
  }

  protected incrementQty(item: CartItem): void {
    this.cartService.updateQuantity(item.product.id, item.quantity + 1);
  }

  protected decrementQty(item: CartItem): void {
    if (item.quantity > 1) {
      this.cartService.updateQuantity(item.product.id, item.quantity - 1);
    } else {
      this.removeItem(item);
    }
  }

  protected removeItem(item: CartItem): void {
    this.cartService.removeFromCart(item.product.id);
  }

  protected checkout(): void {
    alert(`Thank you for your order! Proceeding to checkout for ${this.cartService.cartCount()} items totalling ${formatInrValue(this.cartService.cartTotal())}.`);
    this.cartService.clearCart();
    this.cartService.closeCart();
  }
}
