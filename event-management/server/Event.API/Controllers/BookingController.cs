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
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IUserService _userService;

        public BookingController(IBookingService bookingService, IUserService userService)
        {
            _bookingService = bookingService;
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> BookTickets([FromBody] BookTicketsRequest request)
        {
            try
            {
                int attendeeId = _userService.GetCurrentUserId();
                var booking = await _bookingService.BookTicketsAsync(attendeeId, request.EventId, request.TierQuantities);
                if (booking == null)
                    return BadRequest(new { Message = "Booking failed. Capacity exceeded, event not live, or too many tickets requested." });

                return Ok(booking);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("{bookingId}/confirm")]
        public async Task<IActionResult> ConfirmBooking(int bookingId, [FromBody] ConfirmBookingRequest request)
        {
            var booking = await _bookingService.ConfirmBookingPaymentAsync(bookingId, request.StripeChargeId, request.PaymentMethod);
            if (booking == null)
                return BadRequest(new { Message = "Confirm payment failed. Booking not found or not in pending state." });

            return Ok(booking);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyBookings([FromQuery] string? status)
        {
            try
            {
                int attendeeId = _userService.GetCurrentUserId();
                var bookings = await _bookingService.GetMyBookingsAsync(attendeeId, status);
                return Ok(bookings);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("{bookingId}/cancel")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var success = await _bookingService.CancelBookingAsync(bookingId);
            if (!success)
                return BadRequest(new { Message = "Cancellation failed." });

            return Ok(new { Message = "Booking cancelled successfully." });
        }

        [HttpPost("{bookingId}/revert")]
        public async Task<IActionResult> RevertBooking(int bookingId)
        {
            try
            {
                var success = await _bookingService.RevertPendingBookingAsync(bookingId);
                if (!success)
                    return BadRequest(new { Message = "Booking revert failed." });

                return Ok(new { Message = "Booking reverted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
