using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Event.Contracts.IServices;
using Event.Models.DTOs;

namespace Event.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IUserService _userService;

        public EventController(IEventService eventService, IUserService userService)
        {
            _eventService = eventService;
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> BrowseEvents(
            [FromQuery] string? keyword,
            [FromQuery] string? category,
            [FromQuery] DateTime? minDateTime,
            [FromQuery] string? regionId,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            var result = await _eventService.BrowseEventsAsync(keyword, category, minDateTime, regionId, page, size);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{eventId}")]
        public async Task<IActionResult> GetEventDetails(int eventId)
        {
            var ev = await _eventService.GetEventDetailsAsync(eventId);
            if (ev == null)
                return NotFound(new { Message = "Event not found." });

            return Ok(ev);
        }

        [HttpGet("recommended")]
        public async Task<IActionResult> GetRecommendedEvents()
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var events = await _eventService.GetEventsByInterestedRegionsAsync(userId);
                return Ok(events);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Event.Business.Exceptions.NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/report")]
        public async Task<IActionResult> ReportEvent(int eventId, [FromBody] ReportEventRequest request)
        {
            try
            {
                int reporterId = _userService.GetCurrentUserId();
                var success = await _eventService.ReportEventAsync(reporterId, eventId, request.Reason);
                if (!success)
                    return BadRequest(new { Message = "Failed to report event." });

                return Ok(new { Message = "Event reported successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/feedback")]
        public async Task<IActionResult> SubmitFeedback(int eventId, [FromBody] SubmitFeedbackRequest request)
        {
            try
            {
                int attendeeId = _userService.GetCurrentUserId();
                var success = await _eventService.SubmitEventFeedbackAsync(attendeeId, eventId, request.Rating, request.Review);
                if (!success)
                    return BadRequest(new { Message = "Failed to submit feedback." });

                return Ok(new { Message = "Feedback submitted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("verify-ticket")]
        public async Task<IActionResult> VerifyTicket([FromBody] VerifyTicketRequest request)
        {
            try
            {
                var booking = await _eventService.VerifyTicketCheckInAsync(request.Hash);
                return Ok(new
                {
                    Message = "Ticket verified and checked in successfully.",
                    BookingId = booking.Booking_Id,
                    AttendeeId = booking.Attendee_Id,
                    EventId = booking.Event_Id,
                    CheckInStatus = booking.CheckIn_Status,
                    BookingStatus = booking.Booking_Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            try
            {
                int organizerId = _userService.GetCurrentUserId();
                var createdEvent = await _eventService.CreateEventAsync(organizerId, request);
                return CreatedAtAction(nameof(GetEventDetails), new { eventId = createdEvent.Event_Id }, createdEvent);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("check-staff")]
        public async Task<IActionResult> CheckStaffAvailability([FromBody] CheckStaffAvailabilityRequest request)
        {
            try
            {
                var response = await _eventService.CheckStaffAvailabilityAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/confirm")]
        public async Task<IActionResult> ConfirmEvent(int eventId, [FromBody] ConfirmBookingRequest request)
        {
            try
            {
                var ev = await _eventService.ConfirmEventUpfrontPaymentAsync(eventId, request.StripeChargeId, request.PaymentMethod);
                return Ok(new
                {
                    Message = "Event upfront payment confirmed. Event is now Live.",
                    Event = ev
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/cancel")]
        public async Task<IActionResult> CancelEvent(int eventId)
        {
            try
            {
                var success = await _eventService.CancelEventAsync(eventId);
                if (!success)
                    return BadRequest(new { Message = "Event cancellation failed." });

                return Ok(new { Message = "Event cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{eventId}/revert")]
        public async Task<IActionResult> RevertEvent(int eventId)
        {
            try
            {
                var success = await _eventService.RevertPendingEventCreationAsync(eventId);
                if (!success)
                    return BadRequest(new { Message = "Event revert failed." });

                return Ok(new { Message = "Event creation reverted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
