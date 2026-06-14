using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IServices
{
    public interface IBookingService
    {
        Task<Booking?> BookTicketsAsync(int attendeeId, int eventId, Dictionary<string, int> tierQuantities);
        Task<Booking?> ConfirmBookingPaymentAsync(int bookingId, string stripeChargeId, string paymentMethod);
        Task<IEnumerable<Booking>> GetMyBookingsAsync(int attendeeId);
        Task<bool> CancelBookingAsync(int bookingId, string refundType = "Dynamic");
        Task ReleaseExpiredEventBookingAsync();
        Task<bool> RevertPendingBookingAsync(int bookingId);
    }
}
