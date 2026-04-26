import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BusSearchResult } from '../models/search.models';

@Injectable({
  providedIn: 'root'
})
export class SearchService {
  private apiUrl = 'http://localhost:5171/api/search';

  constructor(private http: HttpClient) {}

  searchBuses(from: string, to: string, date: string): Observable<BusSearchResult[]> {
    const params = new HttpParams()
      .set('from', from)
      .set('to', to)
      .set('date', date);
    
    return this.http.get<BusSearchResult[]>(this.apiUrl, { params });
  }

  getCities(query: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/cities`, {
      params: new HttpParams().set('query', query)
    });
  }
}
