import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of, throwError } from 'rxjs';
import { AppStoreService } from '../store/app-store.service';
import { ActionTypes } from '../store/actions/app.actions';
import { RegionModel } from '../models/region.model';
import { mockRegions } from '../data/region.mock';

@Injectable({
  providedIn: 'root'
})
export class RegionService {
  private readonly baseUrl = 'http://localhost:5106/api';

  constructor(
    private http: HttpClient,
    private store: AppStoreService
  ) {}

  public loadRegions(): Observable<RegionModel[]> {
    this.store.dispatch({ type: ActionTypes.LOAD_REGIONS_START });
    
    // Commented out server HTTP call:
    /*
    return this.http.get<RegionModel[]>(`${this.baseUrl}/regions`).pipe(
      tap((regions) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_REGIONS_SUCCESS,
          payload: regions
        });
      }),
      catchError((err) => {
        this.store.dispatch({
          type: ActionTypes.LOAD_REGIONS_FAIL,
          payload: err.message || 'Failed to load regions'
        });
        return throwError(() => err);
      })
    );
    */

    this.store.dispatch({
      type: ActionTypes.LOAD_REGIONS_SUCCESS,
      payload: mockRegions
    });

    return of(mockRegions);
  }

  public setLocalRegion(regionId: string): void {
    this.store.dispatch({
      type: ActionTypes.SET_REGION,
      payload: regionId
    });
    if (typeof window !== 'undefined') {
      localStorage.setItem('currentRegionId', regionId);
    }
  }
}
