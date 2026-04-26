export interface SeatStatus {
  seatNumber: string;
  status: 'Booked' | 'Blocked';
  gender?: 'Male' | 'Female' | 'Other';
}

export interface SeatLayout {
  layoutConfig: string | null;
  unavailableSeats: SeatStatus[];
  source: string;
  destination: string;
  departureTime: string;
  busNumber: string;
  busName: string;
  platformFeeType: string;
  platformFeeValue: number;
}

export interface LockSeatsRequest {
  journeyId: string;
  seatNumbers: string[];
}

export interface LockSeatsResponse {
  lockId: string;
  expiresAt: string;
}

export interface Passenger {
  seatNumber: string;
  name: string;
  age: number;
  gender: 'M' | 'F' | 'Other';
}

export interface ConfirmBookingRequest {
  journeyId: string;
  passengers: Passenger[];
  paymentToken?: string;
  boardingPointId: string;
  droppingPointId: string;
}

export interface BookingResponse {
  bookingId: string;
  status: string;
}

export interface BookingHistory {
  bookingId: string;
  source: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  busName: string;
  busNumber: string;
  totalAmount: number;
  status: string;
  seatNumbers: string[];
  passengerNames: string[];
  boardingPoint: string;
  droppingPoint: string;
  category: string; // 'Upcoming' | 'Completed' | 'Cancelled'
}
