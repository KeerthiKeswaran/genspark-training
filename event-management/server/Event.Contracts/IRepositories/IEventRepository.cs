using System;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IEventRepository : IGenericRepository<Event.Models.Event>
    {
        Task<PagedResult<Event.Models.Event>> SearchEventsAsync(string? keyword, string? category, DateTime? minDateTime, string? regionId, int page, int size);
        Task<Event.Models.Event?> GetEventDetailsAsync(int eventId);
        Task<bool> ExistsAsync(int eventId);
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetExpiredEventsAsync(DateTime cutoffTime);
        Task AddReportAsync(EventReport report);
        Task<System.Collections.Generic.IEnumerable<EventReport>> GetAllReportsAsync();
        Task<EventReport?> GetReportByIdAsync(int reportId);
        Task UpdateReportAsync(EventReport report);
        Task AddFeedbackAsync(EventFeedback feedback);
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetEventsByRegionsAsync(System.Collections.Generic.IEnumerable<string> regionIds);
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetLiveEventsWithDetailsAsync();
        Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetEventsByOrganizerAsync(int organizerId);
        Task<PagedResult<Event.Models.Event>> GetEventsPagedAsync(string? keyword, string? eventType, string? status, DateTime? startDate, DateTime? endDate, string? sortBy, int page, int size);
    }
}
