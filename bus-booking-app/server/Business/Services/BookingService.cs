using server.Core.Entities;
using server.Core.Enums;
using server.Contracts.Interfaces;
using server.Features.Booking;
using server.Business.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ISeatLockService _seatLockService;

        public BookingService(IBookingRepository bookingRepository, ISeatLockService seatLockService)
        {
            _bookingRepository = bookingRepository;
            _seatLockService = seatLockService;
        }

        public async Task<SeatLayoutDto?> GetLayoutAsync(Guid journeyId)
        {
            // Explicitly release expired locks first
            await _seatLockService.ReleaseExpiredLocks();

            var journey = await _bookingRepository.GetJourneyByIdAsync(journeyId);
            if (journey == null) return null;

            var confirmedPassengers = await _bookingRepository.GetConfirmedPassengersForJourneyAsync(journeyId);
            var bookedSeats = confirmedPassengers.Select(p => new SeatStatusDto
            {
                SeatNumber = p.SeatNumber,
                Status = "Booked",
                Gender = p.Gender == Core.Enums.Gender.F ? "Female"
                       : p.Gender == Core.Enums.Gender.M ? "Male"
                       : "Other"
            }).ToList();

            var locks = await _bookingRepository.GetActiveLocksForJourneyAsync(journeyId);
            var blockedSeats = locks.Select(l => new SeatStatusDto 
            { 
                SeatNumber = l.SeatNumber, 
                Status = "Blocked" 
            }).ToList();

            var unavailableSeats = bookedSeats.Concat(blockedSeats).ToList();

            var op = journey.Bus?.Operator;
            var company = string.IsNullOrWhiteSpace(op?.CompanyName) ? op?.FullName : op?.CompanyName;
            if (string.IsNullOrWhiteSpace(company)) company = "Intercity"; // default fallback for now
            else if (company == "IntrCity") company = "Intercity";
            
            var busNumberPart = journey.Bus?.BusNumber?.Split('-').Last() ?? "";
            var busName = company;

            // Fetch Configuration
            var config = await _bookingRepository.GetGlobalConfigurationAsync();

            // Hubs fetching with restriction
            var sourceCity = await _bookingRepository.GetCityByNameAsync(journey.Route!.Source);
            var destCity = await _bookingRepository.GetCityByNameAsync(journey.Route!.Destination);

            if (sourceCity == null || destCity == null) return null;

            var boardingHubs = await _bookingRepository.GetHubsByCityIdAndTypeAsync(sourceCity.Id, new List<HubType> { HubType.Boarding, HubType.Both });
            var droppingHubs = await _bookingRepository.GetHubsByCityIdAndTypeAsync(destCity.Id, new List<HubType> { HubType.Dropping, HubType.Both });

            if (journey.BoardingHubIds != null && journey.BoardingHubIds.Any())
            {
                boardingHubs = boardingHubs.Where(h => journey.BoardingHubIds.Contains(h.Id)).ToList();
            }
            if (journey.DroppingHubIds != null && journey.DroppingHubIds.Any())
            {
                droppingHubs = droppingHubs.Where(h => journey.DroppingHubIds.Contains(h.Id)).ToList();
            }

            return new SeatLayoutDto
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
            };
        }

        public async Task<BookingResponse> ConfirmBookingAsync(Guid userId, ConfirmBookingRequest request)
        {
            await using var transaction = await _bookingRepository.BeginTransactionAsync();
            try
            {
                var journey = await _bookingRepository.GetJourneyByIdAsync(request.JourneyId);
                if (journey == null)
                {
                    throw new EntityNotFoundException("Journey not found.");
                }

                if (request.Passengers == null || request.Passengers.Count == 0)
                {
                    throw new BookingValidationException("Passenger details are required to confirm booking.");
                }

                // Validate seats are still locked by this user
                var seatNumbers = request.Passengers.Select(p => p.SeatNumber).ToList();
                var locks = await _bookingRepository.GetActiveLocksForSeatsAsync(request.JourneyId, seatNumbers, userId);

                if (locks.Count != seatNumbers.Count)
                {
                    throw new BookingValidationException("Seat locks expired or invalid. Please select seats again.");
                }

                // Gender Adjacency Validation
                var confirmedPassengers = await _bookingRepository.GetConfirmedPassengersForJourneyAsync(request.JourneyId);
                var existingPassengers = confirmedPassengers.Select(p => new { p.SeatNumber, p.Gender }).ToList();

                foreach (var pDto in request.Passengers)
                {
                    var adjSeat = GetAdjacentSeat(pDto.SeatNumber);
                    if (adjSeat != null)
                    {
                        var neighbor = existingPassengers.FirstOrDefault(p => p.SeatNumber == adjSeat);
                        if (neighbor != null && neighbor.Gender != pDto.Gender)
                        {
                            throw new BookingValidationException($"Seat {pDto.SeatNumber} cannot be booked by {pDto.Gender} as adjacent seat {adjSeat} is booked by {neighbor.Gender}. Strict gender separation applies.");
                        }
                    }
                }

                // Fetch Configuration
                var config = await _bookingRepository.GetGlobalConfigurationAsync();
                
                string feeType = config?.PlatformFeeType ?? "Fixed";
                decimal feeValue = config?.PlatformFeeValue ?? 50.00m;
                
                decimal baseTotal = journey.Price * request.Passengers.Count;
                decimal calculatedFee = feeType == "Percentage" ? (baseTotal * feeValue / 100m) : feeValue;

                // Create Booking
                var booking = new Booking
                {
                    CustomerId = userId,
                    JourneyId = request.JourneyId,
                    TotalAmount = baseTotal,
                    PlatformFee = calculatedFee,
                    Status = BookingStatus.Confirmed,
                    BoardingHubId = request.BoardingPointId != Guid.Empty ? request.BoardingPointId : null,
                    DroppingHubId = request.DroppingPointId != Guid.Empty ? request.DroppingPointId : null
                };

                await _bookingRepository.AddBookingAsync(booking);

                // Add Passengers
                foreach (var pDto in request.Passengers)
                {
                    await _bookingRepository.AddPassengerAsync(new Passenger
                    {
                        Booking = booking,
                        SeatNumber = pDto.SeatNumber,
                        Name = pDto.Name,
                        Age = pDto.Age,
                        Gender = pDto.Gender
                    });
                }

                // Create Payment
                await _bookingRepository.AddPaymentAsync(new Payment
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
                await _bookingRepository.RemoveSeatLocksAsync(locks);

                await _bookingRepository.SaveChangesAsync();
                await transaction.CommitAsync();

                return new BookingResponse
                {
                    BookingId = booking.Id,
                    Status = "Confirmed"
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<BookingHistoryDto>> GetHistoryAsync(Guid userId)
        {
            var bookings = await _bookingRepository.GetBookingsByCustomerIdAsync(userId);
            var now = DateTime.UtcNow;

            return bookings.Select(b => {
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
                    Category = b.Status == BookingStatus.Cancelled ? "Cancelled"
                             : (b.Journey?.ArrivalTime ?? DateTime.UtcNow) > now ? "Upcoming" 
                             : "Completed"
                };
            }).ToList();
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
