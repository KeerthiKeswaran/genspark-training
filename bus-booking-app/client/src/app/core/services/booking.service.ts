import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { 
  SeatLayout, 
  LockSeatsRequest, 
  LockSeatsResponse, 
  ConfirmBookingRequest, 
  BookingResponse, 
  BookingHistory 
} from '../models/booking.models';

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5171/api/booking';

  getLayout(journeyId: string): Observable<SeatLayout> {
    return this.http.get<SeatLayout>(`${this.apiUrl}/layout/${journeyId}`);
  }


  lockSeats(request: LockSeatsRequest): Observable<LockSeatsResponse> {
    return this.http.post<LockSeatsResponse>(`${this.apiUrl}/lock-seats`, request);
  }

  confirmBooking(request: ConfirmBookingRequest): Observable<BookingResponse> {
    return this.http.post<BookingResponse>(`${this.apiUrl}/confirm`, request);
  }

  releaseLocks(journeyId: string, seats: string[]): Observable<any> {
    return this.http.delete(`${this.apiUrl}/release-locks`, {
      params: { journeyId, seats: seats.join(',') }
    });
  }

  getHistory(): Observable<BookingHistory[]> {
    return this.http.get<BookingHistory[]>(`${this.apiUrl}/history`);
  }

  cancelBooking(bookingId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${bookingId}`);
  }
}
