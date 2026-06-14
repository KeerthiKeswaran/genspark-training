using System;
using System.Threading.Tasks;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using System.Linq;
using Serilog;
using Event.Business.Exceptions;
using Event.Models.DTOs;
using System.Collections.Generic;
using Event.Business.Helpers;

namespace Event.Business.Services
{
    public class EventService : IEventService
    {
        #region Fields

        private readonly IEventRepository _eventRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IVenueRepository _venueRepository;
        private readonly IPlatformSettingsRepository _settingsRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IPaymentService _paymentService;
        private readonly IOrganizerUpfrontPaymentRepository _upfrontPaymentRepository;
        private readonly IVirtualMeetingService _virtualMeetingService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IBookingPaymentRepository _bookingPaymentRepository;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IRefundService _refundService;

        // Thread-safe in-memory cache for calculations (temporary memory store)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Event.Models.DTOs.StaffAvailabilityResponse> _staffCache = 
            new System.Collections.Concurrent.ConcurrentDictionary<string, Event.Models.DTOs.StaffAvailabilityResponse>();

        #endregion

        #region Constructor

        public EventService(
            IEventRepository eventRepository,
            IBookingRepository bookingRepository,
            IVenueRepository venueRepository,
            IPlatformSettingsRepository settingsRepository,
            IStaffRepository staffRepository,
            ITransactionRepository transactionRepository,
            IPaymentService paymentService,
            IOrganizerUpfrontPaymentRepository upfrontPaymentRepository,
            IVirtualMeetingService virtualMeetingService,
            INotificationRepository notificationRepository,
            IBookingPaymentRepository bookingPaymentRepository,
            IEmailService emailService,
            IUserRepository userRepository,
            IRefundService refundService)
        {
            _eventRepository = eventRepository;
            _bookingRepository = bookingRepository;
            _venueRepository = venueRepository;
            _settingsRepository = settingsRepository;
            _staffRepository = staffRepository;
            _transactionRepository = transactionRepository;
            _paymentService = paymentService;
            _upfrontPaymentRepository = upfrontPaymentRepository;
            _virtualMeetingService = virtualMeetingService;
            _notificationRepository = notificationRepository;
            _bookingPaymentRepository = bookingPaymentRepository;
            _emailService = emailService;
            _userRepository = userRepository;
            _refundService = refundService;
        }

        #endregion

        #region BrowseEventsAsync

        public async Task<PagedResult<Event.Models.Event>> BrowseEventsAsync(string? keyword, string? category, DateTime? minDateTime, string? regionId, int page, int size)
        {
            // 1. Calculate the cutoff search time (must be at least 30 minutes in the future)
            var cutoffTime = DateTime.UtcNow.AddMinutes(30);
            var searchMinTime = minDateTime.HasValue && minDateTime.Value > cutoffTime 
                ? minDateTime.Value 
                : cutoffTime;

            // 2. Query event repository for paged results matching filters
            return await _eventRepository.SearchEventsAsync(keyword, category, searchMinTime, regionId, page, size);
        }

        #endregion

        #region GetEventDetailsAsync

        public async Task<Event.Models.Event?> GetEventDetailsAsync(int eventId)
        {
            // 1. Fetch details with eager loading of tiers and venue/seat capacities
            var ev = await _eventRepository.GetEventDetailsAsync(eventId);

            // 2. Validate that event exists
            if (ev == null)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            return ev;
        }

        #endregion

        #region ReportEventAsync

        public async Task<bool> ReportEventAsync(int reporterId, int eventId, string reason)
        {
            // 1. Verify event existence
            var evExists = await _eventRepository.ExistsAsync(eventId);
            if (!evExists)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            // 2. Create and save new event report
            var eventReport = new EventReport
            {
                Event_Id = eventId,
                Reporter_Id = reporterId,
                Reason = reason,
                Created_At = DateTime.UtcNow
            };
            await _eventRepository.AddReportAsync(eventReport);
            return true;
        }

        #endregion

        #region SubmitEventFeedbackAsync

