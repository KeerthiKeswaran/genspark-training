import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { BookingModel, InitiateBookingRequest, ConfirmBookingRequest } from '../models/booking.model';
import { mockBookings } from '../data/booking.mock';

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5106/api/booking';

  /**
   * Get all bookings for the authenticated user.
   * API: GET /api/booking?status=<status>
   */
  getMyBookings(status?: string): Observable<BookingModel[]> {
    // === MOCK DATA (remove when API is ready) ===
    let filtered = [...mockBookings];
    if (status) {
      filtered = filtered.filter(b => b.booking_Status === status);
    }
    return of(filtered);
    // ============================================
    // const params: any = {};
    // if (status) params['status'] = status;
    // return this.http.get<BookingModel[]>(this.baseUrl, { params });
  }

  /**
   * Initiate a new booking for an event.
   * API: POST /api/booking
   */
  initiateBooking(request: InitiateBookingRequest): Observable<{ booking_Id: number }> {
    // === MOCK RESPONSE (remove when API is ready) ===
    return of({ booking_Id: Math.floor(Math.random() * 90000) + 10000 });
    // ================================================
    // return this.http.post<{ booking_Id: number }>(this.baseUrl, request);
  }

  /**
   * Confirm a booking after payment.
   * API: POST /api/booking/{bookingId}/confirm
   */
  confirmBooking(bookingId: number, request: ConfirmBookingRequest): Observable<BookingModel> {
    // === MOCK RESPONSE (remove when API is ready) ===
    const mock: BookingModel = {
      booking_Id: bookingId,
      attendee_Id: 10000,
      event_Id: 0,
      event_Title: 'Mock Event',
      event_Type: 'Physical',
      event_Date_Time: new Date().toISOString(),
      booking_Status: 'Confirmed',
      qr_Code_Data: `BOOKING-${bookingId}-CONFIRMED-${Date.now()}`,
      checkIn_Status: 'Pending',
      created_At: new Date().toISOString(),
      total_Amount: 0,
      details: []
    };
    return of(mock);
    // ================================================
    // return this.http.post<BookingModel>(`${this.baseUrl}/${bookingId}/confirm`, request);
  }

  /**
   * Calculate booking and ticket fees.
   * API: POST /api/booking/calculate-fee
   */
  calculateTicketFee(eventId: number, tierQuantities: Record<string, number>): Observable<{ fee: number }> {
    // === MOCK DATA (remove when API is ready) ===
    let count = 0;
    Object.values(tierQuantities).forEach(q => count += q);
    return of({ fee: count * 45 }); // Mock handling fee of ₹45 per ticket
    // ============================================
    // return this.http.post<{ fee: number }>(`${this.baseUrl}/calculate-fee`, { eventId, tierQuantities });
  }

  /**
   * Cancel a booking.
   * API: POST /api/booking/{bookingId}/cancel
   */
  cancelBooking(bookingId: number): Observable<void> {
    // === MOCK RESPONSE (remove when API is ready) ===
    return of(undefined);
    // ================================================
    // return this.http.post<void>(`${this.baseUrl}/${bookingId}/cancel`, {});
  }

  /**
   * Revert a pending booking.
   * API: POST /api/booking/{bookingId}/revert
   */
  revertBooking(bookingId: number): Observable<void> {
    // === MOCK RESPONSE (remove when API is ready) ===
    return of(undefined);
    // ================================================
    // return this.http.post<void>(`${this.baseUrl}/${bookingId}/revert`, {});
  }
}
