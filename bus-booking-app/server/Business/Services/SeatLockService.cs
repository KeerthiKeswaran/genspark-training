using server.Core.Entities;
using server.Contracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.Infrastructure.Services
{
    public class SeatLockService : ISeatLockService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly int _lockDurationMinutes = 5;

        public SeatLockService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<bool> IsSeatAvailable(Guid journeyId, string seatNumber)
        {
            // Check if booked
            var isBooked = await _bookingRepository.IsSeatBookedAsync(journeyId, seatNumber);
            if (isBooked) return false;

            // Check if locked
            var isLocked = await _bookingRepository.IsSeatLockedAsync(journeyId, seatNumber);
            return !isLocked;
        }

        public async Task<SeatLock?> LockSeats(Guid journeyId, List<string> seatNumbers, Guid userId)
        {
            using var transaction = await _bookingRepository.BeginTransactionAsync();
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

                await _bookingRepository.AddSeatLocksAsync(locks);
                await _bookingRepository.SaveChangesAsync();
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
            var expiredLocks = await _bookingRepository.GetExpiredLocksAsync(now);

            if (expiredLocks.Any())
            {
                await _bookingRepository.RemoveSeatLocksAsync(expiredLocks);
                await _bookingRepository.SaveChangesAsync();
            }
        }

        public async Task ReleaseLocks(Guid journeyId, List<string> seatNumbers)
        {
            var locks = await _bookingRepository.GetLocksForSeatsAsync(journeyId, seatNumbers);

            if (locks.Any())
            {
                await _bookingRepository.RemoveSeatLocksAsync(locks);
                await _bookingRepository.SaveChangesAsync();
            }
        }
    }
}
