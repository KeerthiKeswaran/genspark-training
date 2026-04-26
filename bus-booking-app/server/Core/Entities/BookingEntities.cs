using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using server.Core.Enums;

namespace server.Core.Entities
{
    [Index(nameof(JourneyId), nameof(SeatNumber), IsUnique = true)]
    public class SeatLock
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid JourneyId { get; set; }
        public Schedule? Journey { get; set; }
        
        [Required]
        public string SeatNumber { get; set; } = string.Empty;
        
        [Required]
        public Guid LockedByUserId { get; set; }
        public User? LockedByUser { get; set; }
        
        public DateTime ExpiresAt { get; set; }
    }

    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid CustomerId { get; set; }
        public User? Customer { get; set; }
        
        [Required]
        public Guid JourneyId { get; set; }
        public Schedule? Journey { get; set; }
        
        public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
        public decimal TotalAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Passenger>? Passengers { get; set; }
        public Payment? Payment { get; set; }
        
        public Guid? BoardingHubId { get; set; }
        public Hub? BoardingHub { get; set; }
        
        public Guid? DroppingHubId { get; set; }
        public Hub? DroppingHub { get; set; }
    }

    public class Passenger
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid BookingId { get; set; }
        public Booking? Booking { get; set; }
        
        [Required]
        public string SeatNumber { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public Gender Gender { get; set; }
    }

    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid BookingId { get; set; }
        public Booking? Booking { get; set; }
        
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime? ProcessedAt { get; set; }
    }
}
