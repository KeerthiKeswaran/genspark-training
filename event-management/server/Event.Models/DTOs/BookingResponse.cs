using System;
using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class BookingResponse
    {
        public int Booking_Id { get; set; }
        public int Attendee_Id { get; set; }
        public int Event_Id { get; set; }
        public string Event_Title { get; set; } = string.Empty;
        public string Event_Type { get; set; } = string.Empty;
        public DateTime Event_Date_Time { get; set; }
        public string Booking_Status { get; set; } = string.Empty;
        public string? Qr_Code_Path { get; set; }
        public string CheckIn_Status { get; set; } = string.Empty;
        public DateTime Created_At { get; set; }
        public string? Virtual_Url { get; set; }
        public string? Virtual_Password_Hash { get; set; }
        public List<BookingDetailDto> Details { get; set; } = new List<BookingDetailDto>();
    }

    public class BookingDetailDto
    {
        public string Tier_Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
