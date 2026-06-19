import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PixabayService {
  private http = inject(HttpClient);
  private apiKey = environment.pixabayApiKey;
  private cache = new Map<string, string>();
  private readonly cacheStorageKey = 'pixabayImageCache';

  constructor() {
    this.loadCache();
  }

  private getCacheKey(regionName: string): string {
    return regionName.trim().toLowerCase();
  }

  private loadCache(): void {
    if (typeof window === 'undefined') {
      return;
    }

    const rawCache = localStorage.getItem(this.cacheStorageKey);
    if (!rawCache) {
      return;
    }

    try {
      const parsed = JSON.parse(rawCache) as Record<string, string>;
      Object.entries(parsed).forEach(([key, value]) => {
        if (value) {
          this.cache.set(key, value);
        }
      });
    } catch {
      localStorage.removeItem(this.cacheStorageKey);
    }
  }

  private saveCache(): void {
    if (typeof window === 'undefined') {
      return;
    }

    try {
      localStorage.setItem(this.cacheStorageKey, JSON.stringify(Object.fromEntries(this.cache)));
    } catch {
      // If localStorage is disabled or full, we still keep the in-memory cache.
    }
  }

  searchRegionImage(regionName: string): Observable<string> {
    const cacheKey = this.getCacheKey(regionName);
    const existingImage = this.cache.get(cacheKey);
    if (existingImage) {
      return of(existingImage);
    }

    const url = `https://pixabay.com/api/`;
    const params = {
      key: this.apiKey,
      q: `${regionName} landmark`,
      image_type: 'photo',
      orientation: 'horizontal',
      per_page: '3'
    };

    return this.http.get<any>(url, { params }).pipe(
      map(response => {
        const fallback = 'https://images.unsplash.com/photo-1501281668745-f7f57925c3b4?w=600&auto=format&fit=crop&q=80';
        const result = response?.hits?.length > 0 ? response.hits[0].largeImageURL : fallback;
        this.cache.set(cacheKey, result);
        this.saveCache();
        return result;
      }),
      catchError(() => {
        const fallback = 'https://images.unsplash.com/photo-1501281668745-f7f57925c3b4?w=600&auto=format&fit=crop&q=80';
        this.cache.set(cacheKey, fallback);
        this.saveCache();
        return of(fallback);
      })
    );
  }
}
