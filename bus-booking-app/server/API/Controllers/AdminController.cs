using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Core.Entities;
using server.Infrastructure.Data;
using server.Core.Enums;
using server.Contracts.Interfaces;

namespace server.Features.Administration
{
    // [Authorize(Roles = "Admin")] // Commented out for dummy platform ease, but should be enabled in prod
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IOperatorWorkflowService _workflowService;
        private readonly INotificationService _notificationService;

        public AdminController(AppDbContext context, IOperatorWorkflowService workflowService, INotificationService notificationService)
        {
            _context = context;
            _workflowService = workflowService;
            _notificationService = notificationService;
        }

        // --- Master Data (Locations) ---

        [HttpPost("cities")]
        public async Task<ActionResult> AddCity([FromBody] CityDto dto)
        {
            var city = new City { Name = dto.Name, State = dto.State };
            _context.Cities.Add(city);
            await _context.SaveChangesAsync();
            return Ok(new { message = "City added successfully", city });
        }

        [HttpPost("hubs")]
        public async Task<ActionResult> AddHub([FromBody] HubDto dto)
        {
            var cityExists = await _context.Cities.AnyAsync(c => c.Id == dto.CityId);
            if (!cityExists) return BadRequest("City does not exist.");

            if (!Enum.TryParse(dto.Type, out HubType parsedType))
            {
                parsedType = HubType.Both;
            }

            var hub = new Hub { Name = dto.Name, CityId = dto.CityId, Type = parsedType };
            _context.Hubs.Add(hub);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Hub added successfully", hub });
        }

        // --- Route Management ---

        [HttpGet("routes")]
        public async Task<ActionResult<IEnumerable<RouteResponseDto>>> GetRoutes()
        {
            var routes = await _context.Routes
                .Select(r => new RouteResponseDto
                {
                    Id = r.Id,
                    Source = r.Source,
                    Destination = r.Destination,
                    DistanceKm = r.DistanceKm
                })
                .ToListAsync();

            return Ok(routes);
        }

