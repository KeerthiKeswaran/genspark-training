using server.Core.Enums;
using server.Contracts.Interfaces;
using System;
using System.Threading.Tasks;

namespace server.Features.Booking
{
    public class CancellationService : ICancellationService
    {
        private readonly IBookingRepository _bookingRepository;

        public CancellationService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<(bool Success, string Message)> CancelBooking(Guid bookingId, Guid userId)
        {
            var booking = await _bookingRepository.GetBookingWithJourneyAsync(bookingId, userId);

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
            var payment = await _bookingRepository.GetPaymentByBookingIdAsync(bookingId);
            if (payment != null && payment.Status == PaymentStatus.Success)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.ProcessedAt = DateTime.UtcNow;
            }

            // Release seats (update AvailableSeats count)
            var passengerCount = await _bookingRepository.GetPassengerCountByBookingIdAsync(bookingId);
            booking.Journey.AvailableSeats += passengerCount;

            await _bookingRepository.SaveChangesAsync();

            return (true, "Booking cancelled successfully. Refund initiated.");
        }
    }
}
