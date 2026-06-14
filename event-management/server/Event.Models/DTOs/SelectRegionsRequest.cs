using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class SelectRegionsRequest
    {
        public List<string> RegionIds { get; set; } = new List<string>();
    }
}
