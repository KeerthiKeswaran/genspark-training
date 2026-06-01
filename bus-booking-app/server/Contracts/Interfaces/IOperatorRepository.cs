using server.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace server.Contracts.Interfaces
{
    public interface IOperatorRepository
    {
        Task<BusOperator?> GetOperatorByIdAsync(Guid operatorId);
        Task<List<Schedule>> GetFutureSchedulesByOperatorIdAsync(Guid operatorId);
        Task<List<Booking>> GetConfirmedBookingsByScheduleIdAsync(Guid scheduleId);
        Task UpdateOperatorAsync(BusOperator op);
        Task SaveChangesAsync();
    }
}
