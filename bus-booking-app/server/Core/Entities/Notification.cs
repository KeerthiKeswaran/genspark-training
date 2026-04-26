using System;
using System.ComponentModel.DataAnnotations;

namespace server.Core.Entities
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsRead { get; set; } = false;
        
        // Recipient
        public Guid UserId { get; set; }
        
        // For identifying the type of notification (Optional)
        public string Type { get; set; } = "General"; 
        
        // Link to a specific entity (Optional, e.g., trip ID or operator ID)
        public string? RelatedEntityId { get; set; }
    }
}
