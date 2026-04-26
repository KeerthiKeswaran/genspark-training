import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OperatorStats {
  totalBuses: number;
  activeSchedules: number;
  totalBookings: number;
  totalRevenue: number;
  recentTrips: any[];
}

@Injectable({
  providedIn: 'root'
})
export class OperatorService {
  private apiUrl = 'http://localhost:5171/api/Operator';

  constructor(private http: HttpClient) {}

  getStats(operatorId: string): Observable<OperatorStats> {
    return this.http.get<OperatorStats>(`${this.apiUrl}/${operatorId}/stats`);
  }

  getBuses(operatorId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${operatorId}/buses`);
  }

  addBus(operatorId: string, bus: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/${operatorId}/buses`, bus);
  }

  deleteBus(operatorId: string, busId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${operatorId}/buses/${busId}`);
  }

  getSchedules(operatorId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${operatorId}/schedules`);
  }

  createSchedule(operatorId: string, schedule: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/${operatorId}/schedules`, schedule);
  }

  cancelSchedule(operatorId: string, scheduleId: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${operatorId}/schedules/${scheduleId}/cancel`, {});
  }

  getRoutes(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/routes`);
  }

  requestAccountReview(operatorId: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${operatorId}/request-review`, {});
  }
}
