import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, throwError, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';

export interface Product {
  id: number;
  title: string;
  description: string;
  price: number;
  rating: number;
  stock: number;
  brand: string;
  category: string;
  thumbnail: string;
  images: string[];
}

export interface ProductResponse {
  products: Product[];
  total: number;
  skip: number;
  limit: number;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly http = inject(HttpClient);

  // In-memory cache map to hold loaded product details on the frontend
  private readonly productsCache = new Map<number, Product>();

  // Dictionary mapping user-friendly meta-categories to multiple DummyJSON subcategories
  private readonly metaCategories: { [key: string]: string[] } = {
    electronics: ['laptops', 'smartphones', 'mobile-accessories', 'tablets'],
    beauty: ['beauty', 'fragrances'],
    fashion: ['mens-shirts', 'mens-shoes', 'womens-dresses', 'womens-shoes', 'womens-bags', 'womens-jewellery', 'sunglasses'],
    groceries: ['groceries'],
    furniture: ['furniture', 'home-decoration', 'kitchen-accessories'],
    pharmacy: ['skin-care'],
    sports: ['sports-accessories'],
    automotive: ['vehicle', 'motorcycle']
  };

  // Helper method to add loaded products into the memory cache
  private cacheProducts(products: Product[]): void {
    products.forEach(product => this.productsCache.set(product.id, product));
  }

  // Fetch a mix of products (e.g. deals of the day)
  getMixedProducts(limit = 12): Observable<Product[]> {
    return this.http.get<ProductResponse>(`https://dummyjson.com/products?limit=${limit}`)
      .pipe(
        map(response => response.products),
        tap(products => this.cacheProducts(products)), // Cache products on fetch
        catchError(error => {
          console.error('Error fetching mixed products:', error);
          return throwError(() => new Error('Failed to load products.'));
        })
      );
  }

  // Fetch products belonging to a specific category (supporting multi-subcategory aggregation)
  getProductsByCategory(category: string, limit = 12): Observable<Product[]> {
    const subcategories = this.metaCategories[category.toLowerCase()] || [category];

    // Build parallel API requests for each subcategory
    const requests = subcategories.map(subcat => {
      // Calculate partition size per category
      const partitionLimit = Math.max(4, Math.ceil(limit / subcategories.length));
      return this.http.get<ProductResponse>(`https://dummyjson.com/products/category/${subcat}?limit=${partitionLimit}`)
        .pipe(
          map(response => response.products),
          catchError(() => {
            // Return empty list on single subcat failure so others can load
            return [];
          })
        );
    });

    // Run parallel calls via forkJoin and aggregate the results
    return forkJoin(requests).pipe(
      map(results => {
        // Flatten array of arrays
        const allProducts = results.reduce((acc, curr) => acc.concat(curr), []);
        // Sort items by rating in descending order to show "top products"
        const sorted = allProducts.sort((a, b) => b.rating - a.rating).slice(0, limit);
        this.cacheProducts(sorted); // Cache the final combined results
        return sorted;
      }),
      catchError(error => {
        console.error(`Error loading category ${category} products:`, error);
        return throwError(() => new Error(`Failed to load ${category} products.`));
      })
    );
  }

  // Fetch a single product by ID (using frontend memory cache or fallback API call)
  getProductById(id: number): Observable<Product> {
    const cachedProduct = this.productsCache.get(id);
    if (cachedProduct) {
      // Return cached data immediately as an observable
      return of(cachedProduct);
    }

    // Fallback: If not in cache (e.g. direct page refresh), load from API and add it to cache
    return this.http.get<Product>(`https://dummyjson.com/products/${id}`)
      .pipe(
        tap(product => this.productsCache.set(product.id, product)), // Cache it for future routes
        catchError(error => {
          console.error(`Error loading product details for ID ${id}:`, error);
          return throwError(() => new Error('Failed to load product details.'));
        })
      );
  }

  // Search products on DummyJSON API
  searchProducts(query: string): Observable<Product[]> {
    if (!query || !query.trim()) {
      return of([]);
    }
    return this.http.get<ProductResponse>(`https://dummyjson.com/products/search?q=${encodeURIComponent(query)}`)
      .pipe(
        map(response => response.products),
        tap(products => this.cacheProducts(products)), // Cache products on fetch
        catchError(error => {
          console.error('Error searching products:', error);
          return throwError(() => new Error('Failed to search products.'));
        })
      );
  }
}
