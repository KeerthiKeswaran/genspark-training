using server.Features.Booking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.Contracts.Interfaces
{
    public interface IBookingService
    {
        Task<SeatLayoutDto?> GetLayoutAsync(Guid journeyId);
        Task<BookingResponse> ConfirmBookingAsync(Guid userId, ConfirmBookingRequest request);
        Task<List<BookingHistoryDto>> GetHistoryAsync(Guid userId);
    }
}