        public async Task<bool> SubmitEventFeedbackAsync(int attendeeId, int eventId, int rating, string review)
        {
            // 1. Verify event existence
            var evExists = await _eventRepository.ExistsAsync(eventId);
            if (!evExists)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            // 2. Create and save new attendee feedback
            var feedback = new EventFeedback
            {
                Attendee_Id = attendeeId,
                Event_Id = eventId,
                Rating = rating,
                Review = review
            };
            await _eventRepository.AddFeedbackAsync(feedback);
            return true;
        }

        #endregion

        #region VerifyTicketCheckInAsync

        public async Task<Booking> VerifyTicketCheckInAsync(string secretHash)
        {
            if (string.IsNullOrWhiteSpace(secretHash))
                throw new ValidationException("Secret hash is required.");

            var booking = await _bookingRepository.GetBookingBySecretHashAsync(secretHash);
            if (booking == null)
                throw new NotFoundException("Booking not found for the provided QR code.");

            if (booking.Booking_Status != "Confirmed")
                throw new ValidationException($"Cannot check in. Booking status is '{booking.Booking_Status}'.");

            if (booking.CheckIn_Status == "Checked-In")
                throw new ValidationException("This ticket has already been checked in.");

            booking.CheckIn_Status = "Checked-In";
            await _bookingRepository.UpdateAsync(booking);
            return booking;
        }

        #endregion

        #region CheckStaffAvailabilityAsync

        public async Task<Event.Models.DTOs.StaffAvailabilityResponse> CheckStaffAvailabilityAsync(Event.Models.DTOs.CheckStaffAvailabilityRequest request)
        {
            var venue = await _venueRepository.GetByIdAsync(request.VenueId);
            if (venue == null || !venue.Is_Available)
                throw new NotFoundException("Venue not found or is currently unavailable.");

            var settings = await _settingsRepository.GetSettingsAsync()
                ?? throw new ValidationException("Platform settings are not configured.");

            int requiredStaff = CalculateRequiredStaffCount(venue);
            int availableStaff = await _staffRepository.GetAvailableStaffCountAsync(venue.Region_Id, request.DateTime);

            var response = new Event.Models.DTOs.StaffAvailabilityResponse
            {
                RequiredStaffCount = requiredStaff,
                AvailableStaffCount = availableStaff
            };

            // Rule 1: If available staff is less than 2, return 0 staff (none available)
            if (availableStaff < 2)
            {
                response.AvailableStaffCount = 0;
                response.StaffingCost = 0;
                response.IsAdequate = false;
                response.Message = "No support staff are available (minimum pool requirement of 2 staff not met).";
            }
            // Rule 2: If available is more than 2, but still less than the requirement
            else if (availableStaff < requiredStaff)
            {
                response.StaffingCost = settings.Staff_Flat_Rate * availableStaff;
                response.IsAdequate = false;
                response.Message = $"Partial staff available ({availableStaff} of {requiredStaff} required).";
            }
            else
            {
                response.StaffingCost = settings.Staff_Flat_Rate * requiredStaff;
                response.IsAdequate = true;
                response.Message = "Sufficient support staff are available.";
            }

            // Cache the result in temporary memory
            string cacheKey = $"{request.VenueId}_{request.DateTime:yyyyMMddHHmmss}";
            _staffCache[cacheKey] = response;

            return response;
        }

        #endregion

        #region CreateEventAsync

