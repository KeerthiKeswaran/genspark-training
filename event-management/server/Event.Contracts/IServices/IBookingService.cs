using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IBookingService
    {
        Task<BookingResponse?> BookTicketsAsync(int attendeeId, int eventId, Dictionary<string, int> tierQuantities);
        Task<BookingResponse?> ConfirmBookingPaymentAsync(int bookingId, string stripeChargeId, string paymentMethod);
        Task<IEnumerable<BookingResponse>> GetMyBookingsAsync(int attendeeId);
        Task<bool> CancelBookingAsync(int bookingId, string refundType = "Dynamic");
        Task ReleaseExpiredEventBookingAsync();
        Task<bool> RevertPendingBookingAsync(int bookingId);
    }
}
