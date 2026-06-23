import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, from } from 'rxjs';
import { map, catchError, switchMap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface CacheEntry {
  dataUrl: string | null;  // base64 data URL of the image, not a remote URL
  timestamp: number;
}

@Injectable({
  providedIn: 'root'
})
export class PixabayService {
  private http = inject(HttpClient);
  private apiKey = environment.pixabayApiKey;
  private readonly cacheStorageKey = 'pixabayImageCache';
  private readonly cacheTTL = 24 * 60 * 60 * 1000; // 24 hours TTL

  constructor() {
    this.migrateOldCache();
  }

  private getCacheKey(regionName: string): string {
    return regionName.trim().toLowerCase();
  }

  /** Remove old-format cache entries that stored remote URLs instead of base64 data */
  private migrateOldCache(): void {
    if (typeof window === 'undefined') return;

    const rawCache = localStorage.getItem(this.cacheStorageKey);
    if (!rawCache) return;

    try {
      const parsed = JSON.parse(rawCache) as Record<string, any>;
      let needsUpdate = false;

      Object.entries(parsed).forEach(([key, value]) => {
        // Old format stored 'url' field or remote URLs — purge them
        if (value && (value.url !== undefined || (value.dataUrl && value.dataUrl.startsWith('http')))) {
          delete parsed[key];
          needsUpdate = true;
        }
      });

      if (needsUpdate) {
        localStorage.setItem(this.cacheStorageKey, JSON.stringify(parsed));
      }
    } catch {
      localStorage.removeItem(this.cacheStorageKey);
    }
  }

  private readCache(): Record<string, CacheEntry> {
    if (typeof window === 'undefined') return {};

    const rawCache = localStorage.getItem(this.cacheStorageKey);
    if (!rawCache) return {};

    try {
      return JSON.parse(rawCache) || {};
    } catch {
      localStorage.removeItem(this.cacheStorageKey);
      return {};
    }
  }

  private writeCache(cache: Record<string, CacheEntry>): void {
    if (typeof window === 'undefined') return;

    try {
      localStorage.setItem(this.cacheStorageKey, JSON.stringify(cache));
    } catch {
      // localStorage full — silently fail
    }
  }

  /** Convert a remote image URL to a base64 data URL by fetching it as a blob */
  private fetchImageAsBase64(imageUrl: string): Observable<string | null> {
    return this.http.get(imageUrl, { responseType: 'blob' }).pipe(
      switchMap(blob => {
        return from(new Promise<string | null>((resolve) => {
          const reader = new FileReader();
          reader.onloadend = () => resolve(reader.result as string);
          reader.onerror = () => resolve(null);
          reader.readAsDataURL(blob);
        }));
      }),
      catchError(() => of(null))
    );
  }

  searchRegionImage(regionName: string): Observable<string | null> {
    if (typeof window === 'undefined') {
      return of(null);
    }

    const cacheKey = this.getCacheKey(regionName);
    const now = Date.now();
    const cache = this.readCache();
    const existing = cache[cacheKey];

    // If we have a valid base64 data URL cached and it hasn't expired, return it directly
    if (existing && existing.dataUrl && existing.dataUrl.startsWith('data:') && (now - existing.timestamp < this.cacheTTL)) {
      return of(existing.dataUrl);
    }

    // Skip API request if key is placeholder or default
    if (!this.apiKey || this.apiKey === 'YOUR_PIXABAY_API_KEY') {
      const latestCache = this.readCache();
      latestCache[cacheKey] = { dataUrl: null, timestamp: now };
      this.writeCache(latestCache);
      return of(null);
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
      switchMap(response => {
        const imageUrl = response?.hits?.length > 0 ? response.hits[0].webformatURL : null;

        if (!imageUrl) {
          const latestCache = this.readCache();
          latestCache[cacheKey] = { dataUrl: null, timestamp: now };
          this.writeCache(latestCache);
          return of(null);
        }

        // Download the image as a blob and convert to base64 data URL
        return this.fetchImageAsBase64(imageUrl).pipe(
          map(dataUrl => {
            const latestCache = this.readCache();
            latestCache[cacheKey] = { dataUrl, timestamp: now };
            this.writeCache(latestCache);
            return dataUrl;
          })
        );
      }),
      catchError(() => {
        const latestCache = this.readCache();
        latestCache[cacheKey] = { dataUrl: null, timestamp: now };
        this.writeCache(latestCache);
        return of(null);
      })
    );
  }
}
