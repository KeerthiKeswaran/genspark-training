using System;
using System.Threading.Tasks;
using Event.Models;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IEventService
    {
        Task<PagedResult<BrowsedEventResponse>> BrowseEventsAsync(string? keyword, string? category, DateTime? minDateTime, string? regionId, int page, int size);
        Task<EventDetailsResponse?> GetEventDetailsAsync(int eventId);
        Task<bool> ReportEventAsync(int reporterId, int eventId, string reason);
        Task<bool> SubmitEventFeedbackAsync(int attendeeId, int eventId, int rating, string review);
        Task<Booking> VerifyTicketCheckInAsync(string secretHash);
        Task<EventDetailsResponse> CreateEventAsync(int organizerId, Event.Models.DTOs.CreateEventRequest request);
        Task<Event.Models.DTOs.StaffAvailabilityResponse> CheckStaffAvailabilityAsync(Event.Models.DTOs.CheckStaffAvailabilityRequest request);
        Task<EventDetailsResponse> ConfirmEventUpfrontPaymentAsync(int eventId, string stripeChargeId, string paymentMethod);
        Task<(bool Success, string SessionId, string SessionUrl, string ErrorMessage)> CreateCheckoutSessionForEventCreationAsync(int eventId, string successUrl, string cancelUrl);
        Task<Event.Models.DTOs.PlatformSettingsResponse?> GetPlatformSettingsAsync();
        Task<string> SaveDescriptionFileAsync(string text);
        Task<string> SaveImageFileAsync(string fileName, byte[] fileBytes);
        Task ReleaseExpiredEventCreationAsync();
        Task<bool> CancelEventAsync(int eventId, string refundType = "Dynamic", string cancellationMessage = "We regret to inform you that the event you booked has been cancelled by the organizer.");
        Task<bool> RevertPendingEventCreationAsync(int eventId);
        Task<System.Collections.Generic.IEnumerable<BrowsedEventResponse>> GetEventsByInterestedRegionsAsync(int userId);
        Task<System.Collections.Generic.IEnumerable<RegionResponse>> GetPopularRegionsAsync(int? rankNumber);
        Task<System.Collections.Generic.IEnumerable<BrowsedEventResponse>> GetTrendingEventsAsync(int? count);
        Task<System.Collections.Generic.IEnumerable<BrowsedEventResponse>> GetPopularEventsInCommonAsync(int regionsLimit);
        Task<System.Collections.Generic.IEnumerable<TicketTierCapacityResponse>> GetEventTicketTierCapacitiesAsync(int eventId);
        Task ReleaseCompletedEventsAsync();
        Task ProcessDismissedPayoutsAsync();
    }
}
