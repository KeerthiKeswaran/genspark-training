using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class UserProfileResponse
    {
        public int User_Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile_Number { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> InterestedRegions { get; set; } = new List<string>();
    }
}
