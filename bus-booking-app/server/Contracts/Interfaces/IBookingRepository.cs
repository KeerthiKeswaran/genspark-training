using server.Core.Entities;
using server.Core.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.Contracts.Interfaces
{
    public interface IDbContextTransactionWrapper : IDisposable, IAsyncDisposable
    {
        Task CommitAsync();
        Task RollbackAsync();
    }

    public interface IBookingRepository
    {
        Task<Booking?> GetBookingByIdAsync(Guid bookingId);
        Task<Booking?> GetBookingWithJourneyAsync(Guid bookingId, Guid userId);
        Task<List<Booking>> GetBookingsByCustomerIdAsync(Guid customerId);
        Task AddBookingAsync(Booking booking);
        Task UpdateBookingAsync(Booking booking);
        Task SaveChangesAsync();

        // Related Entities CRUD & Operations
        Task AddPassengerAsync(Passenger passenger);
        Task AddPaymentAsync(Payment payment);
        Task RemoveSeatLocksAsync(List<SeatLock> locks);
        Task AddSeatLocksAsync(List<SeatLock> locks);
        Task<bool> IsSeatBookedAsync(Guid journeyId, string seatNumber);
        Task<bool> IsSeatLockedAsync(Guid journeyId, string seatNumber);
        Task<List<SeatLock>> GetExpiredLocksAsync(DateTime now);
        Task<List<SeatLock>> GetLocksForSeatsAsync(Guid journeyId, List<string> seatNumbers);
        Task<Payment?> GetPaymentByBookingIdAsync(Guid bookingId);
        Task<int> GetPassengerCountByBookingIdAsync(Guid bookingId);

        // Helper queries needed for business transactions
        Task<Schedule?> GetJourneyByIdAsync(Guid journeyId);
        Task<GlobalConfiguration?> GetGlobalConfigurationAsync();
        Task<City?> GetCityByNameAsync(string cityName);
        Task<List<Hub>> GetHubsByCityIdAndTypeAsync(Guid cityId, List<HubType> types);
        Task<List<Passenger>> GetConfirmedPassengersForJourneyAsync(Guid journeyId);
        Task<List<SeatLock>> GetActiveLocksForSeatsAsync(Guid journeyId, List<string> seatNumbers, Guid userId);
        Task<List<SeatLock>> GetActiveLocksForJourneyAsync(Guid journeyId);

        // Transaction management
        Task<IDbContextTransactionWrapper> BeginTransactionAsync();
    }
}
