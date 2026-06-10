import { Injectable, signal, computed } from '@angular/core';
import { Product } from './product.service';

export interface CartItem {
  product: Product;
  quantity: number;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  // Signal managing the state of items in the cart
  private readonly cartItemsSignal = signal<CartItem[]>([]);

  // Signal managing the open/close state of the cart panel
  readonly isOpen = signal<boolean>(false);

  // Read-only signal exposed to components
  readonly items = this.cartItemsSignal.asReadonly();

  // Computed signal for total number of items in the cart
  readonly cartCount = computed(() => 
    this.cartItemsSignal().reduce((sum, item) => sum + item.quantity, 0)
  );

  // Computed signal for total price of items in the cart
  readonly cartTotal = computed(() => 
    this.cartItemsSignal().reduce((sum, item) => sum + item.product.price * item.quantity, 0)
  );

  /**
   * Adds a product to the cart. If it already exists, increments quantity.
   */
  addToCart(product: Product, quantity = 1): void {
    this.cartItemsSignal.update(items => {
      const existingItem = items.find(item => item.product.id === product.id);
      
      if (existingItem) {
        // Clamp quantity to the product's available stock
        const newQty = Math.min(existingItem.quantity + quantity, product.stock);
        return items.map(item => 
          item.product.id === product.id 
            ? { ...item, quantity: newQty } 
            : item
        );
      } else {
        // Clamp initial quantity to the product's available stock
        const initialQty = Math.min(quantity, product.stock);
        return [...items, { product, quantity: initialQty }];
      }
    });
    
    // Automatically open the cart drawer to show the user the update
    this.isOpen.set(true);
  }

  /**
   * Updates the quantity of a specific item in the cart.
   */
  updateQuantity(productId: number, quantity: number): void {
    this.cartItemsSignal.update(items => 
      items.map(item => {
        if (item.product.id === productId) {
          const clampedQty = Math.max(1, Math.min(quantity, item.product.stock));
          return { ...item, quantity: clampedQty };
        }
        return item;
      })
    );
  }

  /**
   * Removes a product from the cart by its ID.
   */
  removeFromCart(productId: number): void {
    this.cartItemsSignal.update(items => 
      items.filter(item => item.product.id !== productId)
    );
  }

  /**
   * Clears all items in the cart.
   */
  clearCart(): void {
    this.cartItemsSignal.set([]);
  }

  /**
   * Opens the cart drawer.
   */
  openCart(): void {
    this.isOpen.set(true);
  }

  /**
   * Closes the cart drawer.
   */
  closeCart(): void {
    this.isOpen.set(false);
  }

  /**
   * Toggles the cart drawer open/close state.
   */
  toggleCart(): void {
    this.isOpen.update(open => !open);
  }
}
