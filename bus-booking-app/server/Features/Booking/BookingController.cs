using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Core.Entities;
using server.Core.Enums;
using server.Infrastructure.Data;
using System.Security.Claims;

namespace server.Features.Booking
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ISeatLockManager _seatLockManager;
        private readonly ICancellationService _cancellationService;

        public BookingController(AppDbContext context, ISeatLockManager seatLockManager, ICancellationService cancellationService)
        {
            _context = context;
            _seatLockManager = seatLockManager;
            _cancellationService = cancellationService;
        }

        [HttpGet("layout/{journeyId}")]
        public async Task<ActionResult<SeatLayoutDto>> GetLayout(Guid journeyId)
        {
            // Explicitly release expired locks first
            await _seatLockManager.ReleaseExpiredLocks();

            var journey = await _context.Schedules
                .Include(s => s.Bus)
                    .ThenInclude(b => b!.Operator)
                .Include(s => s.Route)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == journeyId);

            if (journey == null) return NotFound("Journey not found.");

            var bookedSeats = await _context.Passengers
                .Where(p => p.Booking!.JourneyId == journeyId && p.Booking.Status == BookingStatus.Confirmed)
                .Select(p => new SeatStatusDto
                {
                    SeatNumber = p.SeatNumber,
                    Status = "Booked",
                    Gender = p.Gender == Core.Enums.Gender.F ? "Female"
                           : p.Gender == Core.Enums.Gender.M ? "Male"
                           : "Other"
                })
                .AsNoTracking()
                .ToListAsync();

            var blockedSeats = await _context.SeatLocks
                .Where(l => l.JourneyId == journeyId && l.ExpiresAt > DateTime.UtcNow)
                .Select(l => new SeatStatusDto { SeatNumber = l.SeatNumber, Status = "Blocked" })
                .AsNoTracking()
                .ToListAsync();

            var unavailableSeats = bookedSeats.Concat(blockedSeats).ToList();

            var op = journey.Bus?.Operator;
            var company = string.IsNullOrWhiteSpace(op?.CompanyName) ? op?.FullName : op?.CompanyName;
            if (string.IsNullOrWhiteSpace(company)) company = "Intercity"; // default fallback for now
            else if (company == "IntrCity") company = "Intercity";
            
            var busNumberPart = journey.Bus?.BusNumber?.Split('-').Last() ?? "";
            var busName = company;

            // Fetch Configuration
            var config = await _context.GlobalConfigurations.FirstOrDefaultAsync();

            // Hubs fetching with restriction
            var sourceCity = await _context.Cities.FirstOrDefaultAsync(c => c.Name == journey.Route!.Source);
            var destCity = await _context.Cities.FirstOrDefaultAsync(c => c.Name == journey.Route!.Destination);

            var boardingHubs = await _context.Hubs
                .Where(h => h.CityId == sourceCity!.Id && (h.Type == HubType.Boarding || h.Type == HubType.Both))
                .ToListAsync();

            var droppingHubs = await _context.Hubs
                .Where(h => h.CityId == destCity!.Id && (h.Type == HubType.Dropping || h.Type == HubType.Both))
                .ToListAsync();

            if (journey.BoardingHubIds != null && journey.BoardingHubIds.Any())
            {
                boardingHubs = boardingHubs.Where(h => journey.BoardingHubIds.Contains(h.Id)).ToList();
            }
            if (journey.DroppingHubIds != null && journey.DroppingHubIds.Any())
            {
                droppingHubs = droppingHubs.Where(h => journey.DroppingHubIds.Contains(h.Id)).ToList();
            }

            return Ok(new SeatLayoutDto
            {
                LayoutConfig = journey.Bus?.LayoutConfig,
                UnavailableSeats = unavailableSeats,
                Source = journey.Route?.Source ?? "",
                Destination = journey.Route?.Destination ?? "",
                DepartureTime = journey.DepartureTime,
                BusNumber = busNumberPart,
                BusName = busName,
                PlatformFeeType = config?.PlatformFeeType ?? "Fixed",
                PlatformFeeValue = config?.PlatformFeeValue ?? 50.00m,
                BoardingHubs = boardingHubs.Select(h => new HubStatusDto { Id = h.Id, Name = h.Name, Type = h.Type.ToString() }).ToList(),
                DroppingHubs = droppingHubs.Select(h => new HubStatusDto { Id = h.Id, Name = h.Name, Type = h.Type.ToString() }).ToList()
            });
        }


        [HttpPost("lock-seats")]
        public async Task<ActionResult<LockSeatsResponse>> LockSeats([FromBody] LockSeatsRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var lockObj = await _seatLockManager.LockSeats(request.JourneyId, request.SeatNumbers, userId);
            
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var journey = await _context.Schedules.FindAsync(request.JourneyId);
                if (journey == null) return NotFound(new { message = "Journey not found." });

                if (request.Passengers == null || request.Passengers.Count == 0)
                {
                    return BadRequest(new { message = "Passenger details are required to confirm booking." });
                }

                // Validate seats are still locked by this user
                var seatNumbers = request.Passengers.Select(p => p.SeatNumber).ToList();
                var locks = await _context.SeatLocks
                    .Where(l => l.JourneyId == request.JourneyId && seatNumbers.Contains(l.SeatNumber) && l.LockedByUserId == userId && l.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                if (locks.Count != seatNumbers.Count)
                {
                    return BadRequest(new { message = "Seat locks expired or invalid. Please select seats again." });
                }

                // Gender Adjacency Validation
                var existingPassengers = await _context.Passengers
                    .Where(p => p.Booking!.JourneyId == request.JourneyId && p.Booking.Status == BookingStatus.Confirmed)
                    .Select(p => new { p.SeatNumber, p.Gender })
                    .ToListAsync();

                foreach (var pDto in request.Passengers)
                {
                    var adjSeat = GetAdjacentSeat(pDto.SeatNumber);
                    if (adjSeat != null)
                    {
                        var neighbor = existingPassengers.FirstOrDefault(p => p.SeatNumber == adjSeat);
                        if (neighbor != null && neighbor.Gender != pDto.Gender)
                        {
                            return BadRequest(new { message = $"Seat {pDto.SeatNumber} cannot be booked by {pDto.Gender} as adjacent seat {adjSeat} is booked by {neighbor.Gender}. Strict gender separation applies." });
                        }
                    }
                }

                // Fetch Configuration
                var config = await _context.GlobalConfigurations.FirstOrDefaultAsync();
                
                string feeType = config?.PlatformFeeType ?? "Fixed";
                decimal feeValue = config?.PlatformFeeValue ?? 50.00m;
                
                decimal baseTotal = journey.Price * request.Passengers.Count;
                decimal calculatedFee = feeType == "Percentage" ? (baseTotal * feeValue / 100m) : feeValue;

                // Create Booking
                var booking = new Core.Entities.Booking
                {
                    CustomerId = userId,
                    JourneyId = request.JourneyId,
                    TotalAmount = baseTotal,
                    PlatformFee = calculatedFee,
                    Status = BookingStatus.Confirmed,
                    BoardingHubId = request.BoardingPointId != Guid.Empty ? request.BoardingPointId : null,
                    DroppingHubId = request.DroppingPointId != Guid.Empty ? request.DroppingPointId : null
                };

                _context.Bookings.Add(booking);

                // Add Passengers
                foreach (var pDto in request.Passengers)
                {
                    _context.Passengers.Add(new Passenger
                    {
                        Booking = booking,
                        SeatNumber = pDto.SeatNumber,
                        Name = pDto.Name,
                        Age = pDto.Age,
                        Gender = pDto.Gender
                    });
                }

                // Create Payment
                _context.Payments.Add(new Payment
                {
                    Booking = booking,
                    Amount = booking.TotalAmount + booking.PlatformFee,
                    TransactionId = "TXN_" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    Status = PaymentStatus.Success,
                    ProcessedAt = DateTime.UtcNow
                });

                // Update available seats
                journey.AvailableSeats -= request.Passengers.Count;

                // Release locks
                _context.SeatLocks.RemoveRange(locks);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new BookingResponse
                {
                    BookingId = booking.Id,
                    Status = "Confirmed"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = $"Internal Server Error: {ex.Message} - {ex.InnerException?.Message}" });
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<BookingHistoryDto>>> GetHistory()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var bookings = await _context.Bookings
                .Include(b => b.Journey)
                    .ThenInclude(j => j!.Route)
                .Include(b => b.Journey)
                    .ThenInclude(j => j!.Bus)
                        .ThenInclude(bus => bus!.Operator)
                .Include(b => b.Passengers)
                .Include(b => b.BoardingHub)
                .Include(b => b.DroppingHub)
                .Where(b => b.CustomerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var now = DateTime.UtcNow;

            var historyDtos = bookings.Select(b => {
                var op = b.Journey?.Bus?.Operator;
                var company = string.IsNullOrWhiteSpace(op?.CompanyName) ? op?.FullName : op?.CompanyName;
                if (string.IsNullOrWhiteSpace(company)) company = "Intercity";
                else if (company == "IntrCity") company = "Intercity";
                
                var busNumberPart = b.Journey?.Bus?.BusNumber?.Split('-').Last() ?? "";

                return new BookingHistoryDto
                {
                    BookingId = b.Id,
                    Source = b.Journey?.Route?.Source ?? "Unknown",
                    Destination = b.Journey?.Route?.Destination ?? "Unknown",
                    DepartureTime = b.Journey?.DepartureTime ?? DateTime.UtcNow,
                    ArrivalTime = b.Journey?.ArrivalTime ?? DateTime.UtcNow,
                    BusName = company,
                    BusNumber = busNumberPart,
                    TotalAmount = b.TotalAmount,
                    Status = b.Status.ToString(),
                    SeatNumbers = b.Passengers?.Select(p => p.SeatNumber).ToList() ?? new List<string>(),
                    PassengerNames = b.Passengers?.Select(p => p.Name).ToList() ?? new List<string>(),
                    BoardingPoint = b.BoardingHub?.Name ?? "N/A",
                    DroppingPoint = b.DroppingHub?.Name ?? "N/A",
                    Category = b.Status == server.Core.Enums.BookingStatus.Cancelled ? "Cancelled"
                             : (b.Journey?.ArrivalTime ?? DateTime.UtcNow) > now ? "Upcoming" 
                             : "Completed"
                };
            }).ToList();

            return Ok(historyDtos);
        }

        [HttpDelete("release-locks")]
        public async Task<ActionResult> ReleaseLocks([FromQuery] Guid journeyId, [FromQuery] string seats)
        {
            var seatNumbers = seats.Split(',').ToList();
            await _seatLockManager.ReleaseLocks(journeyId, seatNumbers);
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


        private string? GetAdjacentSeat(string seatNumber)
        {
            if (string.IsNullOrEmpty(seatNumber) || seatNumber.Length < 2) return null;
            char row = seatNumber[0];
            if (!int.TryParse(seatNumber.Substring(1), out int num)) return null;

            if (num == 1) return $"{row}2";
            if (num == 2) return $"{row}1";
            if (num == 3) return $"{row}4";
            if (num == 4) return $"{row}3";
            
            return null;
        }
    }
}
