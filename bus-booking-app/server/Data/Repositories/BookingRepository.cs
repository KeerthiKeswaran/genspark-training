using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using server.Core.Entities;
using server.Core.Enums;
using server.Contracts.Interfaces;
using server.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.Infrastructure.Repositories
{
    public class DbContextTransactionWrapper : IDbContextTransactionWrapper
    {
        private readonly IDbContextTransaction _transaction;

        public DbContextTransactionWrapper(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync() => _transaction.CommitAsync();

        public Task RollbackAsync() => _transaction.RollbackAsync();

        public void Dispose() => _transaction.Dispose();

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }

    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
        {
            return await _context.Bookings.FindAsync(bookingId);
        }

        public async Task<Booking?> GetBookingWithJourneyAsync(Guid bookingId, Guid userId)
        {
            return await _context.Bookings
                .Include(b => b.Journey)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == userId);
        }

        public async Task<List<Booking>> GetBookingsByCustomerIdAsync(Guid customerId)
        {
            return await _context.Bookings
                .Include(b => b.Journey)
                    .ThenInclude(j => j!.Route)
                .Include(b => b.Journey)
                    .ThenInclude(j => j!.Bus)
                        .ThenInclude(bus => bus!.Operator)
                .Include(b => b.Passengers)
                .Include(b => b.BoardingHub)
                .Include(b => b.DroppingHub)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task AddBookingAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
        }

        public Task UpdateBookingAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            return Task.CompletedTask;
        }

        public async Task AddPassengerAsync(Passenger passenger)
        {
            await _context.Passengers.AddAsync(passenger);
        }

        public async Task AddPaymentAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public Task RemoveSeatLocksAsync(List<SeatLock> locks)
        {
            _context.SeatLocks.RemoveRange(locks);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Schedule?> GetJourneyByIdAsync(Guid journeyId)
        {
            return await _context.Schedules
                .Include(s => s.Bus)
                    .ThenInclude(b => b!.Operator)
                .Include(s => s.Route)
                .FirstOrDefaultAsync(s => s.Id == journeyId);
        }

        public async Task<GlobalConfiguration?> GetGlobalConfigurationAsync()
        {
            return await _context.GlobalConfigurations.FirstOrDefaultAsync();
        }

        public async Task<City?> GetCityByNameAsync(string cityName)
        {
            return await _context.Cities.FirstOrDefaultAsync(c => c.Name == cityName);
        }

        public async Task<List<Hub>> GetHubsByCityIdAndTypeAsync(Guid cityId, List<HubType> types)
        {
            return await _context.Hubs
                .Where(h => h.CityId == cityId && types.Contains(h.Type))
                .ToListAsync();
        }

        public async Task<List<Passenger>> GetConfirmedPassengersForJourneyAsync(Guid journeyId)
        {
            return await _context.Passengers
                .Where(p => p.Booking!.JourneyId == journeyId && p.Booking.Status == BookingStatus.Confirmed)
                .ToListAsync();
        }

        public async Task<List<SeatLock>> GetActiveLocksForSeatsAsync(Guid journeyId, List<string> seatNumbers, Guid userId)
        {
            return await _context.SeatLocks
                .Where(l => l.JourneyId == journeyId && seatNumbers.Contains(l.SeatNumber) && l.LockedByUserId == userId && l.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<List<SeatLock>> GetActiveLocksForJourneyAsync(Guid journeyId)
        {
            return await _context.SeatLocks
                .Where(l => l.JourneyId == journeyId && l.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<IDbContextTransactionWrapper> BeginTransactionAsync()
        {
            var tx = await _context.Database.BeginTransactionAsync();
            return new DbContextTransactionWrapper(tx);
        }

        public async Task AddSeatLocksAsync(List<SeatLock> locks)
        {
            await _context.SeatLocks.AddRangeAsync(locks);
        }

        public async Task<bool> IsSeatBookedAsync(Guid journeyId, string seatNumber)
        {
            return await _context.Passengers
                .AnyAsync(p => p.Booking!.JourneyId == journeyId && p.SeatNumber == seatNumber && p.Booking.Status == BookingStatus.Confirmed);
        }

        public async Task<bool> IsSeatLockedAsync(Guid journeyId, string seatNumber)
        {
            return await _context.SeatLocks
                .AnyAsync(l => l.JourneyId == journeyId && l.SeatNumber == seatNumber && l.ExpiresAt > DateTime.UtcNow);
        }

        public async Task<List<SeatLock>> GetExpiredLocksAsync(DateTime now)
        {
            return await _context.SeatLocks
                .Where(l => l.ExpiresAt <= now)
                .ToListAsync();
        }

        public async Task<List<SeatLock>> GetLocksForSeatsAsync(Guid journeyId, List<string> seatNumbers)
        {
            return await _context.SeatLocks
                .Where(l => l.JourneyId == journeyId && seatNumbers.Contains(l.SeatNumber))
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByBookingIdAsync(Guid bookingId)
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
        }

        public async Task<int> GetPassengerCountByBookingIdAsync(Guid bookingId)
        {
            return await _context.Passengers.CountAsync(p => p.BookingId == bookingId);
        }
    }
}
