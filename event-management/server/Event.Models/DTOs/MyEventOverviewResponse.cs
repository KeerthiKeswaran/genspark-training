using System;

namespace Event.Models.DTOs
{
    public class MyEventOverviewResponse
    {
        public int Event_Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Event_Type { get; set; } = string.Empty;
        public DateTime Date_Time { get; set; }
        public decimal Duration_Hours { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Venue_Name { get; set; }
    }
}
