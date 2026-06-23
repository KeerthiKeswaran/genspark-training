// Booking & Ticket Models

export interface TicketTierSelection {
  tierName: string;
  price: number;
  quantity: number;
  totalSeats: number;
  availableSeats: number;
}

export interface BookingDetail {
  tier_Name: string;
  quantity: number;
  price: number;
}

export interface BookingModel {
  booking_Id: number;
  attendee_Id: number;
  event_Id: number;
  event_Title: string;
  event_Type: 'Physical' | 'Virtual' | 'Hybrid';
  event_Date_Time: string;
  event_Image_Url?: string;
  event_Venue?: string;
  event_Region?: string;
  booking_Status: 'Pending' | 'Confirmed' | 'Cancelled';
  qr_Code_Data?: string;  // base64 QR code
  checkIn_Status: 'Pending' | 'CheckedIn' | 'Missed';
  created_At: string;
  virtual_Url?: string;
  total_Amount: number;
  details: BookingDetail[];
}

export interface InitiateBookingRequest {
  eventId: number;
  tierQuantities: Record<string, number>;
}

export interface ConfirmBookingRequest {
  stripeChargeId: string;
  paymentMethod: string;
}

export interface BookingStep {
  id: number;
  label: string;
  completed: boolean;
  active: boolean;
}
