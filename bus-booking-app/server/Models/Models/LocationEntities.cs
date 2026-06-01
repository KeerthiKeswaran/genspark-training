using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace server.Core.Entities
{
    public class City
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string State { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<Hub>? Hubs { get; set; }
    }

    public enum HubType
    {
        Boarding,
        Dropping,
        Both
    }

    public class Hub
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public Guid CityId { get; set; }
        
        [JsonIgnore]
        public City? City { get; set; }
        
        public HubType Type { get; set; } = HubType.Both;
        
        public Guid? OperatorId { get; set; }
        public BusOperator? Operator { get; set; }
    }
}
