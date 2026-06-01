using server.Core.Enums;

namespace server.Features.Booking
{
    public class SeatLayoutDto
    {
        public string? LayoutConfig { get; set; }
        public List<SeatStatusDto> UnavailableSeats { get; set; } = new();
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public string BusName { get; set; } = string.Empty;
        public string PlatformFeeType { get; set; } = "Fixed";
        public decimal PlatformFeeValue { get; set; } = 50.00m;
        
        public List<HubStatusDto> BoardingHubs { get; set; } = new();
        public List<HubStatusDto> DroppingHubs { get; set; } = new();
    }

    public class HubStatusDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class SeatStatusDto
    {
        public string SeatNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Booked" or "Blocked"
        public string? Gender { get; set; } // "M" or "F"
    }

    public class LockSeatsRequest
    {
        public Guid JourneyId { get; set; }
        public List<string> SeatNumbers { get; set; } = new();
    }

    public class LockSeatsResponse
    {
        public Guid LockId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class ConfirmBookingRequest
    {
        public Guid JourneyId { get; set; }
        public List<PassengerDto> Passengers { get; set; } = new();
        public string? PaymentToken { get; set; } // Dummy token
        public Guid BoardingPointId { get; set; }
        public Guid DroppingPointId { get; set; }
    }

    public class PassengerDto
    {
        public string SeatNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public Gender Gender { get; set; }
    }

    public class BookingResponse
    {
        public Guid BookingId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BookingHistoryDto
    {
        public Guid BookingId { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> SeatNumbers { get; set; } = new();
        public List<string> PassengerNames { get; set; } = new();
        
        public string BoardingPoint { get; set; } = string.Empty;
        public string DroppingPoint { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // "Upcoming", "Completed", or "Cancelled"
    }
}
