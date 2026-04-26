export interface BusSearchResult {
  scheduleId: string;
  busNumber: string;
  busType: string;
  operatorName: string;
  operatorAddress: string;
  source: string;
  destination: string;
  departureTime: string;
  arrivalTime: string;
  price: number;
  availableSeats: number;
}

export interface SearchCriteria {
  from: string;
  to: string;
  date: string;
}
