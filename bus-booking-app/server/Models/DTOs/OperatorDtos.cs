using System;
using System.Collections.Generic;

namespace server.Features.Operator
{
    public class OperatorStatsDto
    {
        public int TotalBuses { get; set; }
        public int ActiveSchedules { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<OperatorTripDto> RecentTrips { get; set; } = new();
    }

    public class OperatorTripDto
    {
        public Guid ScheduleId { get; set; }
        public string Route { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public DateTime Departure { get; set; }
        public string Status { get; set; } = string.Empty;
        public int BookedSeats { get; set; }
        public int MaxSeats { get; set; }
        public decimal Revenue { get; set; }
    }

    public class BusRequestDto
    {
        public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = "AC Sleeper";
        public int TotalSeats { get; set; }
        public string? LayoutConfig { get; set; }
    }

    public class ScheduleRequestDto
    {
        public Guid BusId { get; set; }
        public Guid RouteId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public List<Guid> BoardingHubIds { get; set; } = new();
        public List<Guid> DroppingHubIds { get; set; } = new();
    }

    public class HubRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid CityId { get; set; }
        public string Type { get; set; } = "Both";
    }
}
