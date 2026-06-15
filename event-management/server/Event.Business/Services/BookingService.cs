using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using System.IO;
using Serilog;
using Event.Business.Exceptions;
using Event.Models.DTOs;
using Event.Business.Helpers;

namespace Event.Business.Services
{
    public class BookingService : IBookingService
    {
        #region Fields

        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IBookingPaymentRepository _bookingPaymentRepository;
        private readonly IPlatformSettingsRepository _settingsRepository;
        private readonly IPaymentService _paymentService;
        private readonly IQrCodeService _qrCodeService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IRefundService _refundService;

        #endregion

        #region Constructor

        public BookingService(
            IBookingRepository bookingRepository,
            IEventRepository eventRepository,
            ITransactionRepository transactionRepository,
            IBookingPaymentRepository bookingPaymentRepository,
            IPlatformSettingsRepository settingsRepository,
            IPaymentService paymentService,
            IQrCodeService qrCodeService,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IEmailService emailService,
            INotificationRepository notificationRepository,
            IRefundService refundService)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _transactionRepository = transactionRepository;
            _bookingPaymentRepository = bookingPaymentRepository;
            _settingsRepository = settingsRepository;
            _paymentService = paymentService;
            _qrCodeService = qrCodeService;
            _configuration = configuration;
            _emailService = emailService;
            _notificationRepository = notificationRepository;
            _refundService = refundService;
        }

        #endregion

        #region BookTicketsAsync

