using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace server.Core.Entities
{
    [Index(nameof(Source))]
    [Index(nameof(Destination))]
    public class BusRoute
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Source { get; set; } = string.Empty;
        [Required] public string Destination { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
    }

    public class Bus
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string BusNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = "AC Sleeper"; 
        public int TotalSeats { get; set; }
        
        public Guid OperatorId { get; set; }
        public BusOperator? Operator { get; set; }

        public Guid? AssignedRouteId { get; set; }
        public BusRoute? AssignedRoute { get; set; }
    }

    [Index(nameof(DepartureTime))]
    public class Schedule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid BusId { get; set; }
        public Bus? Bus { get; set; }
        
        public Guid RouteId { get; set; }
        public BusRoute? Route { get; set; }
        
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
    }
}
