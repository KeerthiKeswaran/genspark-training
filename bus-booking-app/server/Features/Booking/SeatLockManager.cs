using Microsoft.EntityFrameworkCore;
using server.Core.Entities;
using server.Infrastructure.Data;

namespace server.Features.Booking
{
    public interface ISeatLockManager
    {
        Task<bool> IsSeatAvailable(Guid journeyId, string seatNumber);
        Task<SeatLock?> LockSeats(Guid journeyId, List<string> seatNumbers, Guid userId);
        Task ReleaseExpiredLocks();
        Task ReleaseLocks(Guid journeyId, List<string> seatNumbers);
    }

    public class SeatLockManager : ISeatLockManager
    {
        private readonly AppDbContext _context;
        private readonly int _lockDurationMinutes = 5;

        public SeatLockManager(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsSeatAvailable(Guid journeyId, string seatNumber)
        {
            // Check if booked
            var isBooked = await _context.Passengers
                .AnyAsync(p => p.Booking!.JourneyId == journeyId && p.SeatNumber == seatNumber && p.Booking.Status == Core.Enums.BookingStatus.Confirmed);

            if (isBooked) return false;

            // Check if locked
            var isLocked = await _context.SeatLocks
                .AnyAsync(l => l.JourneyId == journeyId && l.SeatNumber == seatNumber && l.ExpiresAt > DateTime.UtcNow);

            return !isLocked;
        }

        public async Task<SeatLock?> LockSeats(Guid journeyId, List<string> seatNumbers, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check all seats availability
                foreach (var seatNumber in seatNumbers)
                {
                    if (!await IsSeatAvailable(journeyId, seatNumber))
                    {
                        return null; // One of the seats is taken
                    }
                }

                var expiresAt = DateTime.UtcNow.AddMinutes(_lockDurationMinutes);
                var locks = seatNumbers.Select(s => new SeatLock
                {
                    JourneyId = journeyId,
                    SeatNumber = s,
                    LockedByUserId = userId,
                    ExpiresAt = expiresAt
                }).ToList();

                _context.SeatLocks.AddRange(locks);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return locks.First(); // Return one of the locks as reference
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ReleaseExpiredLocks()
        {
            var now = DateTime.UtcNow;
            var expiredLocks = await _context.SeatLocks
                .Where(l => l.ExpiresAt <= now)
                .ToListAsync();

            if (expiredLocks.Any())
            {
                _context.SeatLocks.RemoveRange(expiredLocks);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReleaseLocks(Guid journeyId, List<string> seatNumbers)
        {
            var locks = await _context.SeatLocks
                .Where(l => l.JourneyId == journeyId && seatNumbers.Contains(l.SeatNumber))
                .ToListAsync();

            if (locks.Any())
            {
                _context.SeatLocks.RemoveRange(locks);
                await _context.SaveChangesAsync();
            }
        }
    }
}
