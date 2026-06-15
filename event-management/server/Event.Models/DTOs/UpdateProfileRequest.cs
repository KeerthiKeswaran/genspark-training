using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class UpdateProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
    }
}
