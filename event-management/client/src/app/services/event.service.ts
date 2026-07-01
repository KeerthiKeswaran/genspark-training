import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap, catchError, throwError, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { AppStoreService } from '../store/app-store.service';
import { ActionTypes } from '../store/actions/app.actions';
import { BrowsedEventResponse, PagedResult } from '../models/event.model';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private readonly baseUrl = 'http://localhost:5106/api';

  constructor(
    private http: HttpClient,
    private store: AppStoreService
  ) {}

  // Helper to map a raw server BrowsedEventResponse (PascalCase) to client model (camelCase)
  private mapEvent(ev: any): BrowsedEventResponse {
    const tiers: any[] = ev.ticketTiers || ev.TicketTiers || [];
    const minPrice = tiers.length > 0
      ? Math.min(...tiers.map((t: any) => t.price ?? t.Price ?? 0))
      : undefined;

    const mapped: BrowsedEventResponse = {
      event_Id: ev.event_Id ?? ev.Event_Id,
      event_Type: ev.event_Type ?? ev.Event_Type ?? '',
      title: ev.title ?? ev.Title ?? '',
      category: ev.category ?? ev.Category,
      description_Url: ev.description_Url ?? ev.Description_Url,
      image_Url: this.resolveImageUrl(ev.image_Url ?? ev.Image_Url),
      date_Time: ev.date_Time ?? ev.Date_Time ?? '',
      duration_Hours: ev.duration_Hours ?? ev.Duration_Hours ?? 0,
      venue_Name: ev.venue_Name ?? ev.Venue_Name,
      venue_Region_Name: ev.venue_Region_Name ?? ev.Venue_Region_Name,
      region_Id: ev.region_Id ?? ev.Region_Id,
      status: ev.status ?? ev.Status,
      organizer_Name: ev.organizer_Name ?? ev.Organizer_Name,
      ticketTiers: tiers.map((t: any) => ({
        tier_Name: t.tier_Name ?? t.Tier_Name ?? '',
        price: t.price ?? t.Price ?? 0,
        tickets_Sold: t.tickets_Sold ?? t.Tickets_Sold ?? 0
      })),
      minPrice
    };

    return mapped;
  }

  // GET /api/Event?keyword=&category=&regionId=&page=1&size=24
  // Server query params: keyword, category, minDateTime, regionId, page, size
  // Server returns: PagedResult<BrowsedEventResponse> → { Items, TotalCount, Page, PageSize, TotalPages }
  public browseEvents(params: {
    keyword?: string;
    category?: string;
    regionId?: string;
    page?: number;
    size?: number;
  }): Observable<PagedResult<BrowsedEventResponse>> {
    this.store.dispatch({ type: ActionTypes.LOAD_EVENTS_START });

    let httpParams = new HttpParams();
    if (params.keyword)  httpParams = httpParams.set('keyword', params.keyword);
    if (params.category) httpParams = httpParams.set('category', params.category);
    if (params.regionId) httpParams = httpParams.set('regionId', params.regionId);
    httpParams = httpParams.set('page', String(params.page ?? 1));
    httpParams = httpParams.set('size', String(params.size ?? 24));

    return this.http.get<any>(`${this.baseUrl}/Event`, { params: httpParams }).pipe(
      map((res) => {
        const mapped: BrowsedEventResponse[] = (res.items ?? res.Items ?? []).map((ev: any) => this.mapEvent(ev));
        return {
          items: mapped,
          totalCount: res.totalCount ?? res.TotalCount ?? 0,
          page: res.page ?? res.Page ?? 1,
          pageSize: res.pageSize ?? res.PageSize ?? (params.size ?? 24),
          totalPages: res.totalPages ?? res.TotalPages ?? 1
        } as PagedResult<BrowsedEventResponse>;
      }),
      tap((result) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_EVENTS_SUCCESS,
          payload: result
        });
      }),
      catchError((err) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_EVENTS_FAIL,
          payload: err.message || 'Failed to load events'
        });
        return throwError(() => err);
      })
    );
  }

  // GET /api/Event/trending?count={n}
  // Server returns: IEnumerable<BrowsedEventResponse>
  public getTrendingEvents(count?: number): Observable<BrowsedEventResponse[]> {
    this.store.dispatch({ type: ActionTypes.LOAD_TRENDING_START });

    let httpParams = new HttpParams();
    if (count) httpParams = httpParams.set('count', String(count));

    return this.http.get<any[]>(`${this.baseUrl}/Event/trending`, { params: httpParams }).pipe(
      map((events) => events.map(ev => this.mapEvent(ev))),
      tap((mapped) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_TRENDING_SUCCESS,
          payload: mapped
        });
      }),
      catchError((err) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_TRENDING_FAIL,
          payload: err.message || 'Failed to load trending events'
        });
        return throwError(() => err);
      })
    );
  }

  // GET /api/Event/recommended   [Authenticated]
  // Server returns: IEnumerable<BrowsedEventResponse> — events in the user's interested region
  public getRecommendedEvents(): Observable<BrowsedEventResponse[]> {
    this.store.dispatch({ type: ActionTypes.LOAD_RECOMMENDED_START });

    return this.http.get<any[]>(`${this.baseUrl}/Event/recommended`).pipe(
      map((events) => events.map(ev => this.mapEvent(ev))),
      tap((mapped) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_RECOMMENDED_SUCCESS,
          payload: mapped
        });
      }),
      catchError((err) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_RECOMMENDED_FAIL,
          payload: err.message || 'Failed to load recommended events'
        });
        return throwError(() => err);
      })
    );
  }

  // GET /api/Event/popular?regionsLimit={n}
  // Server returns: IEnumerable<BrowsedEventResponse>
  public getPopularEvents(regionsLimit?: number): Observable<BrowsedEventResponse[]> {
    let httpParams = new HttpParams();
    if (regionsLimit) httpParams = httpParams.set('regionsLimit', String(regionsLimit));

    return this.http.get<any[]>(`${this.baseUrl}/Event/popular`, { params: httpParams }).pipe(
      map((events) => events.map(ev => this.mapEvent(ev)))
    );
  }

  // GET /api/Event?keyword={kw}&page=1&size=5
  // Query backend search API without modifying the app store state.
  public searchEventsQuick(keyword: string): Observable<BrowsedEventResponse[]> {
    let httpParams = new HttpParams()
      .set('keyword', keyword)
      .set('page', '1')
      .set('size', '5');

    return this.http.get<any>(`${this.baseUrl}/Event`, { params: httpParams }).pipe(
      map((res) => (res.items ?? res.Items ?? []).map((ev: any) => this.mapEvent(ev)))
    );
  }

  // GET /api/Event/{eventId}
  // Server returns: BrowsedEventResponse
  public getEventById(eventId: number): Observable<BrowsedEventResponse> {
    return this.http.get<any>(`${this.baseUrl}/Event/${eventId}`).pipe(
      map(ev => this.mapEvent(ev))
    );
  }
  // GET /api/Event/categories
  // Returns string[] from categories.json. Cached in localStorage under 'event_categories'.
  public getCategories(): Observable<string[]> {
    const CACHE_KEY = 'event_categories';
    const cached = localStorage.getItem(CACHE_KEY);
    if (cached) {
      try {
        const parsed = JSON.parse(cached) as string[];
        if (Array.isArray(parsed) && parsed.length > 0) {
          return of(parsed);
        }
      } catch { /* fall through to API */ }
    }

    return this.http.get<string[]>(`${this.baseUrl}/Event/categories`).pipe(
      tap((categories) => {
        localStorage.setItem(CACHE_KEY, JSON.stringify(categories));
      })
    );
  }

  // GET /api/Event/age-categories
  // Returns { key: string, display: string }[]
  public getAgeCategories(): Observable<{ key: string, display: string }[]> {
    return this.http.get<{ key: string, display: string }[]>(`${this.baseUrl}/Event/age-categories`);
  }

  // ── Organizer APIs ──────────────────────────────────────────────

  // GET /api/user/my-dashboard  [Authenticated]
  public getMyDashboard(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/User/my-dashboard`);
  }

  // GET /api/user/my-events  [Authenticated]
  public getMyEvents(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/User/my-events`);
  }

  // GET /api/user/my-events/{eventId}  [Authenticated]
  public getMyEventDetails(eventId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/User/my-events/${eventId}`);
  }

  // GET /api/event/venues  [Public]
  public getVenues(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/Event/venues`);
  }

  // POST /api/event  [Authenticated] — Create event (returns pending event)
  public createEvent(payload: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/Event`, payload);
  }

  // POST /api/event/{eventId}/create-checkout-session
  public createCheckoutSession(eventId: number, successUrl: string, cancelUrl: string): Observable<{ sessionId: string, sessionUrl: string }> {
    return this.http.post<{ sessionId: string, sessionUrl: string }>(`${this.baseUrl}/Event/${eventId}/create-checkout-session`, {
      successUrl,
      cancelUrl
    });
  }

  // GET /api/event/platform-settings  [Public]
  public getPlatformSettings(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/Event/platform-settings`);
  }

  // POST /api/event/upload-description  [Authenticated]
  public uploadDescription(text: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/Event/upload-description`, { text });
  }

  // POST /api/event/upload-image  [Authenticated]
  public uploadImage(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.baseUrl}/Event/upload-image`, formData);
  }

  // POST /api/event/check-staff  [Authenticated]
  public checkStaffAvailability(venueId: number, dateTime: string, durationHours: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/Event/check-staff`, { venueId, dateTime, durationHours });
  }

  // POST /api/event/{eventId}/confirm  [Authenticated]
  public confirmEvent(eventId: number, stripeChargeId: string, paymentMethod: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/Event/${eventId}/confirm`, { stripeChargeId, paymentMethod });
  }

  // POST /api/event/{eventId}/revert  [Authenticated]
  public revertEvent(eventId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/Event/${eventId}/revert`, {});
  }

  /**
   * Helper to resolve relative assets/images URLs to absolute backend URLs.
   */
  public resolveImageUrl(url: string | null | undefined): string | undefined {
    if (!url) return undefined;
    if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:')) {
      return url;
    }
    const cleanUrl = url.startsWith('/') ? url : '/' + url;
    return `http://localhost:5106${cleanUrl}`;
  }
}
