using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using server.Core.Enums;

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
        public string? LayoutConfig { get; set; } // JSON string for grid layout
        public bool IsApproved { get; set; } = false;
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
        public string? RejectionReason { get; set; }
        
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
        public JourneyStatus Status { get; set; } = JourneyStatus.Scheduled;

        // Custom boarding and dropping points for this specific trip
        public List<Guid>? BoardingHubIds { get; set; } = new();
        public List<Guid>? DroppingHubIds { get; set; } = new();
    }
}
