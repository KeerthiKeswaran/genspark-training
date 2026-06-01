using System.ComponentModel.DataAnnotations;

namespace server.Core.Entities
{
    public class GlobalConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string PlatformFeeType { get; set; } = "Fixed"; // "Fixed" or "Percentage"
        
        public decimal PlatformFeeValue { get; set; } = 50.00m;
        
        public decimal OperatorCommissionPercentage { get; set; } = 10.00m;
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
