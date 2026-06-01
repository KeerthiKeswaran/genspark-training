using server.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.Contracts.Interfaces
{
    public interface ISeatLockService
    {
        Task<bool> IsSeatAvailable(Guid journeyId, string seatNumber);
        Task<SeatLock?> LockSeats(Guid journeyId, List<string> seatNumbers, Guid userId);
        Task ReleaseExpiredLocks();
        Task ReleaseLocks(Guid journeyId, List<string> seatNumbers);
    }
}