        [HttpPost("routes")]
        public async Task<ActionResult> AddRoute([FromBody] RouteRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Source) || string.IsNullOrWhiteSpace(dto.Destination))
                return BadRequest("Source and Destination are required.");

            var route = new BusRoute
            {
                Source = dto.Source,
                Destination = dto.Destination,
                DistanceKm = dto.DistanceKm
            };

            _context.Routes.Add(route);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Route added successfully", route });
        }

        // --- User & Operator Management ---

        [HttpGet("operators")]
        public async Task<ActionResult<IEnumerable<OperatorResponseDto>>> GetOperators()
        {
            var ops = await _context.BusOperators
                .Select(o => new OperatorResponseDto
                {
                    Id = o.Id,
                    CompanyName = (string.IsNullOrEmpty(o.CompanyName) || o.CompanyName == "Unspecified Company") ? o.FullName : o.CompanyName,
                    Email = o.Email,
                    Phone = o.Phone,
                    IsApproved = o.IsApproved,
                    Status = o.Status,
                    RejectionReason = o.RejectionReason,
                    CreatedAt = o.CreatedAt
                })
                .OrderBy(o => o.Status == ApprovalStatus.Pending ? 0 : 1) // Pending first
                .ThenByDescending(o => o.CreatedAt) 
                .ToListAsync();

            return Ok(ops);
        }

        [HttpPut("operators/{id}/approve")]
        public async Task<ActionResult> ApproveOperator(Guid id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try 
            {
                var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == id);
                if (op == null) return NotFound("Operator not found");

                op.IsApproved = true;
                op.Status = ApprovalStatus.Approved;
                op.RejectionReason = null;
                
                _context.BusOperators.Update(op);

                // Automatically approve all pending buses for this operator
                var pendingBuses = await _context.Buses
                    .Where(b => b.OperatorId == id && !b.IsApproved && b.Status == ApprovalStatus.Pending)
                    .ToListAsync();

                foreach (var b in pendingBuses)
                {
                    b.IsApproved = true;
                    b.Status = ApprovalStatus.Approved;
                    b.RejectionReason = null;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                await _notificationService.NotifyUserAsync(op.Id, "Account Approved", "Congratulations! Your bus operator account has been approved. You can now manage your fleet and schedules.", "Approval");
                await _notificationService.SendEmailAsync(op.Email, "Operator Account Approved", "Your account on BUSBOOK has been approved.");

                return Ok(new { message = "Operator approved successfully and all pending buses were also approved." });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPut("operators/{id}/deny")]
        public async Task<ActionResult> DenyOperator(Guid id, [FromBody] RejectionRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == id);
                if (op == null) return NotFound("Operator not found");

                op.IsApproved = false;
                op.Status = ApprovalStatus.Rejected;
                op.RejectionReason = request.Reason;

                // Automatically deny all pending buses for this operator
                var pendingBuses = await _context.Buses
                    .Where(b => b.OperatorId == id && !b.IsApproved && b.Status == ApprovalStatus.Pending)
                    .ToListAsync();

                foreach (var b in pendingBuses)
                {
                    b.IsApproved = false;
                    b.Status = ApprovalStatus.Rejected;
                    b.RejectionReason = $"Operator account rejected: {request.Reason}";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _notificationService.NotifyUserAsync(op.Id, "Account Registration Rejected", $"Your operator account registration was rejected. Reason: {request.Reason}", "Rejection");
                
                return Ok(new { message = "Operator registration rejected and all associated pending buses were also denied." });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPut("operators/{id}/deactivate")]
        public async Task<ActionResult> DeactivateOperator(Guid id)
        {
            var result = await _workflowService.DeactivateOperatorAsync(id);
            if (!result.Success) return BadRequest(result.Message);

            return Ok(new { message = result.Message });
        }

        [HttpPut("operators/{id}/activate")]
        public async Task<ActionResult> ActivateOperator(Guid id)
        {
            var result = await _workflowService.ReactivateOperatorAsync(id);
            if (!result.Success) return BadRequest(result.Message);

            return Ok(new { message = result.Message });
        }

        // --- Fleet Approval ---

        [HttpGet("buses/pending")]
        public async Task<ActionResult<IEnumerable<BusApprovalDto>>> GetPendingBuses()
        {
            var buses = await _context.Buses
                .Include(b => b.Operator)
                .Where(b => !b.IsApproved)
                .OrderByDescending(b => b.Id)
                .Select(b => new BusApprovalDto
                {
                    Id = b.Id,
                    BusNumber = b.BusNumber,
                    BusType = b.BusType,
                    TotalSeats = b.TotalSeats,
                    OperatorName = b.Operator != null ? b.Operator.FullName : "Unknown",
                    CompanyName = (b.Operator != null && !string.IsNullOrEmpty(b.Operator.CompanyName) && b.Operator.CompanyName != "Unspecified Company") 
                                  ? b.Operator.CompanyName : (b.Operator != null ? b.Operator.FullName : "Unknown"),
                    IsApproved = b.IsApproved,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason
                })
                .ToListAsync();

            return Ok(buses);
        }

        [HttpPut("buses/{id}/approve")]
        public async Task<ActionResult> ApproveBus(Guid id)
        {
            var bus = await _context.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == id);
            if (bus == null) return NotFound("Bus not found");

            // Check if operator is approved
            if (bus.Operator == null || !bus.Operator.IsApproved)
            {
                return BadRequest("Cannot approve bus because the operator is not yet approved. Please approve the operator first.");
            }

            bus.IsApproved = true;
            bus.Status = ApprovalStatus.Approved;
            bus.RejectionReason = null;

            await _context.SaveChangesAsync();

            if (bus.OperatorId != Guid.Empty)
            {
                await _notificationService.NotifyUserAsync(
                    bus.OperatorId, 
                    "Bus Approved", 
                    $"Congratulations! Your fleet has been approved. Your bus {bus.BusNumber} is now available for scheduling trips.", 
                    "BusApproval",
                    bus.Id.ToString()
                );
            }

            return Ok(new { message = "Bus approved successfully" });
        }

        [HttpPut("buses/{id}/deny")]
        public async Task<ActionResult> DenyBus(Guid id, [FromBody] RejectionRequest request)
        {
            var bus = await _context.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == id);
            if (bus == null) return NotFound("Bus not found");

            bus.IsApproved = false;
            bus.Status = ApprovalStatus.Rejected;
            bus.RejectionReason = request.Reason;

            await _context.SaveChangesAsync();

            if (bus.OperatorId != Guid.Empty)
            {
                await _notificationService.NotifyUserAsync(
                    bus.OperatorId, 
                    "Bus Registration Rejected", 
                    $"Your bus {bus.BusNumber} registration was rejected. Reason: {request.Reason}", 
                    "BusRejection",
                    bus.Id.ToString()
                );
            }

            return Ok(new { message = "Bus registration rejected" });
        }

        // --- Platform Settings ---

        [HttpGet("settings/fee")]
        public async Task<ActionResult<FeeSettingDto>> GetFeeSettings()
        {
            var config = await _context.GlobalConfigurations.FirstOrDefaultAsync();

            return Ok(new FeeSettingDto
            {
                FeeType = config?.PlatformFeeType ?? "Fixed",
                FeeValue = config?.PlatformFeeValue ?? 50.00m,
                CommissionPercentage = config?.OperatorCommissionPercentage ?? 10.00m
            });
        }

        [HttpPut("settings/fee")]
        public async Task<ActionResult> UpdateFeeSettings([FromBody] FeeSettingDto dto)
        {
            var config = await _context.GlobalConfigurations.FirstOrDefaultAsync();
            
            if (config == null)
            {
                config = new GlobalConfiguration();
                _context.GlobalConfigurations.Add(config);
            }

            config.PlatformFeeType = dto.FeeType;
            config.PlatformFeeValue = dto.FeeValue;
            config.OperatorCommissionPercentage = dto.CommissionPercentage;
            config.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Platform settings updated successfully." });
        }

        [HttpGet("stats")]
        public async Task<ActionResult<AdminStatsDto>> GetStats(
            [FromQuery] DateTime? startDate, 
            [FromQuery] DateTime? endDate, 
            [FromQuery] Guid? operatorId)
        {
            var now = DateTime.UtcNow;
            
            // Set default range: Today (start of day) to End of Tomorrow
            var start = startDate?.ToUniversalTime() ?? DateTime.UtcNow.Date;
            var end = endDate?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddDays(2);

            // 1. Define Base Filtered Queries
            var validBookingsQuery = _context.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed && b.Payment != null && b.Payment.Status == PaymentStatus.Success);
            
            var schedulesQuery = _context.Schedules.AsQueryable();
            
            // Apply Date Range Filter: Show trips starting in this window
            validBookingsQuery = validBookingsQuery.Where(b => b.Journey!.DepartureTime >= start && b.Journey!.DepartureTime <= end);
            schedulesQuery = schedulesQuery.Where(s => s.DepartureTime >= start && s.DepartureTime <= end);

            // Apply Operator Filter if provided
            if (operatorId.HasValue && operatorId.Value != Guid.Empty)
            {
                validBookingsQuery = validBookingsQuery.Where(b => b.Journey!.Bus!.OperatorId == operatorId.Value);
                schedulesQuery = schedulesQuery.Where(s => s.Bus!.OperatorId == operatorId.Value);
            }

            // Apply Date Range Filter
            // For bookings, we filter by the Journey's Departure Time


            // Fetch Configuration for revenue logic
            var config = await _context.GlobalConfigurations.FirstOrDefaultAsync();
            decimal commissionRate = config?.OperatorCommissionPercentage ?? 10.00m;

            // 2. Core Analytics (Filtered)
            var totalBookings = await validBookingsQuery.CountAsync();
            
            var totalBaseAmount = await validBookingsQuery.SumAsync(b => b.TotalAmount);
            var totalFees = await validBookingsQuery.SumAsync(b => b.PlatformFee);
            
            var grossBookingRevenue = totalBaseAmount + totalFees;
            var netRevenue = totalFees + (totalBaseAmount * commissionRate / 100m);
            var operatorPayout = grossBookingRevenue - netRevenue;

            var upcomingBookings = await validBookingsQuery
                .CountAsync(b => b.Journey != null && b.Journey.DepartureTime > now);
            
            var completedBookings = await validBookingsQuery
                .CountAsync(b => b.Journey != null && b.Journey.ArrivalTime < now);

            var activeOperators = await _context.BusOperators.CountAsync(o => o.IsApproved);
            var totalCities = await _context.Cities.CountAsync();

            // 3. Platform Trip Activity (Filtered)
            var recentTrips = await schedulesQuery
                .Include(s => s.Route)
                .Include(s => s.Bus)
                .ThenInclude(b => b!.Operator)
                .OrderByDescending(s => s.DepartureTime)
                .Take(1000) // Support larger activity views
                .Select(s => new AdminTripDto
                {
                    ScheduleId = s.Id,
                    Route = s.Route != null ? $"{s.Route.Source} → {s.Route.Destination}" : "Unknown Route",
                    Operator = s.Bus != null && s.Bus.Operator != null ? s.Bus.Operator.CompanyName : "Unknown Operator",
                    Departure = s.DepartureTime,
                    Status = s.Status == JourneyStatus.Cancelled ? "Cancelled"
                           : s.ArrivalTime < now ? "Completed"
                           : "Upcoming",
                    BookedSeats = _context.Passengers.Count(p => 
                        p.Booking!.JourneyId == s.Id && 
                        p.Booking.Status == BookingStatus.Confirmed && 
                        p.Booking.Payment != null && 
                        p.Booking.Payment.Status == PaymentStatus.Success),
                    CancelledSeats = _context.Passengers.Count(p => 
                        p.Booking!.JourneyId == s.Id && 
                        p.Booking.Status == BookingStatus.Cancelled),
                    MaxSeats = s.Bus != null ? s.Bus.TotalSeats : 40,
                    Price = s.Price
                })
                .ToListAsync();

            return Ok(new AdminStatsDto
            {
                TotalBookings = totalBookings,
                UpcomingBookings = upcomingBookings,
                CompletedBookings = completedBookings,
                GrossBookingRevenue = grossBookingRevenue,
                NetRevenue = netRevenue,
                OperatorPayout = operatorPayout,
                ActiveOperators = activeOperators,
                TotalCities = totalCities,
                RecentTrips = recentTrips
            });
        }
    }
}
