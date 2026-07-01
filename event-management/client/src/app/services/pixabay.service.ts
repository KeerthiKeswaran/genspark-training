import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, from } from 'rxjs';
import { map, catchError, switchMap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PixabayService {
  private http = inject(HttpClient);
  private apiKey = environment.pixabayApiKey;
  private readonly cacheStorageKey = 'pixabayImageCache';

  private readCache(): Record<string, any> {
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

  private writeCache(cache: Record<string, any>): void {
    if (typeof window === 'undefined') return;

    try {
      localStorage.setItem(this.cacheStorageKey, JSON.stringify(cache));
    } catch {
      // localStorage full — silently fail
    }
  }

  private blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => resolve(reader.result as string);
      reader.onerror = reject;
      reader.readAsDataURL(blob);
    });
  }

  searchRegionImage(regionId: string, regionName: string): Observable<any | null> {
    if (typeof window === 'undefined') {
      return of(null);
    }

    const cacheKey = regionId;
    const cache = this.readCache();

    const existing = cache[cacheKey];
    // Check if the key (regionId) exists directly in the cache object and has non-null data
    if (existing && existing.data) {
      return of(existing);
    }

    // Skip API request if key is placeholder or default
    if (!this.apiKey || this.apiKey === 'YOUR_PIXABAY_API_KEY') {
      const latestCache = this.readCache();
      latestCache[cacheKey] = null;
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
        const imageObj = response?.hits?.length > 0 ? response.hits[0] : null;
        if (!imageObj) {
          return of(null);
        }

        const imageUrl = imageObj.webformatURL || imageObj.previewURL;

        // Download the image as a Blob, convert to Base64, and store strictly { id, data }
        return this.http.get(imageUrl, { responseType: 'blob' }).pipe(
          switchMap(blob => from(this.blobToBase64(blob))),
          map(base64 => {
            const entry = {
              id: imageObj.id,
              data: base64
            };
            const latestCache = this.readCache();
            latestCache[cacheKey] = entry;
            this.writeCache(latestCache);
            return entry;
          }),
          catchError((err) => {
            console.error('Failed to fetch image blob or convert to base64', err);
            // Fallback: save the ID and null data so we don't repeat API calls
            const entry = {
              id: imageObj.id,
              data: null
            };
            const latestCache = this.readCache();
            latestCache[cacheKey] = entry;
            this.writeCache(latestCache);
            return of(entry);
          })
        );
      }),
      catchError(() => {
        const latestCache = this.readCache();
        latestCache[cacheKey] = null;
        this.writeCache(latestCache);
        return of(null);
      })
    );
  }
}
