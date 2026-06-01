using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Contracts.Interfaces;
using server.Business.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace server.Features.Booking
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ISeatLockService _seatLockService;
        private readonly ICancellationService _cancellationService;

        public BookingController(
            IBookingService bookingService,
            ISeatLockService seatLockService,
            ICancellationService cancellationService)
        {
            _bookingService = bookingService;
            _seatLockService = seatLockService;
            _cancellationService = cancellationService;
        }

        [HttpGet("layout/{journeyId}")]
        public async Task<ActionResult<SeatLayoutDto>> GetLayout(Guid journeyId)
        {
            var layout = await _bookingService.GetLayoutAsync(journeyId);
            if (layout == null) return NotFound("Journey not found.");
            return Ok(layout);
        }

        [HttpPost("lock-seats")]
        public async Task<ActionResult<LockSeatsResponse>> LockSeats([FromBody] LockSeatsRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var lockObj = await _seatLockService.LockSeats(request.JourneyId, request.SeatNumbers, userId);
            
            if (lockObj == null)
            {
                return Conflict(new { message = "One or more seats are already taken." });
            }

            return Ok(new LockSeatsResponse
            {
                LockId = lockObj.Id,
                ExpiresAt = lockObj.ExpiresAt
            });
        }

        [HttpPost("confirm")]
        public async Task<ActionResult<BookingResponse>> ConfirmBooking([FromBody] ConfirmBookingRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var response = await _bookingService.ConfirmBookingAsync(userId, request);
                return Ok(response);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BookingValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Internal Server Error: {ex.Message} - {ex.InnerException?.Message}" });
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<BookingHistoryDto>>> GetHistory()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var history = await _bookingService.GetHistoryAsync(userId);
            return Ok(history);
        }

        [HttpDelete("release-locks")]
        public async Task<ActionResult> ReleaseLocks([FromQuery] Guid journeyId, [FromQuery] string seats)
        {
            var seatNumbers = seats.Split(',').ToList();
            await _seatLockService.ReleaseLocks(journeyId, seatNumbers);
            return Ok();
        }

        [HttpDelete("{bookingId}")]
        public async Task<ActionResult> CancelBooking(Guid bookingId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _cancellationService.CancelBooking(bookingId, userId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}
