using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class EventRepository : GenericRepository<Event.Models.Event>, IEventRepository
    {
        public EventRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<Event.Models.Event>> SearchEventsAsync(
            string? keyword, 
            string? category, 
            DateTime? minDateTime, 
            string? regionId, 
            int page, 
            int size)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(e => e.Title.Contains(keyword) || e.Description_Url.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var lowerCategory = category.ToLower();
                query = query.Where(e => e.Event_Type.ToLower() == lowerCategory);
            }

            if (minDateTime.HasValue)
            {
                query = query.Where(e => e.Date_Time >= minDateTime.Value);
            }

            if (!string.IsNullOrWhiteSpace(regionId))
            {
                query = query.Where(e => e.Venue != null && e.Venue.Region_Id == regionId);
            }

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.Event_Id)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new PagedResult<Event.Models.Event>(items, totalCount, page, size);
        }

        public async Task<Event.Models.Event?> GetEventDetailsAsync(int eventId)
        {
            return await _dbSet
                .Include(e => e.Venue)
                    .ThenInclude(v => v.SeatCapacities)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTiers)
                .Include(e => e.StaffAllocations)
                .FirstOrDefaultAsync(e => e.Event_Id == eventId);
        }

        public async Task<bool> ExistsAsync(int eventId)
        {
            return await _dbSet.AnyAsync(e => e.Event_Id == eventId);
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetExpiredEventsAsync(DateTime cutoffTime)
        {
            // Join with transaction or check creation time (since Event does not have Created_At, we can look up the pending transaction creation time or assume they expire if they are past the event start time or pending longer than cutoffTime). 
            // In our case, the Transaction table has Created_At and Related_Id. We can query events that are "Activation Pending" and have a transaction older than cutoffTime.
            return await _dbSet
                .Where(e => e.Status == "Activation Pending" && 
                            _context.Transactions.Any(t => t.Related_Id == e.Event_Id && 
                                                           t.Transaction_Type == "OrganizerUpfrontPayment" && 
                                                           t.Status == "Pending" && 
                                                           t.Created_At <= cutoffTime))
                .ToListAsync();
        }

        public async Task AddReportAsync(EventReport report)
        {
            await _context.EventReports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<EventReport>> GetAllReportsAsync()
        {
            return await _context.EventReports
                .Include(er => er.Event)
                .Include(er => er.Reporter)
                .ToListAsync();
        }

        public async Task<EventReport?> GetReportByIdAsync(int reportId)
        {
            return await _context.EventReports
                .Include(er => er.Event)
                    .ThenInclude(e => e.Organizer)
                .Include(er => er.Reporter)
                .FirstOrDefaultAsync(er => er.Report_Id == reportId);
        }

        public async Task UpdateReportAsync(EventReport report)
        {
            _context.EventReports.Update(report);
            await _context.SaveChangesAsync();
        }

        public async Task AddFeedbackAsync(EventFeedback feedback)
        {
            await _context.EventFeedbacks.AddAsync(feedback);
            await _context.SaveChangesAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetEventsByRegionsAsync(System.Collections.Generic.IEnumerable<string> regionIds)
        {
            return await _dbSet
                .Include(e => e.Venue)
                .Where(e => e.Status == "Live" && e.Venue != null && regionIds.Contains(e.Venue.Region_Id))
                .ToListAsync();
        }

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetLiveEventsWithDetailsAsync()
        {
            return await _dbSet
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.StaffAllocations)
                .Where(e => e.Status == "Live")
                .ToListAsync();
        }

        public async Task<PagedResult<Event.Models.Event>> GetEventsPagedAsync(
            string? keyword,
            string? eventType,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            int page,
            int size)
        {
            var query = _dbSet
                .Include(e => e.Venue)
                    .ThenInclude(v => v.SeatCapacities)
                .Include(e => e.Organizer)
                .Include(e => e.StaffAllocations)
                .AsQueryable();

            // Filter by keyword (title or description)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(lowerKeyword) || e.Description_Url.ToLower().Contains(lowerKeyword));
            }

            // Filter by event type
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                var lowerType = eventType.ToLower();
                query = query.Where(e => e.Event_Type.ToLower() == lowerType);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                var lowerStatus = status.ToLower();
                query = query.Where(e => e.Status.ToLower() == lowerStatus);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                query = query.Where(e => e.Date_Time >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(e => e.Date_Time <= endDate.Value);
            }

            // Sort
            if (string.Equals(sortBy, "date_desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(e => e.Date_Time);
            }
            else if (string.Equals(sortBy, "title_asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(e => e.Title);
            }
            else if (string.Equals(sortBy, "title_desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(e => e.Title);
            }
            else if (string.Equals(sortBy, "status_asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(e => e.Status);
            }
            else if (string.Equals(sortBy, "status_desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(e => e.Status);
            }
            else
            {
                // Default: earliest first (date_asc)
                query = query.OrderBy(e => e.Date_Time);
            }

            int totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new PagedResult<Event.Models.Event>(items, totalCount, page, size);
        }
    }
}
