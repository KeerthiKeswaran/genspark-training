import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface City {
  id: string;
  name: string;
  state: string;
}

export interface Hub {
  id: string;
  name: string;
  type: string; // "Boarding", "Dropping", "Both"
}

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5171/api/locations';

  getCities(): Observable<City[]> {
    return this.http.get<City[]>(`${this.apiUrl}/cities`);
  }

  getHubs(cityId: string): Observable<Hub[]> {
    return this.http.get<Hub[]>(`${this.apiUrl}/hubs`, { params: { cityId } });
  }
}
