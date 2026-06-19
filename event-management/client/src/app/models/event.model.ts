export interface TicketTier {
  tierName: string;
  price: number;
  totalSeats?: number;
}

export interface EventModel {
  event_Id: number;
  eventType: 'Physical' | 'Virtual' | 'Hybrid';
  title: string;
  descriptionUrl?: string;
  imageUrl?: string;
  dateTime: string;
  durationHours: number;
  requiresStaff?: boolean;
  venueId?: number;
  regionId?: string;
  hasAcceptedPolicy?: boolean;
  ticketTiers?: TicketTier[];
  organizerId?: number;
  status?: 'Pending' | 'Live' | 'Cancelled';
}

export interface BrowsedEventResponse {
  event_Id: number;
  eventType: string;
  title: string;
  descriptionUrl?: string;
  imageUrl?: string;
  dateTime: string;
  durationHours: number;
  venue_Name?: string;
  region_Id?: string;
  region_Name?: string;
  status?: string;
  minPrice?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  size: number;
  totalPages: number;
}
