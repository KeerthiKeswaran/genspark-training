using Microsoft.EntityFrameworkCore;
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
    public class OperatorRepository : IOperatorRepository
    {
        private readonly AppDbContext _context;

        public OperatorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BusOperator?> GetOperatorByIdAsync(Guid operatorId)
        {
            return await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == operatorId);
        }

        public async Task<List<Schedule>> GetFutureSchedulesByOperatorIdAsync(Guid operatorId)
        {
            return await _context.Schedules
                .Include(s => s.Bus)
                .Where(s => s.Bus!.OperatorId == operatorId && s.DepartureTime > DateTime.UtcNow && s.Status != JourneyStatus.Cancelled)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetConfirmedBookingsByScheduleIdAsync(Guid scheduleId)
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Payment)
                .Where(b => b.JourneyId == scheduleId && b.Status == BookingStatus.Confirmed)
                .ToListAsync();
        }

        public Task UpdateOperatorAsync(BusOperator op)
        {
            _context.BusOperators.Update(op);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
