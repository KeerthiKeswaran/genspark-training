// Admin service for handling platform management
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface City {
  id?: string;
  name: string;
  state: string;
}

export interface Hub {
  id?: string;
  name: string;
  cityId: string;
  type: string;
}

export interface Route {
  id?: string;
  source: string;
  destination: string;
  distanceKm: number;
}

export interface Operator {
  id: string;
  companyName: string;
  email: string;
  phone: string;
  isApproved: boolean;
  status: string;
  rejectionReason?: string;
  createdAt: string;
}

export interface FeeSetting {
  feeType: string;
  feeValue: number;
  commissionPercentage: number;
}

export interface AdminTrip {
  scheduleId: string;
  route: string;
  operator: string;
  departure: string;
  status: string;
  bookedSeats: number;
  cancelledSeats: number;
  maxSeats: number;
  price: number;
}

export interface AdminStats {
  totalBookings: number;
  upcomingBookings: number;
  completedBookings: number;
  grossBookingRevenue: number;
  netRevenue: number;
  operatorPayout: number;
  activeOperators: number;
  totalCities: number;
  recentTrips: AdminTrip[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private apiUrl = 'http://localhost:5171/api/Admin';

  constructor(private http: HttpClient) {}

  getStats(startDate?: string, endDate?: string, operatorId?: string): Observable<AdminStats> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    if (operatorId) params = params.set('operatorId', operatorId);
    
    return this.http.get<AdminStats>(`${this.apiUrl}/stats`, { params });
  }

  addCity(city: City): Observable<any> {
    return this.http.post(`${this.apiUrl}/cities`, city);
  }

  addHub(hub: Hub): Observable<any> {
    return this.http.post(`${this.apiUrl}/hubs`, hub);
  }

  getOperators(): Observable<Operator[]> {
    return this.http.get<Operator[]>(`${this.apiUrl}/operators`);
  }

  approveOperator(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/operators/${id}/approve`, {});
  }

  denyOperator(id: string, reason: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/operators/${id}/deny`, { reason });
  }

  deactivateOperator(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/operators/${id}/deactivate`, {});
  }

  activateOperator(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/operators/${id}/activate`, {});
  }

  getFeeSettings(): Observable<FeeSetting> {
    return this.http.get<FeeSetting>(`${this.apiUrl}/settings/fee`);
  }

  updateFeeSettings(settings: FeeSetting): Observable<any> {
    return this.http.put(`${this.apiUrl}/settings/fee`, settings);
  }

  getRoutes(): Observable<Route[]> {
    return this.http.get<Route[]>(`${this.apiUrl}/routes`);
  }

  addRoute(route: Route): Observable<any> {
    return this.http.post(`${this.apiUrl}/routes`, route);
  }

  getPendingBuses(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/buses/pending`);
  }

  approveBus(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/buses/${id}/approve`, {});
  }

  denyBus(id: string, reason: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/buses/${id}/deny`, { reason });
  }
}
