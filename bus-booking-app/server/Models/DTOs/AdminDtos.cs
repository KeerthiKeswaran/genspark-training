using server.Core.Enums;

namespace server.Features.Administration
{
    public class CityDto
    {
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public class HubDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid CityId { get; set; }
        public string Type { get; set; } = "Both"; // "Boarding", "Dropping", "Both"
    }

    public class FeeSettingDto
    {
        public string FeeType { get; set; } = "Fixed"; // "Fixed" or "Percentage"
        public decimal FeeValue { get; set; } = 50.00m;
        public decimal CommissionPercentage { get; set; } = 10.00m;
    }

    public class OperatorResponseDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public ApprovalStatus Status { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminStatsDto
    {
        public int TotalBookings { get; set; }
        public int UpcomingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal GrossBookingRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal OperatorPayout { get; set; }
        public int ActiveOperators { get; set; }
        public int TotalCities { get; set; }
        public List<AdminTripDto> RecentTrips { get; set; } = new();
    }

    public class AdminTripDto
    {
        public Guid ScheduleId { get; set; }
        public string Route { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public DateTime Departure { get; set; }
        public string Status { get; set; } = string.Empty;
        public int BookedSeats { get; set; }
        public int CancelledSeats { get; set; }
        public int MaxSeats { get; set; }
        public decimal Price { get; set; }
    }

    public class BusApprovalDto
    {
        public Guid Id { get; set; }
        public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public string OperatorName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public ApprovalStatus Status { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class RouteRequestDto
    {
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
    }

    public class RouteResponseDto
    {
        public Guid Id { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
    }

    public class RejectionRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
