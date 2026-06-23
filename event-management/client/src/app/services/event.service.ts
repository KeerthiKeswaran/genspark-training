import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { AppStoreService } from '../store/app-store.service';
import { ActionTypes } from '../store/actions/app.actions';
import { BrowsedEventResponse, PagedResult } from '../models/event.model';
import { mockAllEvents } from '../data/event.mock';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private readonly baseUrl = 'http://localhost:5106/api';

  private readonly mockAllEvents: BrowsedEventResponse[] = mockAllEvents;

  constructor(
    private http: HttpClient,
    private store: AppStoreService
  ) {}

  public browseEvents(params: {
    keyword?: string;
    category?: string;
    regionId?: string;
    regionIds?: string[];
    format?: string;
    maxPrice?: number;
    sortBy?: string;
    page?: number;
    size?: number;
  }): Observable<PagedResult<BrowsedEventResponse>> {
    this.store.dispatch({ type: ActionTypes.LOAD_EVENTS_START });

    // Simulated Mock filtering:
    let filtered = [...this.mockAllEvents];

    const activeRegionId = params.regionId || (typeof window !== 'undefined' ? localStorage.getItem('currentRegionId') : null) || 'REG01';

    // If checkboxes are checked, filter by those checked region IDs.
    if (params.regionIds && params.regionIds.length > 0) {
      filtered = filtered.filter(e => !!e.region_Id && params.regionIds!.includes(e.region_Id));
    }

    if (params.keyword) {
      const kw = params.keyword.toLowerCase();
      filtered = filtered.filter(e => 
        e.title.toLowerCase().includes(kw) || 
        (e.descriptionUrl && e.descriptionUrl.toLowerCase().includes(kw))
      );
    }

    if (params.category) {
      const cat = params.category.toLowerCase();
      filtered = filtered.filter(e => {
        const titleAndDesc = (e.title + ' ' + (e.descriptionUrl || '')).toLowerCase();
        if (cat === 'music') return titleAndDesc.includes('music') || titleAndDesc.includes('concert') || titleAndDesc.includes('symphony') || titleAndDesc.includes('recital');
        if (cat === 'technology') return titleAndDesc.includes('tech') || titleAndDesc.includes('ai') || titleAndDesc.includes('hackathon') || titleAndDesc.includes('blockchain') || titleAndDesc.includes('software') || titleAndDesc.includes('saas');
        if (cat === 'food') return titleAndDesc.includes('food') || titleAndDesc.includes('carnival') || titleAndDesc.includes('jigarthanda') || titleAndDesc.includes('bbq') || titleAndDesc.includes('wine') || titleAndDesc.includes('organic') || titleAndDesc.includes('tasting');
        if (cat === 'business') return titleAndDesc.includes('business') || titleAndDesc.includes('leadership') || titleAndDesc.includes('conclave') || titleAndDesc.includes('summit') || titleAndDesc.includes('startup') || titleAndDesc.includes('seminar') || titleAndDesc.includes('growth');
        if (cat === 'art') return titleAndDesc.includes('art') || titleAndDesc.includes('film') || titleAndDesc.includes('sculpting') || titleAndDesc.includes('exhibition') || titleAndDesc.includes('heritage') || titleAndDesc.includes('weaver') || titleAndDesc.includes('walk') || titleAndDesc.includes('tour') || titleAndDesc.includes('folk');
        return true;
      });
    }

    // Format Filter (Physical, Virtual, Hybrid)
    if (params.format) {
      const fmt = params.format.toLowerCase();
      filtered = filtered.filter(e => e.eventType.toLowerCase() === fmt);
    }

    // Max Price Filter
    if (params.maxPrice !== undefined && params.maxPrice !== null && !isNaN(params.maxPrice)) {
      filtered = filtered.filter(e => e.minPrice !== undefined && e.minPrice <= params.maxPrice!);
    }

    // Custom Sorting Options
    if (params.sortBy === 'priceAsc') {
      filtered.sort((a, b) => (a.minPrice || 0) - (b.minPrice || 0));
    } else if (params.sortBy === 'priceDesc') {
      filtered.sort((a, b) => (b.minPrice || 0) - (a.minPrice || 0));
    } else if (params.sortBy === 'dateAsc') {
      filtered.sort((a, b) => new Date(a.dateTime).getTime() - new Date(b.dateTime).getTime());
    } else if (params.sortBy === 'dateDesc') {
      filtered.sort((a, b) => new Date(b.dateTime).getTime() - new Date(a.dateTime).getTime());
    } else {
      // Default: active region first, then soonest date first
      filtered.sort((a, b) => {
        const aIsActive = a.region_Id === activeRegionId ? 1 : 0;
        const bIsActive = b.region_Id === activeRegionId ? 1 : 0;
        if (aIsActive !== bIsActive) {
          return bIsActive - aIsActive;
        }
        return new Date(a.dateTime).getTime() - new Date(b.dateTime).getTime();
      });
    }

    const pageSize = params.size || 10;
    const pageNum = params.page || 1;
    const totalCount = filtered.length;
    const totalPages = Math.ceil(totalCount / pageSize);
    
    const startIndex = (pageNum - 1) * pageSize;
    const paginatedItems = filtered.slice(startIndex, startIndex + pageSize);

    const result: PagedResult<BrowsedEventResponse> = {
      items: paginatedItems,
      totalCount,
      page: pageNum,
      size: pageSize,
      totalPages
    };

    this.store.dispatch({
      type: ActionTypes.LOAD_EVENTS_SUCCESS,
      payload: result
    });

    return of(result);
  }

  public getTrendingEvents(): Observable<BrowsedEventResponse[]> {
    this.store.dispatch({ type: ActionTypes.LOAD_TRENDING_START });

    const mockTrending = [
      this.mockAllEvents[4], // A.R. Rahman Symphony Concert (Chennai)
      this.mockAllEvents[1], // Tech Expo (Chennai)
      this.mockAllEvents[13] // Madurai Chithirai Heritage Art Festival
    ];

    this.store.dispatch({
      type: ActionTypes.LOAD_TRENDING_SUCCESS,
      payload: mockTrending
    });
    return of(mockTrending);
  }
}