        public async Task<BookingResponse?> BookTicketsAsync(int attendeeId, int eventId, Dictionary<string, int> tierQuantities)
        {
            // 1. Validate inputs (ensure tiers are specified and quantities are positive)
            if (tierQuantities == null || !tierQuantities.Any() || tierQuantities.Values.Any(q => q <= 0))
                throw new ValidationException("Ticket tiers and non-zero positive quantities must be specified.");

            var totalRequested = tierQuantities.Values.Sum();

            // 2. Retrieve platform settings for ticket limits
            var settings = await _settingsRepository.GetSettingsAsync()
                ?? new PlatformSettings { Max_Tickets_Per_Booking = 10 };

            // 3. Verify total tickets do not exceed platform limitations
            if (totalRequested > settings.Max_Tickets_Per_Booking)
                throw new ValidationException($"Cannot book more than {settings.Max_Tickets_Per_Booking} tickets per booking.");

            // 4. Begin database transaction to ensure atomicity
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // 5. Fetch event details including ticket tiers and venue capacity details
                var ev = await _eventRepository.GetEventDetailsAsync(eventId);

                // 6. Validate event existence and live status
                if (ev == null)
                    throw new NotFoundException($"Event with ID {eventId} not found.");

                if (ev.Status != "Live")
                    throw new ValidationException("Tickets can only be booked for live events.");

                var bookingDetails = new List<BookingDetail>();
                decimal totalAmount = 0;

                // 7. Iterate through each requested ticket tier to reserve seats and calculate costs
                foreach (var item in tierQuantities)
                {
                    var tierName = item.Key;
                    var quantity = item.Value;

                    var eventTier = ev.TicketTiers.FirstOrDefault(t => t.Tier_Name.Equals(tierName, StringComparison.OrdinalIgnoreCase));
                    if (eventTier == null)
                        throw new NotFoundException($"Ticket tier '{tierName}' not found for event '{ev.Title}'.");

                    if (ev.Event_Type.Equals("Physical", StringComparison.OrdinalIgnoreCase) || 
                        ev.Event_Type.Equals("Hybrid", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ev.Venue == null)
                            throw new ValidationException("Physical event venue details are missing.");

                        var capacity = ev.Venue.SeatCapacities.FirstOrDefault(c => c.Tier_Name.Equals(tierName, StringComparison.OrdinalIgnoreCase));
                        if (capacity == null || eventTier.Tickets_Sold + quantity > capacity.Total_Seats)
                            throw new ConflictException($"Insufficient seats available for ticket tier '{tierName}'.");
                    }

                    eventTier.Tickets_Sold += quantity;
                    totalAmount += eventTier.Price * quantity;

                    bookingDetails.Add(new BookingDetail
                    {
                        Tier_Name = eventTier.Tier_Name,
                        Quantity = quantity
                    });
                }

                // 8. Construct the new Booking entity with "Payment Pending" status
                var booking = new Booking
                {
                    Attendee_Id = attendeeId,
                    Event_Id = eventId,
                    Booking_Status = "Payment Pending",
                    CheckIn_Status = "Pending",
                    Created_At = DateTime.UtcNow,
                    Virtual_Url = ev.Virtual_Url,
                    Details = bookingDetails
                };

                // 9. Save the booking details and update event tickets sold
                await _bookingRepository.AddAsync(booking);
                await _eventRepository.UpdateAsync(ev);

                // 10. Generate a ledger transaction record with "Pending" status
                var ledgerTx = new Transaction
                {
                    Sender_Id = $"Attendee_User_{attendeeId}",
                    Receiver_Id = "Platform_Escrow",
                    Transaction_Type = "BookingPayment",
                    Related_Id = booking.Booking_Id,
                    Amount = totalAmount,
                    Currency = "INR",
                    Status = "Pending",
                    Created_At = DateTime.UtcNow
                };

                await _transactionRepository.AddAsync(ledgerTx);

                // 11. Commit the transaction and return the booking details
                await _bookingRepository.CommitTransactionAsync();
                booking.Event = ev;
                return MapToBookingResponse(booking);
            }
            catch (BaseBusinessException)
            {
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex)
            {
                await _bookingRepository.RollbackTransactionAsync();
                throw new ValidationException($"Failed to book tickets. Details: {ex.Message}");
            }
        }

        #endregion

        #region ConfirmBookingPaymentAsync

        public async Task<BookingResponse?> ConfirmBookingPaymentAsync(int bookingId, string stripeChargeId, string paymentMethod)
        {
            // 1. Begin database transaction
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // 2. Retrieve booking record with associated event details
                var booking = await _bookingRepository.GetBookingDetailsAsync(bookingId);

                // 3. Validate booking existence and its pending payment status
                if (booking == null)
                    throw new NotFoundException($"Booking with ID {bookingId} not found.");

                if (booking.Booking_Status != "Payment Pending")
                    throw new ValidationException($"Booking payment status is already '{booking.Booking_Status}'.");

                // 4. Retrieve the matching pending transaction record
                var ledgerTx = await _transactionRepository.GetPendingBookingTransactionAsync(bookingId);

                if (ledgerTx == null)
                    throw new NotFoundException("Pending escrow ledger transaction not found for this booking.");

                // 5. Attempt payment processing via Stripe Payment Gateway
                var chargeResult = await _paymentService.CreateChargeAsync(ledgerTx.Amount, ledgerTx.Currency, stripeChargeId, $"Booking #{bookingId} payment");
                if (!chargeResult.Success)
                {
                    ledgerTx.Status = "Failed";
                    ledgerTx.Remarks = chargeResult.ErrorMessage;
                    await _transactionRepository.UpdateAsync(ledgerTx);
                    await _bookingRepository.CommitTransactionAsync();

                    try
                    {
                        await RevertPendingBookingAsync(bookingId);
                    }
                    catch (Exception)
                    {
                        // Ignore inner exception to propagate the original charge failed validation exception
                    }

                    throw new ValidationException($"Stripe charge failed: {chargeResult.ErrorMessage}");
                }

                // 6. Confirm the booking and generate QR authentication credentials
                booking.Booking_Status = "Confirmed";
                var secretHash = Guid.NewGuid().ToString("N");
                booking.Qr_Secret_Hash = secretHash;
                byte[] qrBytes = Array.Empty<byte>();

                try
                {
                    // Encode ONLY the raw secret hash in the QR code
                    qrBytes = await _qrCodeService.GenerateQrCodeAsync(secretHash);
                    
                    string rootPath = Directory.GetCurrentDirectory();
                    if (rootPath.Contains("bin"))
                    {
                        rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                    }

                    var ticketsDir = Path.Combine(rootPath, "assets", booking.Attendee_Id.ToString(), "bookings");
                    if (!Directory.Exists(ticketsDir))
                    {
                        Directory.CreateDirectory(ticketsDir);
                    }
                    var filePath = Path.Combine(ticketsDir, $"qr_{bookingId}.png");
                    await File.WriteAllBytesAsync(filePath, qrBytes);
                    booking.Qr_Code_Path = $"/assets/{booking.Attendee_Id}/bookings/qr_{bookingId}.png";
                }
                catch (Exception)
                {
                    // Fallback to simple path if writing fails, but log/let it complete so booking confirmation isn't blocked by filesystem issues.
                    booking.Qr_Code_Path = $"/assets/{booking.Attendee_Id}/bookings/qr_{bookingId}.png";
                }

                // 7. Update transaction status and billing references
                ledgerTx.Status = "Success";
                ledgerTx.Transaction_Reference = chargeResult.TransactionReference;
                ledgerTx.Payment_Method_Details = paymentMethod;

                // 8. Calculate platform ticket commissions and fees
                var settings = await _settingsRepository.GetSettingsAsync();
                decimal commissionPercent = settings?.Ticket_Commission_Percentage ?? 5.0m;
                decimal fixedFee = settings?.Ticket_Fixed_Fee ?? 0.99m;

                decimal platformCut = (ledgerTx.Amount * (commissionPercent / 100m)) + fixedFee;

                // 9. Save payment log and associate it with transaction ID
                var bookingPayment = new BookingPayment
                {
                    Booking_Id = bookingId,
                    Transaction_Id = ledgerTx.Transaction_Id,
                    Amount = ledgerTx.Amount,
                    Platform_Fee_Cut = platformCut,
                    Payment_Status = "Success",
                    Created_At = DateTime.UtcNow
                };

                await _bookingPaymentRepository.AddAsync(bookingPayment);
                await _bookingRepository.UpdateAsync(booking);
                await _transactionRepository.UpdateAsync(ledgerTx);

                // 9.5. Enqueue email notification to attendee
                if (booking.Attendee != null && !string.IsNullOrEmpty(booking.Attendee.Email))
                {
                    try
                    {
                        var ticketDetailsStr = string.Join(", ", booking.Details.Select(d => $"{d.Quantity}x {d.Tier_Name}"));
                        if (booking.Event?.Event_Type.Equals("Virtual", StringComparison.OrdinalIgnoreCase) == true ||
                            booking.Event?.Event_Type.Equals("Hybrid", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            string rawPasscode = "N/A";
                            var upfrontTx = await _transactionRepository.GetSuccessOrganizerUpfrontTransactionAsync(booking.Event_Id);
                            if (upfrontTx != null && !string.IsNullOrEmpty(upfrontTx.Remarks))
                            {
                                var marker = "[Virtual Access Passcode]:";
                                var idx = upfrontTx.Remarks.IndexOf(marker);
                                if (idx != -1)
                                {
                                    rawPasscode = upfrontTx.Remarks.Substring(idx + marker.Length).Trim();
                                }
                            }
                            
                            var virtualUrl = booking.Virtual_Url ?? booking.Event?.Virtual_Url;
                            ticketDetailsStr += $"<br/><span class=\"info-label\">Virtual Link:</span> <a href=\"{virtualUrl}\" style=\"color: #ffcccc; text-decoration: underline;\">{virtualUrl}</a><br/><span class=\"info-label\">Passcode:</span> {rawPasscode}";
                        }

                        var emailDto = new EmailTemplateDto
                        {
                            TemplateName = "EventBookingSuccessTemplate.html",
                            Placeholders = new Dictionary<string, string>
                            {
                                { "bookingId", booking.Booking_Id.ToString() },
                                { "eventName", booking.Event?.Title ?? "" },
                                { "totalAmount", $"INR {ledgerTx.Amount:N2}" },
                                { "ticketDetails", ticketDetailsStr },
                                { "year", DateTime.UtcNow.Year.ToString() },
                                { "qrCode", $"https://quickchart.io/qr?text={secretHash}&size=200" }
                            }
                        };
                        string htmlBody = await _emailService.BuildEmailHtmlAsync(emailDto);
                        await NotificationHelper.SendAndSaveNotificationAsync(
                            _notificationRepository,
                            _emailService,
                            booking.Attendee.Email,
                            $"Ticket Booking Confirmed: {booking.Event?.Title}",
                            htmlBody
                        );
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to send and queue success email for booking confirmation {BookingId}", booking.Booking_Id);
                    }
                }

                // 10. Commit database transaction and return the confirmed booking
                await _bookingRepository.CommitTransactionAsync();
                return MapToBookingResponse(booking);
            }
            catch (BaseBusinessException)
            {
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex)
            {
                await _bookingRepository.RollbackTransactionAsync();
                throw new ValidationException($"Failed to confirm booking payment. Details: {ex.Message}");
            }
        }

        #endregion

        #region GetMyBookingsAsync

        public async Task<IEnumerable<BookingResponse>> GetMyBookingsAsync(int attendeeId, string? status = null)
        {
            var bookings = await _bookingRepository.GetBookingsByUserIdAsync(attendeeId);
            
            if (!string.IsNullOrWhiteSpace(status))
            {
                bookings = bookings.Where(b => b.Booking_Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            return bookings.Select(MapToBookingResponse).Where(x => x != null).Cast<BookingResponse>().ToList();
        }

        #endregion

        #region CancelBookingAsync

        public async Task<bool> CancelBookingAsync(int bookingId, string refundType = "Dynamic")
        {
            // 1. Begin database transaction
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // 2. Fetch booking details along with event ticket tiers
                var booking = await _bookingRepository.GetBookingDetailsAsync(bookingId);

                // 3. Validate booking existence and cancelable status
                if (booking == null)
                    throw new NotFoundException($"Booking with ID {bookingId} not found.");

                if (booking.Booking_Status == "Cancelled")
                    throw new ValidationException("This booking is already cancelled.");

                // 4. Release reserved tickets back to available seat pool
                if (booking.Event?.TicketTiers != null)
                {
                    foreach (var detail in booking.Details)
                    {
                        var eventTier = booking.Event.TicketTiers
                            .FirstOrDefault(t => t.Tier_Name.Equals(detail.Tier_Name, StringComparison.OrdinalIgnoreCase));
                        if (eventTier != null)
                        {
                            eventTier.Tickets_Sold = Math.Max(0, eventTier.Tickets_Sold - detail.Quantity);
                        }
                    }
                    await _eventRepository.UpdateAsync(booking.Event);
                }

                // 5. Calculate and process refund via RefundService
                var (refundAmountVal, remarksVal) = await _refundService.RefundAttendeeAsync(bookingId, refundType);

                // 6. Persist changes to database and complete database transaction
                await _bookingRepository.CommitTransactionAsync();
                return true;
            }
            catch (BaseBusinessException)
            {
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
            catch (Exception ex)
            {
                await _bookingRepository.RollbackTransactionAsync();
                throw new ValidationException($"Failed to cancel booking. Details: {ex.Message}");
            }
        }

        #endregion

        #region ReleaseExpiredEventBookingAsync

        public async Task ReleaseExpiredEventBookingAsync()
        {
            // Step 1: Configure Serilog file logger pointing to logs/business.log with daily rollover.
            var logger = new Serilog.LoggerConfiguration()
                .WriteTo.File("logs/business.log", rollingInterval: Serilog.RollingInterval.Day)
                .CreateLogger();

            // Step 2: Establish the 5-minute expiration cutoff timestamp.
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
            logger.Information("ReleaseExpiredEventBookingAsync job started at {Time}. Cutoff time is {CutoffTime}.", DateTime.UtcNow, cutoffTime);

            try
            {
                // Step 3: Fetch all unconfirmed bookings matching the cutoff filter.
                var expiredBookings = await _bookingRepository.GetExpiredBookingsAsync(cutoffTime);
                int count = 0;

                // Step 4: Iterate through each expired booking record and revert changes.
                foreach (var booking in expiredBookings)
                {
                    try
                    {
                        logger.Information("Expiring Booking ID {BookingId} for Attendee {AttendeeId}.", booking.Booking_Id, booking.Attendee_Id);
                        await RevertPendingBookingAsync(booking.Booking_Id);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to release expired Booking ID {BookingId}.", booking.Booking_Id);
                    }
                }

                logger.Information("ReleaseExpiredEventBookingAsync job completed. Total bookings expired: {Count}.", count);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "An error occurred during the ReleaseExpiredEventBookingAsync background job execution.");
            }
        }

        #endregion

        #region RevertPendingBookingAsync

        public async Task<bool> RevertPendingBookingAsync(int bookingId)
        {
            // Step 1: Start transaction.
            await _bookingRepository.BeginTransactionAsync();
            try
            {
                // Step 2: Retrieve booking details.
                var booking = await _bookingRepository.GetBookingDetailsAsync(bookingId);
                if (booking == null)
                    throw new NotFoundException($"Booking with ID {bookingId} not found.");

                // Step 3: Validate booking is in "Payment Pending" status.
                if (booking.Booking_Status != "Payment Pending")
                    throw new ValidationException($"Booking cannot be reverted. Current status is '{booking.Booking_Status}'.");

                // Step 4: Release reserved ticket capacity.
                if (booking.Event?.TicketTiers != null)
                {
                    foreach (var detail in booking.Details)
                    {
                        var eventTier = booking.Event.TicketTiers
                            .FirstOrDefault(t => t.Tier_Name.Equals(detail.Tier_Name, StringComparison.OrdinalIgnoreCase));
                        if (eventTier != null)
                        {
                            eventTier.Tickets_Sold = Math.Max(0, eventTier.Tickets_Sold - detail.Quantity);
                        }
                    }
                    await _eventRepository.UpdateAsync(booking.Event);
                }

                // Step 5: Update booking status to Payment Failed.
                booking.Booking_Status = "Payment Failed";
                await _bookingRepository.UpdateAsync(booking);

                // Step 6: Find and mark the pending transaction as Failed.
                var transaction = await _transactionRepository.GetPendingBookingTransactionAsync(bookingId);
                if (transaction != null)
                {
                    transaction.Status = "Failed";
                    transaction.Remarks = "Booking payment reverted/cancelled by user.";
                    await _transactionRepository.UpdateAsync(transaction);
                }

                // Step 7: Commit transaction.
                await _bookingRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception)
            {
                // Step 8: Rollback transaction.
                await _bookingRepository.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private BookingResponse? MapToBookingResponse(Booking? booking)
        {
            if (booking == null) return null;

            return new BookingResponse
            {
                Booking_Id = booking.Booking_Id,
                Attendee_Id = booking.Attendee_Id,
                Event_Id = booking.Event_Id,
                Event_Title = booking.Event?.Title ?? string.Empty,
                Event_Type = booking.Event?.Event_Type ?? string.Empty,
                Event_Date_Time = booking.Event?.Date_Time ?? DateTime.MinValue,
                Booking_Status = booking.Booking_Status,
                Qr_Code_Path = booking.Qr_Code_Path,
                CheckIn_Status = booking.CheckIn_Status,
                Created_At = booking.Created_At,
                Virtual_Url = booking.Virtual_Url ?? booking.Event?.Virtual_Url,
                Virtual_Password_Hash = booking.Event?.Virtual_Password_Hash,
                Details = booking.Details?.Select(d => new BookingDetailDto
                {
                    Tier_Name = d.Tier_Name,
                    Quantity = d.Quantity
                }).ToList() ?? new List<BookingDetailDto>()
            };
        }

        #endregion

    }
}
