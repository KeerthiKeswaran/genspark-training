using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IAdminService
    {
        Task<AdminDashboardStatsDto> GetDashboardStatsAsync();
        Task<PagedResult<EventDetailEto>> GetEventsPagedAsync(string? keyword, string? eventType, string? status, DateTime? startDate, DateTime? endDate, string? sortBy, int page, int size);
        Task<IEnumerable<SupportTicket>> GetSupportTicketsAsync();
        Task<bool> RespondToTicketAsync(int ticketId, string responseText);
        Task<bool> EscalateTicketAsync(int ticketId, string adminId, EscalateTicketRequest request);
        Task<object> GetFlaggedEventsReportsAsync();
        Task<bool> DismissEventReportAsync(int reportId);
        Task<bool> UpholdEventReportAsync(int reportId, string adminId, string actionReason, string organizerAction);

        Task<IEnumerable<RegionResponse>> GetAllRegionsAsync();
        Task<IEnumerable<VenueResponse>> GetAllVenuesAsync();
        Task<VenueResponse> CreateVenueAsync(CreateVenueRequest request);

        Task<IEnumerable<StaffResponse>> GetStaffDirectoryAsync();
        Task<bool> AllocateStaffToEventAsync(int eventId, int employeeId);
    }
}
