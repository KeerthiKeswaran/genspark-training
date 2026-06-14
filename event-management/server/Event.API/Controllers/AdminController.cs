using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Event.Contracts.IServices;
using Event.Business.Exceptions;
using Event.Models.DTOs;

namespace Event.API.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _adminService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("events")]
        public async Task<IActionResult> GetEvents(
            [FromQuery] string? keyword,
            [FromQuery] string? eventType,
            [FromQuery] string? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? sortBy,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            try
            {
                var result = await _adminService.GetEventsPagedAsync(keyword, eventType, status, startDate, endDate, sortBy, page, size);
                return Ok(result);
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("support/tickets")]
        public async Task<IActionResult> GetSupportTickets()
        {
            try
            {
                var tickets = await _adminService.GetSupportTicketsAsync();
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("support/tickets/{id}/respond")]
        public async Task<IActionResult> RespondToTicket(int id, [FromBody] RespondToTicketRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Response))
                {
                    return BadRequest(new { Message = "Response text cannot be empty." });
                }

                var success = await _adminService.RespondToTicketAsync(id, request.Response);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to respond to support ticket." });
                }

                return Ok(new { Message = "Response submitted and user notified successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("support/tickets/{id}/escalate")]
        public async Task<IActionResult> EscalateTicket(int id, [FromBody] EscalateTicketRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { Message = "Request payload cannot be null." });
                }

                // Retrieve current admin ID from JWT claims (NameIdentifier or sub)
                string adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                 ?? User.FindFirst("sub")?.Value 
                                 ?? string.Empty;

                if (string.IsNullOrEmpty(adminId))
                {
                    // Fallback to custom header if available
                    if (HttpContext.Request.Headers.TryGetValue("X-Admin-Id", out var headerId))
                    {
                        adminId = headerId.ToString();
                    }
                }

                if (string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized(new { Message = "Admin identification not found in claims." });
                }

                var success = await _adminService.EscalateTicketAsync(id, adminId, request);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to escalate support ticket." });
                }

                return Ok(new { Message = "Support ticket escalated successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetEventReports()
        {
            try
            {
                var reports = await _adminService.GetFlaggedEventsReportsAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("reports/{reportId}/dismiss")]
        public async Task<IActionResult> DismissReport(int reportId)
        {
            try
            {
                var success = await _adminService.DismissEventReportAsync(reportId);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to dismiss report." });
                }
                return Ok(new { Message = "Report dismissed successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("reports/{reportId}/uphold")]
        public async Task<IActionResult> UpholdReport(int reportId, [FromBody] UpholdReportRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new { Message = "Reason is required when upholding a report." });
                }

                string adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                 ?? User.FindFirst("sub")?.Value 
                                 ?? string.Empty;

                if (string.IsNullOrEmpty(adminId))
                {
                    if (HttpContext.Request.Headers.TryGetValue("X-Admin-Id", out var headerId))
                    {
                        adminId = headerId.ToString();
                    }
                }

                if (string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized(new { Message = "Admin identification not found." });
                }

                var success = await _adminService.UpholdEventReportAsync(reportId, adminId, request.Reason, request.OrganizerAction);
                if (!success)
                {
                    return BadRequest(new { Message = "Failed to uphold report." });
                }
                return Ok(new { Message = "Report upheld and organizer status changed successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }



        [HttpGet("regions")]
        public async Task<IActionResult> GetRegions()
        {
            try
            {
                var regions = await _adminService.GetAllRegionsAsync();
                return Ok(regions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("venues")]
        public async Task<IActionResult> GetVenues()
        {
            try
            {
                var venues = await _adminService.GetAllVenuesAsync();
                return Ok(venues);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("venues")]
        public async Task<IActionResult> CreateVenue([FromBody] CreateVenueRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { Message = "Request payload cannot be null." });

                var venue = await _adminService.CreateVenueAsync(request);
                return Ok(venue);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("staff")]
        public async Task<IActionResult> GetStaff()
        {
            try
            {
                var directory = await _adminService.GetStaffDirectoryAsync();
                return Ok(directory);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("events/{eventId}/allocate-staff")]
        public async Task<IActionResult> AllocateStaff([FromRoute] int eventId, [FromBody] AllocateStaffRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { Message = "Request payload cannot be null." });

                var result = await _adminService.AllocateStaffToEventAsync(eventId, request.EmployeeId);
                return Ok(new { Success = result, Message = "Staff allocated to event successfully." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
