using System;
using System.Threading.Tasks;

namespace server.Contracts.Interfaces
{
    public interface ICancellationService
    {
        Task<(bool Success, string Message)> CancelBooking(Guid bookingId, Guid userId);
    }
}
