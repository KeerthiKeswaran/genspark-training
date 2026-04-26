using Microsoft.EntityFrameworkCore;
using server.Core.Enums;
using server.Infrastructure.Data;

namespace server.Features.Booking
{
    public interface ICancellationService
    {
        Task<(bool Success, string Message)> CancelBooking(Guid bookingId, Guid userId);
    }

    public class CancellationService : ICancellationService
    {
        private readonly AppDbContext _context;

        public CancellationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> CancelBooking(Guid bookingId, Guid userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Journey)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);

            if (booking == null)
            {
                return (false, "Booking not found.");
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return (false, "Booking is already cancelled.");
            }

            var timeToDeparture = booking.Journey!.DepartureTime - DateTime.UtcNow;

            if (timeToDeparture.TotalHours < 24)
            {
                return (false, "Cancellations are only allowed up to 24 hours before departure.");
            }

            // Perform cancellation
            booking.Status = BookingStatus.Cancelled;
            
            // Refund logic (Dummy)
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
            if (payment != null && payment.Status == PaymentStatus.Success)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.ProcessedAt = DateTime.UtcNow;
            }

            // Release seats (update AvailableSeats count)
            var passengerCount = await _context.Passengers.CountAsync(p => p.BookingId == bookingId);
            booking.Journey.AvailableSeats += passengerCount;

            await _context.SaveChangesAsync();

            return (true, "Booking cancelled successfully. Refund initiated.");
        }
    }
}