        public async Task<Event.Models.Event> CreateEventAsync(int organizerId, Event.Models.DTOs.CreateEventRequest request)
        {
            // 0. Validation: Policy acceptance
            if (!request.HasAcceptedPolicy)
                throw new ValidationException("You must accept the policy agreement before creating an event.");

            // 1. Validation: Event type
            if (string.IsNullOrWhiteSpace(request.EventType))
                throw new ValidationException("Event type is required.");

            // 2. Validation: Event date must be at least 24 hours in the future
            if (request.DateTime < DateTime.UtcNow.AddHours(24))
                throw new ValidationException("Events must be scheduled at least 24 hours in the future.");

            // Check organizer status
            var organizer = await _userRepository.GetByIdAsync(organizerId);
            if (organizer == null)
                throw new NotFoundException($"Organizer with ID {organizerId} not found.");

            if (string.Equals(organizer.Status, "Restricted", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Your account is Restricted. You are disabled from creating further events.");
            
            if (string.Equals(organizer.Status, "Deactivated", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Your account is Deactivated.");

            // 3. Retrieve platform settings
            var settings = await _settingsRepository.GetSettingsAsync()
                ?? throw new ValidationException("Platform settings are not configured.");

            decimal upfrontFee = 0;
            Venue? venue = null;

            // 4. Physical / Hybrid validation
            if (request.EventType.Equals("Physical", StringComparison.OrdinalIgnoreCase) ||
                request.EventType.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
            {
                if (!request.VenueId.HasValue)
                    throw new ValidationException("Venue ID is required for Physical or Hybrid events.");

                venue = await _venueRepository.GetByIdAsync(request.VenueId.Value);
                if (venue == null || !venue.Is_Available)
                    throw new NotFoundException("Venue not found or is currently unavailable.");

                // Check venue occupancy
                bool isOccupied = await _venueRepository.IsVenueOccupiedAsync(venue.Venue_Id, request.DateTime);
                if (isOccupied)
                    throw new ConflictException("The selected venue is already booked for this date and time.");

                // Calculate venue rental cost
                decimal venueCost = venue.Hourly_Price * request.DurationHours;
                upfrontFee += settings.Physical_Event_Activation_Fee + venueCost;

                // Check and calculate staff availability if requested by the organizer
                if (request.RequiresStaff)
                {
                    // Look up from temporary cache memory first, otherwise compute
                    string cacheKey = $"{request.VenueId.Value}_{request.DateTime:yyyyMMddHHmmss}";
                    if (_staffCache.TryGetValue(cacheKey, out var cachedResult))
                    {
                        if (cachedResult.AvailableStaffCount == 0)
                        {
                            throw new ConflictException("Cannot book staff. No support staff are available in the region.");
                        }
                        
                        // Add staff cost based on actual allocated or available count (or whatever we can allocate)
                        upfrontFee += cachedResult.StaffingCost;
                    }
                    else
                    {
                        int requiredStaffCount = CalculateRequiredStaffCount(venue);
                        int availableStaffCount = await _staffRepository.GetAvailableStaffCountAsync(venue.Region_Id, request.DateTime);
                        
                        if (availableStaffCount < 2)
                        {
                            throw new ConflictException("Cannot book staff. No support staff are available in the region.");
                        }

                        int allocatedCount = Math.Min(requiredStaffCount, availableStaffCount);
                        upfrontFee += settings.Staff_Flat_Rate * allocatedCount;
                    }
                }
            }
            else if (request.EventType.Equals("Virtual", StringComparison.OrdinalIgnoreCase))
            {
                upfrontFee += settings.Virtual_Event_Activation_Fee;
            }
            else
            {
                throw new ValidationException("Invalid event type. Must be Physical, Virtual, or Hybrid.");
            }

            // Begin transaction
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // Create Event entity (Starts as 'Activation Pending' until upfront payment is completed)
                var newEvent = new Event.Models.Event
                {
                    Organizer_Id = organizerId,
                    Venue_Id = request.VenueId,
                    Event_Type = request.EventType,
                    Title = request.Title,
                    Description_Url = request.DescriptionUrl,
                    Image_Url = request.ImageUrl,
                    Date_Time = request.DateTime,
                    Duration_Hours = request.DurationHours,
                    Status = "Activation Pending",
                    Requires_Staff = request.RequiresStaff,
                    Virtual_Url = request.VirtualUrl,
                    Virtual_Password_Hash = !string.IsNullOrEmpty(request.VirtualPassword) 
                        ? BCrypt.Net.BCrypt.HashPassword(request.VirtualPassword) 
                        : null
                };

                // Add ticket tiers
                foreach (var tier in request.TicketTiers)
                {
                    newEvent.TicketTiers.Add(new EventTicketTier
                    {
                        Tier_Name = tier.TierName,
                        Price = tier.Price,
                        Tickets_Sold = 0
                    });
                }

                await _eventRepository.AddAsync(newEvent);

                // Create ledger transaction for upfront payment
                var transaction = new Transaction
                {
                    Sender_Id = $"Organizer_User_{organizerId}",
                    Receiver_Id = "Platform_Escrow",
                    Transaction_Type = "OrganizerUpfrontPayment",
                    Related_Id = newEvent.Event_Id,
                    Amount = upfrontFee,
                    Currency = "USD",
                    Status = "Pending",
                    Created_At = DateTime.UtcNow,
                    Remarks = $"Upfront payment for publishing Event '{request.Title}'"
                };

                await _transactionRepository.AddAsync(transaction);

                await _bookingRepository.CommitTransactionAsync();
                return newEvent;
            }
            catch (Exception)
            {
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region ConfirmEventUpfrontPaymentAsync

        public async Task<Event.Models.Event> ConfirmEventUpfrontPaymentAsync(int eventId, string stripeChargeId, string paymentMethod)
        {
            // Step 1: Start a new database transaction to guarantee database consistency and atomicity.
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // Step 2: Retrieve the event details, eager loading the venue, capacities, and ticket tiers.
                var ev = await _eventRepository.GetEventDetailsAsync(eventId);
                if (ev == null)
                    throw new NotFoundException($"Event with ID {eventId} not found.");

                // Step 3: Validate that the event status is currently in 'Activation Pending' status.
                if (ev.Status != "Activation Pending")
                    throw new ValidationException($"Event is already in '{ev.Status}' status.");

                // Step 4: Fetch the associated pending upfront payment transaction from the database ledger.
                var transaction = await _transactionRepository.GetPendingOrganizerUpfrontTransactionAsync(eventId);
                if (transaction == null)
                    throw new NotFoundException("Pending upfront payment transaction not found for this event.");

                // Step 5: Charge the organizer's card using the payment gateway service (Stripe).
                var chargeResult = await _paymentService.CreateChargeAsync(
                    transaction.Amount,
                    transaction.Currency,
                    stripeChargeId,
                    $"Organizer Upfront Payment for Event #{eventId}: {ev.Title}");

                // Step 6: Handle payment failures by updating transaction logs and committing the failed state.
                if (!chargeResult.Success)
                {
                    transaction.Status = "Failed";
                    transaction.Remarks = chargeResult.ErrorMessage;
                    await _transactionRepository.UpdateAsync(transaction);
                    await _bookingRepository.CommitTransactionAsync();
                    throw new ValidationException($"Stripe charge failed: {chargeResult.ErrorMessage}");
                }

                // Step 7: Update the transaction log to record successful checkout parameters.
                transaction.Status = "Success";
                transaction.Transaction_Reference = chargeResult.TransactionReference;
                transaction.Payment_Method_Details = paymentMethod;
                await _transactionRepository.UpdateAsync(transaction);

                // Step 8: Log a successful record in the OrganizerUpfrontPayments mapping table.
                var upfrontPayment = new OrganizerUpfrontPayment
                {
                    Event_Id = eventId,
                    Transaction_Id = transaction.Transaction_Id,
                    Amount = transaction.Amount,
                    Payment_Status = "Success",
                    Created_At = DateTime.UtcNow
                };
                await _upfrontPaymentRepository.AddAsync(upfrontPayment);

                // Step 9: Set the main Event Status to 'Live' indicating that it is active and viewable by users.
                ev.Status = "Live";
                
                // Step 10: For Physical or Hybrid events, allocate support staffs.
                if (ev.Event_Type.Equals("Physical", StringComparison.OrdinalIgnoreCase) ||
                    ev.Event_Type.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
                {
                    if (ev.Requires_Staff && ev.Venue != null)
                    {
                        // Calculate required staff using the helper method
                        int requiredStaffCount = CalculateRequiredStaffCount(ev.Venue);

                        // Query available support staff members in that region
                        var availableStaffs = await _staffRepository.GetAvailableStaffsAsync(ev.Venue.Region_Id, ev.Date_Time);
                        var staffsToAllocate = availableStaffs.Take(requiredStaffCount).ToList();

                        // Save allocations and update staff availability status
                        foreach (var staff in staffsToAllocate)
                        {
                            ev.StaffAllocations.Add(new EventStaffAllocation
                            {
                                Event_Id = ev.Event_Id,
                                Employee_ID = staff.Employee_ID
                            });

                            staff.IsAllocated = true;
                            await _staffRepository.UpdateAsync(staff);
                        }
                    }
                }

                // Step 11: For Virtual or Hybrid events, generate a Jitsi Meet meeting link and password hash.
                string? generatedPasscode = null;
                if (ev.Event_Type.Equals("Virtual", StringComparison.OrdinalIgnoreCase) ||
                    ev.Event_Type.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
                {
                    var (roomUrl, rawPasscode) = await _virtualMeetingService.GenerateMeetingRoomAsync(ev.Title);
                    ev.Virtual_Url = roomUrl;
                    ev.Virtual_Password_Hash = BCrypt.Net.BCrypt.HashPassword(rawPasscode);
                    generatedPasscode = rawPasscode;
                    
                    transaction.Remarks += $"\n[Virtual Access Passcode]: {rawPasscode}";
                    await _transactionRepository.UpdateAsync(transaction);
                }

                // Step 12: Persist all updated properties on the Event object.
                await _eventRepository.UpdateAsync(ev);

                // Step 12.5: Enqueue email notification to organizer
                if (ev.Organizer != null && !string.IsNullOrEmpty(ev.Organizer.Email))
                {
                    try
                    {
                        string locationDetails = "";
                        string linkStyle = "color: #ffffff; text-decoration: underline;";
                        string virtualInfo = $"<a href='{ev.Virtual_Url}' style='{linkStyle}'>{ev.Virtual_Url}</a> <br/> Password: {generatedPasscode} <br/> <em>(Please maintain this password confidential)</em>";

                        if (ev.Event_Type.Equals("Virtual", StringComparison.OrdinalIgnoreCase))
                            locationDetails = ev.Virtual_Url != null ? virtualInfo : "Link TBD";
                        else if (ev.Event_Type.Equals("Physical", StringComparison.OrdinalIgnoreCase))
                            locationDetails = ev.Venue?.Name ?? "Venue TBD";
                        else if (ev.Event_Type.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
                            locationDetails = $"{ev.Venue?.Name ?? "Venue TBD"} (Physical) <br/> {(ev.Virtual_Url != null ? virtualInfo : "Link TBD")} (Virtual)";

                        var emailDto = new EmailTemplateDto
                        {
                            TemplateName = "EventCreationSuccessTemplate.html",
                            Placeholders = new Dictionary<string, string>
                            {
                                { "title", ev.Title },
                                { "dateTime", ev.Date_Time.ToString("f") },
                                { "eventType", ev.Event_Type },
                                { "locationDetails", locationDetails },
                                { "upfrontFee", transaction.Amount.ToString("C") },
                                { "year", DateTime.UtcNow.Year.ToString() }
                            }
                        };
                        string htmlBody = await _emailService.BuildEmailHtmlAsync(emailDto);
                        await NotificationHelper.SendAndSaveNotificationAsync(
                            _notificationRepository,
                            _emailService,
                            ev.Organizer.Email,
                            $"Event Activated Successfully: {ev.Title}",
                            htmlBody
                        );
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to send and queue success email for event creation {EventId}", ev.Event_Id);
                    }
                }

                // Step 13: Commit the database transaction to apply all modifications atomically.
                await _bookingRepository.CommitTransactionAsync();

                return ev;
            }
            catch (Exception)
            {
                // Step 14: Roll back the transaction if any processing step throws an exception.
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region ReleaseExpiredEventCreationAsync

        public async Task ReleaseExpiredEventCreationAsync()
        {
            // Step 1: Configure Serilog file logger pointing to logs/business.log.
            var logger = new Serilog.LoggerConfiguration()
                .WriteTo.File("logs/business.log", rollingInterval: Serilog.RollingInterval.Day)
                .CreateLogger();

            // Step 2: Establish the 5-minute event creation expiration cutoff.
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
            logger.Information("ReleaseExpiredEventCreationAsync job started at {Time}. Cutoff time is {CutoffTime}.", DateTime.UtcNow, cutoffTime);

            try
            {
                // Step 3: Fetch all expired 'Activation Pending' events from database.
                var expiredEvents = await _eventRepository.GetExpiredEventsAsync(cutoffTime);
                int count = 0;

                // Step 4: Loop through each expired event to roll back settings.
                foreach (var ev in expiredEvents)
                {
                    try
                    {
                        logger.Information("Expiring unconfirmed Event ID {EventId}: '{Title}' created by Organizer ID {OrganizerId}.", ev.Event_Id, ev.Title, ev.Organizer_Id);
                        await RevertPendingEventCreationAsync(ev.Event_Id);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to release expired Event ID {EventId}.", ev.Event_Id);
                    }
                }

                logger.Information("ReleaseExpiredEventCreationAsync job completed. Total events expired: {Count}.", count);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "An error occurred during the ReleaseExpiredEventCreationAsync background job execution.");
            }
        }

        #endregion

        #region CancelEventAsync

        public async Task<bool> CancelEventAsync(int eventId, string refundType = "Dynamic", string cancellationMessage = "We regret to inform you that the event you booked has been cancelled by the organizer.")
        {
            // Step 1: Start database transaction boundaries.
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // Step 2: Retrieve event details (with ticket tiers and venues).
                var ev = await _eventRepository.GetEventDetailsAsync(eventId);
                if (ev == null)
                    throw new NotFoundException($"Event with ID {eventId} not found.");

                // Step 3: Prevent duplicate cancellations.
                if (ev.Status == "Cancelled")
                    throw new ValidationException("Event is already cancelled.");

                // Step 4: Process organizer and attendee refunds using RefundService
                var (organizerRefundAmount, organizerRemarks, attendeeRefundResults) = await _refundService.RefundOrganizerAsync(eventId, refundType);

                // Step 5: Retrieve all bookings made for this event.
                var bookings = await _bookingRepository.GetBookingsByEventIdAsync(eventId);

                // Step 6: Release and free up allocated support staff members.
                foreach (var allocation in ev.StaffAllocations)
                {
                    var staff = await _staffRepository.GetByIdAsync(allocation.Employee_ID);
                    if (staff != null)
                    {
                        staff.IsAllocated = false;
                        await _staffRepository.UpdateAsync(staff);
                    }
                }

                // Step 7: Commit transaction to apply changes.
                await _bookingRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                // Step 8: Roll back on exception.
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region RevertPendingEventCreationAsync

        public async Task<bool> RevertPendingEventCreationAsync(int eventId)
        {
            // Step 1: Start transaction for database safety.
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // Step 2: Fetch the event.
                var ev = await _eventRepository.GetEventDetailsAsync(eventId);
                if (ev == null)
                    throw new NotFoundException($"Event with ID {eventId} not found.");

                // Step 3: Validate that the event is in "Activation Pending" status.
                if (ev.Status != "Activation Pending")
                    throw new ValidationException($"Event cannot be reverted. Current status is '{ev.Status}'.");

                // Step 4: Find the associated pending activation payment ledger record and set it to Failed.
                var transaction = await _transactionRepository.GetPendingOrganizerUpfrontTransactionAsync(eventId);
                if (transaction != null)
                {
                    transaction.Status = "Failed";
                    transaction.Remarks = "Event creation pending payment reverted/cancelled by the organizer.";
                    await _transactionRepository.UpdateAsync(transaction);
                }

                // Step 5: Soft delete by setting event status to Failed.
                ev.Status = "Failed";
                await _eventRepository.UpdateAsync(ev);

                // Step 6: Commit transaction.
                await _bookingRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                // Step 7: Rollback on error.
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region CalculateRequiredStaffCount

        private int CalculateRequiredStaffCount(Venue venue)
        {
            int totalSeats = venue.SeatCapacities.Sum(c => c.Total_Seats);
            // Standard rule: 1 staff per 100 seats, minimum 1 staff
            return Math.Max(1, (int)Math.Ceiling(totalSeats / 100.0));
        }

        #endregion



        #region GetEventsByInterestedRegionsAsync

        public async Task<System.Collections.Generic.IEnumerable<Event.Models.Event>> GetEventsByInterestedRegionsAsync(int userId)
        {
            var user = await _userRepository.GetUserProfileAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User not found.");
            }

            var regionIds = user.InterestedRegions.Select(r => r.Region_Id).ToList();
            if (!regionIds.Any())
            {
                return new List<Event.Models.Event>();
            }

            return await _eventRepository.GetEventsByRegionsAsync(regionIds);
        }

        #endregion
    }
}
