using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Core.Entities;
using server.Core.Enums;
using server.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.Features.Operator
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperatorController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly server.Contracts.Interfaces.INotificationService _notificationService;

        public OperatorController(AppDbContext context, server.Contracts.Interfaces.INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // --- Stats & Analytics ---

        [HttpGet("{operatorId}/stats")]
        public async Task<ActionResult<OperatorStatsDto>> GetStats(Guid operatorId)
        {
            var now = DateTime.UtcNow;

            var buses = await _context.Buses
                .Where(b => b.OperatorId == operatorId)
                .ToListAsync();

            var schedules = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .Where(s => s.Bus!.OperatorId == operatorId && s.DepartureTime > now.AddDays(-30))
                .OrderByDescending(s => s.DepartureTime)
                .ToListAsync();

            var scheduleIds = schedules.Select(s => s.Id).ToList();

            var bookings = await _context.Bookings
                .Include(b => b.Passengers)
                .Where(b => scheduleIds.Contains(b.JourneyId) && b.Status == BookingStatus.Confirmed)
                .ToListAsync();

            var stats = new OperatorStatsDto
            {
                TotalBuses = buses.Count,
                ActiveSchedules = schedules.Count(s => s.DepartureTime > now && s.Status != JourneyStatus.Cancelled),
                TotalBookings = bookings.Count,
                TotalRevenue = bookings.Sum(b => b.TotalAmount),
                RecentTrips = schedules.Take(10).Select(s => new OperatorTripDto
                {
                    ScheduleId = s.Id,
                    Route = s.Route != null ? $"{s.Route.Source} → {s.Route.Destination}" : "Unknown Route",
                    BusNumber = s.Bus?.BusNumber ?? "Unknown",
                    Departure = s.DepartureTime,
                    Status = s.Status.ToString(),
                    BookedSeats = bookings.Where(b => b.JourneyId == s.Id).Sum(b => b.Passengers?.Count ?? 0),
                    MaxSeats = s.Bus?.TotalSeats ?? 40,
                    Revenue = bookings.Where(b => b.JourneyId == s.Id).Sum(b => b.TotalAmount)
                }).ToList()
            };

            return Ok(stats);
        }

        // --- Fleet Management ---

        [HttpGet("{operatorId}/buses")]
        public async Task<ActionResult<IEnumerable<Bus>>> GetBuses(Guid operatorId)
        {
            var buses = await _context.Buses
                .Where(b => b.OperatorId == operatorId)
                .ToListAsync();
            return Ok(buses);
        }        [HttpPost("{operatorId}/buses")]
        public async Task<ActionResult> AddBus(Guid operatorId, [FromBody] BusRequestDto dto)
        {
            try 
            {
                // 1. Validate basic input
                if (dto == null) return BadRequest("Invalid request payload.");
                if (string.IsNullOrWhiteSpace(dto.BusNumber)) return BadRequest("Bus number is required.");

                // 2. Check if Bus Number already exists (Unique constraint check)
                var busNum = dto.BusNumber.Trim();
                Console.WriteLine($"[Info] Registering bus: {busNum} for operator: {operatorId}");
                
                var existingBus = await _context.Buses.FirstOrDefaultAsync(b => b.BusNumber == busNum);
                if (existingBus != null)
                {
                    // If the existing bus is Approved or Pending, block it.
                    if (existingBus.IsApproved || existingBus.Status == ApprovalStatus.Pending)
                    {
                        return BadRequest($"A bus with number {busNum} is already registered and is either approved or pending review.");
                    }
                    
                    // If it was Rejected, we'll remove it first so the new one can be added fresh
                    _context.Buses.Remove(existingBus);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[Info] Removing previously rejected bus {busNum} to allow fresh registration.");
                }

                // 3. Verify user and operator profile (avoiding tracking conflicts)
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == operatorId);
                if (user == null) return NotFound("User account not found.");
                
                if (user.Role != UserRole.Operator) 
                    return BadRequest("Your account is not registered as a Bus Operator.");

                // 4. Ensure specialized BusOperator profile exists (TPT child table)
                var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == operatorId);
                if (op == null) 
                {
                    try {
                        // RAW SQL INSERT to avoid EF Core TPT tracking conflict with existing User record
                        await _context.Database.ExecuteSqlRawAsync(
                            "INSERT INTO \"BusOperators\" (\"Id\", \"CompanyName\", \"IsApproved\", \"Address\", \"Status\") VALUES ({0}, {1}, {2}, {3}, {4})",
                            user.Id, user.FullName, false, "", (int)ApprovalStatus.Pending
                        );
                        
                        op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == operatorId);
                        Console.WriteLine($"[Success] Created missing Operator profile for {operatorId}");
                    } catch (Exception opEx) {
                        Console.WriteLine($"[Error] Failed to create Operator profile: {opEx.Message}");
                        throw new Exception("Could not initialize operator profile. Please contact support.", opEx);
                    }
                }
                else if (op.Status == ApprovalStatus.Rejected)
                {
                    // Automatic re-request: If they add a bus while rejected, reset operator status to Pending
                    op.Status = ApprovalStatus.Pending;
                    op.IsApproved = false;
                    op.RejectionReason = "Auto-resubmitted via New Bus Registration";
                    
                    _context.Entry(op).State = EntityState.Modified;
                    await _context.SaveChangesAsync(); // Save immediately to be sure
                    Console.WriteLine($"[Info] Auto-resubmitting operator {operatorId} due to new bus registration.");
                }

                // 5. Create and Save the Bus
                var bus = new Bus
                {
                    BusNumber = busNum,
                    BusType = dto.BusType ?? "AC Sleeper",
                    TotalSeats = dto.TotalSeats > 0 ? dto.TotalSeats : 40,
                    LayoutConfig = dto.LayoutConfig,
                    OperatorId = operatorId,
                    IsApproved = false,
                    Status = ApprovalStatus.Pending
                };

                _context.Buses.Add(bus);
                await _context.SaveChangesAsync();

                // 6. Notify Admin
                var admin = await _context.Admins.AsNoTracking().FirstOrDefaultAsync();
                if (admin != null)
                {
                    try {
                        await _notificationService.NotifyUserAsync(
                            admin.Id, 
                            "New Bus Approval Required", 
                            $"Operator {op?.CompanyName ?? "Unknown"} added a new bus: {bus.BusNumber}. Please review and approve.", 
                            "BusApproval", 
                            bus.Id.ToString()
                        );
                    } catch (Exception nEx) {
                        Console.WriteLine($"[Warning] Notification failed: {nEx.Message}");
                        // Don't fail the whole request if notification fails
                    }
                }

                return Ok(new { message = "Bus added successfully. It is now pending Admin approval.", busId = bus.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Critical Error] AddBus: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"[Inner Exception]: {ex.InnerException.Message}");
                
                return StatusCode(500, new { 
                    message = "A server error occurred while processing your request.", 
                    details = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        // --- Schedule Management ---

        [HttpGet("{operatorId}/schedules")]
        public async Task<ActionResult<IEnumerable<Schedule>>> GetSchedules(Guid operatorId)
        {
            var schedules = await _context.Schedules
                .Include(s => s.Bus)
                .Include(s => s.Route)
                .Where(s => s.Bus!.OperatorId == operatorId)
                .OrderByDescending(s => s.DepartureTime)
                .ToListAsync();

            return Ok(schedules);
        }

        [HttpPost("{operatorId}/schedules")]
        public async Task<ActionResult> CreateSchedule(Guid operatorId, [FromBody] ScheduleRequestDto dto)
        {
            try 
            {
                var bus = await _context.Buses.FindAsync(dto.BusId);
                if (bus == null) return BadRequest("Bus not found.");
                if (bus.OperatorId != operatorId) return BadRequest("Unauthorized: You do not own this bus.");
                if (!bus.IsApproved) return BadRequest("This bus has not been approved by the Admin yet.");

                var route = await _context.Routes.FindAsync(dto.RouteId);
                if (route == null) return BadRequest("Invalid route selected.");

                // Auto-assign default hubs if none provided (user removed manual selection)
                if (dto.BoardingHubIds == null || dto.BoardingHubIds.Count == 0)
                {
                    var sourceCity = await _context.Cities.FirstOrDefaultAsync(c => c.Name == route.Source);
                    if (sourceCity != null)
                    {
                        var defaultHubs = await _context.Hubs
                            .Where(h => h.CityId == sourceCity.Id && (h.Type == HubType.Boarding || h.Type == HubType.Both))
                            .Take(2)
                            .Select(h => h.Id)
                            .ToListAsync();
                        dto.BoardingHubIds = defaultHubs;
                    }
                }

                if (dto.DroppingHubIds == null || dto.DroppingHubIds.Count == 0)
                {
                    var destCity = await _context.Cities.FirstOrDefaultAsync(c => c.Name == route.Destination);
                    if (destCity != null)
                    {
                        var defaultHubs = await _context.Hubs
                            .Where(h => h.CityId == destCity.Id && (h.Type == HubType.Dropping || h.Type == HubType.Both))
                            .Take(2)
                            .Select(h => h.Id)
                            .ToListAsync();
                        dto.DroppingHubIds = defaultHubs;
                    }
                }

                var schedule = new Schedule
                {
                    BusId = dto.BusId,
                    RouteId = dto.RouteId,
                    DepartureTime = dto.DepartureTime.ToUniversalTime(),
                    ArrivalTime = dto.ArrivalTime.ToUniversalTime(),
                    Price = dto.Price,
                    AvailableSeats = bus.TotalSeats,
                    Status = JourneyStatus.Scheduled,
                    BoardingHubIds = dto.BoardingHubIds,
                    DroppingHubIds = dto.DroppingHubIds
                };

                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();

                // Notify Admin
                var admin = await _context.Admins.FirstOrDefaultAsync();
                if (admin != null)
                {
                    var op = await _context.BusOperators.FindAsync(operatorId);
                    await _notificationService.NotifyUserAsync(admin.Id, "New Trip Added", $"Operator {op?.CompanyName} added a new trip: {route.Source} to {route.Destination}.", "NewTrip", schedule.Id.ToString());
                }

                return Ok(new { message = "Schedule created successfully", scheduleId = schedule.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] CreateSchedule: {ex.Message}");
                return StatusCode(500, "Internal server error while creating schedule.");
            }
        }

        [HttpPut("{operatorId}/schedules/{scheduleId}/cancel")]
        public async Task<ActionResult> CancelSchedule(Guid operatorId, Guid scheduleId)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Bus)
                .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Bus!.OperatorId == operatorId);

            if (schedule == null) return NotFound("Schedule not found.");
            if (schedule.Status == JourneyStatus.Cancelled) return BadRequest("Already cancelled.");

            schedule.Status = JourneyStatus.Cancelled;

            // Notify affected customers
            var bookings = await _context.Bookings
                .Include(b => b.Journey)
                .ThenInclude(j => j!.Route)
                .Where(b => b.JourneyId == scheduleId && b.Status == BookingStatus.Confirmed)
                .ToListAsync();

            var route = await _context.Routes.FindAsync(schedule.RouteId);
            var routeText = route != null ? $"{route.Source} to {route.Destination}" : "your journey";

            foreach (var booking in bookings)
            {
                booking.Status = BookingStatus.Cancelled;
                
                await _notificationService.NotifyUserAsync(
                    booking.CustomerId,
                    "Trip Cancelled",
                    $"We regret to inform you that your trip from {routeText} on {schedule.DepartureTime:MMM d} has been cancelled by the operator. A full refund has been initiated.",
                    "TripCancelled",
                    scheduleId.ToString()
                );
            }

            // Also notify Admin
            var admin = await _context.Admins.AsNoTracking().FirstOrDefaultAsync();
            if (admin != null)
            {
                var op = await _context.BusOperators.AsNoTracking().FirstOrDefaultAsync(o => o.Id == operatorId);
                await _notificationService.NotifyUserAsync(
                    admin.Id,
                    "Operator Cancelled Trip",
                    $"Operator {op?.CompanyName ?? "Unknown"} has cancelled trip {routeText} scheduled for {schedule.DepartureTime:g}.",
                    "OperatorCancellation"
                );
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Schedule cancelled. {bookings.Count} customers notified." });
        }

        [HttpGet("routes")]
        public async Task<ActionResult<IEnumerable<BusRoute>>> GetAvailableRoutes()
        {
            var routes = await _context.Routes.ToListAsync();
            return Ok(routes);
        }

        [HttpPost("{operatorId}/hubs")]
        public async Task<ActionResult> CreateHub(Guid operatorId, [FromBody] HubRequestDto dto)
        {
            var cityExists = await _context.Cities.AnyAsync(c => c.Id == dto.CityId);
            if (!cityExists) return BadRequest("City does not exist.");

            if (!Enum.TryParse(dto.Type, out HubType parsedType))
            {
                parsedType = HubType.Both;
            }

            var hub = new Hub
            {
                Name = dto.Name,
                CityId = dto.CityId,
                Type = parsedType,
                OperatorId = operatorId
            };

            _context.Hubs.Add(hub);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Hub added successfully", hubId = hub.Id });
        }

        [HttpDelete("{operatorId}/buses/{busId}")]
        public async Task<ActionResult> DeleteBus(Guid operatorId, Guid busId)
        {
            var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == busId && b.OperatorId == operatorId);
            if (bus == null) return NotFound("Bus not found or unauthorized.");

            // Check if bus has ANY booking history
            var hasBookings = await _context.Bookings.AnyAsync(b => b.Journey!.BusId == busId);
            if (hasBookings) 
            {
                return BadRequest("Cannot remove this vehicle because it has associated booking history. To protect user records, this vehicle must remain in the system. You may cancel its future trips instead.");
            }

            // Check if bus has active schedules (even if no bookings yet)
            var hasSchedules = await _context.Schedules.AnyAsync(s => s.BusId == busId && s.Status != JourneyStatus.Cancelled && s.DepartureTime > DateTime.UtcNow);
            if (hasSchedules) return BadRequest("Cannot remove bus with active future schedules. Please cancel the trips first.");

            _context.Buses.Remove(bus);
            await _context.SaveChangesAsync();

            // Notify Admin that request was revoked/removed
            var admin = await _context.Admins.AsNoTracking().FirstOrDefaultAsync();
            if (admin != null)
            {
                var op = await _context.BusOperators.AsNoTracking().FirstOrDefaultAsync(o => o.Id == operatorId);
                await _notificationService.NotifyUserAsync(
                    admin.Id,
                    "Fleet Registration Revoked",
                    $"Operator {op?.CompanyName ?? "Unknown"} has revoked/removed the registration request for bus {bus.BusNumber}.",
                    "FleetRevoked"
                );
            }

            return Ok(new { message = "Bus removed successfully." });
        }

        [HttpPut("{operatorId}/request-review")]
        public async Task<ActionResult> RequestReview(Guid operatorId)
        {
            // Security: In a production environment, you should verify that operatorId 
            // matches the authenticated user's ID from the JWT claims.
            
            var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == operatorId);
            if (op == null) return NotFound("Operator not found.");

            if (op.Status != ApprovalStatus.Rejected)
                return BadRequest("Only rejected accounts can request a re-review.");

            op.Status = ApprovalStatus.Pending;
            op.IsApproved = false;
            op.RejectionReason = "Manual Re-review Requested";

            await _context.SaveChangesAsync();

            // Notify Admin
            var admin = await _context.Admins.FirstOrDefaultAsync();
            if (admin != null)
            {
                await _notificationService.NotifyUserAsync(admin.Id, "Operator Re-Review Requested", $"Operator {op.CompanyName ?? op.FullName} has requested a manual re-review of their rejected account.", "AccountReview");
            }

            return Ok(new { message = "Re-review request submitted." });
        }
    }
}
