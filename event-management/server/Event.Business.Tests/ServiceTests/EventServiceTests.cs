using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Event.Models;
using Event.Models.DTOs;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Services;
using Event.Business.Exceptions;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class EventServiceTests : ServiceTestBase
    {
        private Mock<IEventRepository> _eventRepositoryMock = null!;
        private Mock<IBookingRepository> _bookingRepositoryMock = null!;
        private Mock<IVenueRepository> _venueRepositoryMock = null!;
        private Mock<IPlatformSettingsRepository> _settingsRepositoryMock = null!;
        private Mock<IStaffRepository> _staffRepositoryMock = null!;
        private Mock<ITransactionRepository> _transactionRepositoryMock = null!;
        private IPaymentService _paymentService = null!;
        private Mock<IOrganizerUpfrontPaymentRepository> _upfrontPaymentRepositoryMock = null!;
        private Mock<INotificationRepository> _notificationRepositoryMock = null!;
        private Mock<IBookingPaymentRepository> _bookingPaymentRepositoryMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private IRefundService _refundService = null!;

        private IConfiguration _configuration = null!;
        private IEmailService _emailService = null!;
        private IVirtualMeetingService _virtualMeetingService = null!;
        private EventService _eventService = null!;

        private const string Service = "EventService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";
        private const string TestName = "KeerthiKeswaran";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _eventRepositoryMock = new Mock<IEventRepository>();
            _bookingRepositoryMock = new Mock<IBookingRepository>();
            _venueRepositoryMock = new Mock<IVenueRepository>();
            _settingsRepositoryMock = new Mock<IPlatformSettingsRepository>();
            _staffRepositoryMock = new Mock<IStaffRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _upfrontPaymentRepositoryMock = new Mock<IOrganizerUpfrontPaymentRepository>();
            _notificationRepositoryMock = new Mock<INotificationRepository>();
            _bookingPaymentRepositoryMock = new Mock<IBookingPaymentRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => new User { User_Id = id, Name = TestName, Email = TestEmail, Status = "Active" });

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string apiDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Event.API"));
            string appSettingsPath = Path.Combine(apiDir, "appsettings.json");

            _configuration = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
                .Build();

            // _emailService = CreateConcreteEmailService(_configuration);
            // _paymentService = CreateConcretePaymentService(_configuration);
            // _virtualMeetingService = CreateConcreteVirtualMeetingService();
            _emailService = CreateMockEmailService();
            _paymentService = CreateMockPaymentService();
            _virtualMeetingService = CreateMockVirtualMeetingService();

            _notificationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);

            _notificationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);

            _refundService = new RefundService(
                _bookingRepositoryMock.Object,
                _eventRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _bookingPaymentRepositoryMock.Object,
                _paymentService,
                new Mock<IServiceProvider>().Object,
                _emailService,
                _notificationRepositoryMock.Object
            );

            _eventService = new EventService(
                _eventRepositoryMock.Object,
                _bookingRepositoryMock.Object,
                _venueRepositoryMock.Object,
                _settingsRepositoryMock.Object,
                _staffRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _paymentService,
                _upfrontPaymentRepositoryMock.Object,
                _virtualMeetingService,
                _notificationRepositoryMock.Object,
                _bookingPaymentRepositoryMock.Object,
                _emailService,
                _userRepositoryMock.Object,
                _refundService
            );
        }
        #endregion

        #region Test_CreateEventAsync_Virtual_Success
        [Test]
        public async Task Test_CreateEventAsync_Virtual_Success()
        {
            var request = new CreateEventRequest
            {
                Title = "Dev Meetup",
                DescriptionUrl = "A meeting for devs",
                DateTime = DateTime.UtcNow.AddDays(2),
                DurationHours = 2,
                EventType = "Virtual",
                HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest>
                {
                    new CreateTicketTierRequest { TierName = "Free", Price = 0.00m }
                }
            };

            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Virtual_Event_Activation_Fee = 0 });
            _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);

            try
            {
                var result = await _eventService.CreateEventAsync(10001, request);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Status, Is.EqualTo("Activation Pending"));
                LogTestDetail(Service, "CreateEventAsync", "Successful creation of virtual event", request, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreateEventAsync", "Successful creation of virtual event", request, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_CreateEventAsync_Physical_Success
        [Test]
        public async Task Test_CreateEventAsync_Physical_Success()
        {
            var request = new CreateEventRequest
            {
                Title = "Physical Meetup",
                DescriptionUrl = "A real meeting",
                DateTime = DateTime.UtcNow.AddDays(3),
                DurationHours = 3,
                EventType = "Physical",
                VenueId = 10001,
                RequiresStaff = false,
                HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest>
                {
                    new CreateTicketTierRequest { TierName = "General", Price = 10.00m }
                }
            };

            var mockVenue = new Venue
            {
                Venue_Id = 10001,
                Name = "Auditorium A",
                Is_Available = true,
                Hourly_Price = 50.00m
            };

            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Physical_Event_Activation_Fee = 100.00m });
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(10001)).ReturnsAsync(mockVenue);
            _venueRepositoryMock.Setup(r => r.IsVenueOccupiedAsync(10001, It.IsAny<DateTime>())).ReturnsAsync(false);
            _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);

            try
            {
                var result = await _eventService.CreateEventAsync(10001, request);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Status, Is.EqualTo("Activation Pending"));
                LogTestDetail(Service, "CreateEventAsync", "Successful creation of physical event", request, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreateEventAsync", "Successful creation of physical event", request, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_CreateEventAsync_PolicyNotAccepted_ThrowsValidationException
        [Test]
        public void Test_CreateEventAsync_PolicyNotAccepted_ThrowsValidationException()
        {
            var request = new CreateEventRequest
            {
                Title = "Physical Meetup",
                DescriptionUrl = "A real meeting",
                DateTime = DateTime.UtcNow.AddDays(3),
                DurationHours = 3,
                EventType = "Physical",
                VenueId = 10001,
                RequiresStaff = false,
                HasAcceptedPolicy = false,
                TicketTiers = new List<CreateTicketTierRequest>
                {
                    new CreateTicketTierRequest { TierName = "General", Price = 10.00m }
                }
            };

            Assert.ThrowsAsync<ValidationException>(async () =>
            {
                await _eventService.CreateEventAsync(10001, request);
            });
        }
        #endregion

        #region Test_GetEventDetailsAsync_Success
        [Test]
        public async Task Test_GetEventDetailsAsync_Success()
        {
            var mockEvent = new Event.Models.Event { Event_Id = 10010, Title = "Tech Gala", Organizer_Id = 10001, Organizer = new User { User_Id = 10001, Name = "Mock User", Email = "mock@example.com" } };
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10010)).ReturnsAsync(mockEvent);

            try
            {
                var result = await _eventService.GetEventDetailsAsync(10010);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Title, Is.EqualTo("Tech Gala"));
                LogTestDetail(Service, "GetEventDetailsAsync", "Retrieve event details", 10010, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetEventDetailsAsync", "Retrieve event details", 10010, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_BrowseEventsAsync_Success
        [Test]
        public async Task Test_BrowseEventsAsync_Success()
        {
            var mockEvents = new List<Event.Models.Event>
            {
                new Event.Models.Event { Event_Id = 10001, Title = "Event One" }
            };
            var pagedResult = new PagedResult<Event.Models.Event>
            {
                Items = mockEvents,
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };
            _eventRepositoryMock.Setup(r => r.SearchEventsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(pagedResult);

            try
            {
                var result = await _eventService.BrowseEventsAsync("keyword", "category", DateTime.UtcNow.AddDays(1), "region", 1, 10);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Items.Count(), Is.EqualTo(1));
                LogTestDetail(Service, "BrowseEventsAsync", "Browse events with keyword and category", null, result.Items.Count(), true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "BrowseEventsAsync", "Browse events with keyword and category", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ConfirmEventUpfrontPaymentAsync_Success
        [Test]
        public async Task Test_ConfirmEventUpfrontPaymentAsync_Success()
        {
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10005,
                Title = "Gala Night",
                Status = "Activation Pending",
                Event_Type = "Virtual",
                Organizer = new User { Name = TestName, Email = TestEmail }
            };

            var mockTransaction = new Transaction
            {
                Transaction_Id = 1000000000000100L,
                Amount = 50.00m,
                Currency = "USD",
                Status = "Pending"
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10005)).ReturnsAsync(mockEvent);
            _transactionRepositoryMock.Setup(r => r.GetPendingOrganizerUpfrontTransactionAsync(10005)).ReturnsAsync(mockTransaction);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _upfrontPaymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OrganizerUpfrontPayment>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(mockEvent)).Returns(Task.CompletedTask);


            try
            {
                var result = await _eventService.ConfirmEventUpfrontPaymentAsync(10005, "tok_visa", "Card");
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Status, Is.EqualTo("Live"));
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Successful confirmation of upfront payment", 10005, result.Status, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Successful confirmation of upfront payment", 10005, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_CancelEventAsync_Success
        [Test]
        public async Task Test_CancelEventAsync_Success()
        {
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10005,
                Title = "Cancelled Gala",
                Status = "Live",
                Date_Time = DateTime.UtcNow.AddDays(3),
                Organizer = new User { Name = TestName, Email = TestEmail }
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10005)).ReturnsAsync(mockEvent);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(mockEvent)).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(mockEvent.Organizer_Id))
                .ReturnsAsync(new List<Transaction> {
                    new Transaction {
                        Related_Id = 10005,
                        Transaction_Type = "OrganizerUpfrontPayment",
                        Status = "Success",
                        Amount = 100.00m,
                        Transaction_Reference = "ch_test_123"
                    }
                });
            _bookingRepositoryMock.Setup(r => r.GetBookingsByEventIdAsync(10005))
                .ReturnsAsync(new List<Booking>());

            try
            {
                var result = await _eventService.CancelEventAsync(10005);
                Assert.That(result, Is.True);
                Assert.That(mockEvent.Status, Is.EqualTo("Cancelled"));
                LogTestDetail(Service, "CancelEventAsync", "Successful cancellation of event", 10005, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CancelEventAsync", "Successful cancellation of event", 10005, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ReleaseExpiredEventCreationAsync_Success
        [Test]
        public async Task Test_ReleaseExpiredEventCreationAsync_Success()
        {
            var mockExpiredEvents = new List<Event.Models.Event>
            {
                new Event.Models.Event
                {
                    Event_Id = 10020,
                    Title = "Expired Event",
                    Status = "Activation Pending",
                    Venue_Id = 10001
                }
            };

            _eventRepositoryMock.Setup(r => r.GetExpiredEventsAsync(It.IsAny<DateTime>())).ReturnsAsync(mockExpiredEvents);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);

            try
            {
                await _eventService.ReleaseExpiredEventCreationAsync();
                LogTestDetail(Service, "ReleaseExpiredEventCreationAsync", "Rollback expired pending event creations", null, "Completed", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ReleaseExpiredEventCreationAsync", "Rollback expired pending event creations", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ReportEventAsync_Success
        [Test]
        public async Task Test_ReportEventAsync_Success()
        {
            _eventRepositoryMock.Setup(r => r.ExistsAsync(1001)).ReturnsAsync(true);
            _eventRepositoryMock.Setup(r => r.AddReportAsync(It.IsAny<EventReport>())).Returns(Task.CompletedTask);

            try
            {
                var result = await _eventService.ReportEventAsync(5, 1001, "Inappropriate content");
                Assert.That(result, Is.True);
                LogTestDetail(Service, "ReportEventAsync", "Report an event successfully", new { EventId = 1001 }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ReportEventAsync", "Report an event successfully", new { EventId = 1001 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ReportEventAsync_EventNotFound_ThrowsNotFoundException
        [Test]
        public void Test_ReportEventAsync_EventNotFound_ThrowsNotFoundException()
        {
            _eventRepositoryMock.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _eventService.ReportEventAsync(5, 999, "Inappropriate content"));
        }
        #endregion

        #region Test_SubmitEventFeedbackAsync_Success
        [Test]
        public async Task Test_SubmitEventFeedbackAsync_Success()
        {
            _eventRepositoryMock.Setup(r => r.ExistsAsync(1001)).ReturnsAsync(true);
            _eventRepositoryMock.Setup(r => r.AddFeedbackAsync(It.IsAny<EventFeedback>())).Returns(Task.CompletedTask);

            try
            {
                var result = await _eventService.SubmitEventFeedbackAsync(5, 1001, 5, "Great event!");
                Assert.That(result, Is.True);
                LogTestDetail(Service, "SubmitEventFeedbackAsync", "Submit event feedback successfully", new { EventId = 1001 }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SubmitEventFeedbackAsync", "Submit event feedback successfully", new { EventId = 1001 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_SubmitEventFeedbackAsync_EventNotFound_ThrowsNotFoundException
        [Test]
        public void Test_SubmitEventFeedbackAsync_EventNotFound_ThrowsNotFoundException()
        {
            _eventRepositoryMock.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _eventService.SubmitEventFeedbackAsync(5, 999, 5, "Great event!"));
        }
        #endregion

        #region Test_VerifyTicketCheckInAsync_SecretHashEmpty_ThrowsValidationException
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Test_VerifyTicketCheckInAsync_SecretHashEmpty_ThrowsValidationException(string? secretHash)
        {
            Assert.ThrowsAsync<ValidationException>(async () =>
                await _eventService.VerifyTicketCheckInAsync(secretHash!));
        }
        #endregion

        #region Test_VerifyTicketCheckInAsync_BookingNotFound_ThrowsNotFoundException
        [Test]
        public void Test_VerifyTicketCheckInAsync_BookingNotFound_ThrowsNotFoundException()
        {
            _bookingRepositoryMock.Setup(r => r.GetBookingBySecretHashAsync("hash123")).ReturnsAsync((Booking?)null);
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _eventService.VerifyTicketCheckInAsync("hash123"));
        }
        #endregion

        #region Test_VerifyTicketCheckInAsync_BookingNotConfirmed_ThrowsValidationException
        [Test]
        public void Test_VerifyTicketCheckInAsync_BookingNotConfirmed_ThrowsValidationException()
        {
            var booking = new Booking { Booking_Status = "Pending" };
            _bookingRepositoryMock.Setup(r => r.GetBookingBySecretHashAsync("hash123")).ReturnsAsync(booking);
            Assert.ThrowsAsync<ValidationException>(async () =>
                await _eventService.VerifyTicketCheckInAsync("hash123"));
        }
        #endregion

        #region Test_VerifyTicketCheckInAsync_AlreadyCheckedIn_ThrowsValidationException
        [Test]
        public void Test_VerifyTicketCheckInAsync_AlreadyCheckedIn_ThrowsValidationException()
        {
            var booking = new Booking { Booking_Status = "Confirmed", CheckIn_Status = "Checked-In" };
            _bookingRepositoryMock.Setup(r => r.GetBookingBySecretHashAsync("hash123")).ReturnsAsync(booking);
            Assert.ThrowsAsync<ValidationException>(async () =>
                await _eventService.VerifyTicketCheckInAsync("hash123"));
        }
        #endregion

        #region Test_VerifyTicketCheckInAsync_Success
        [Test]
        public async Task Test_VerifyTicketCheckInAsync_Success()
        {
            var booking = new Booking { Booking_Status = "Confirmed", CheckIn_Status = "Pending" };
            _bookingRepositoryMock.Setup(r => r.GetBookingBySecretHashAsync("hash123")).ReturnsAsync(booking);
            _bookingRepositoryMock.Setup(r => r.UpdateAsync(booking)).Returns(Task.CompletedTask);

            try
            {
                var result = await _eventService.VerifyTicketCheckInAsync("hash123");
                Assert.That(result.CheckIn_Status, Is.EqualTo("Checked-In"));
                LogTestDetail(Service, "VerifyTicketCheckInAsync", "Check in ticket successfully", new { Hash = "hash123" }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "VerifyTicketCheckInAsync", "Check in ticket successfully", new { Hash = "hash123" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_CheckStaffAvailabilityAsync_VenueNotFoundOrUnavailable_ThrowsNotFoundException
        [Test]
        public void Test_CheckStaffAvailabilityAsync_VenueNotFoundOrUnavailable_ThrowsNotFoundException()
        {
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync((Venue?)null);
            var req = new CheckStaffAvailabilityRequest { VenueId = 100, DateTime = DateTime.UtcNow };
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _eventService.CheckStaffAvailabilityAsync(req));
        }
        #endregion

        #region Test_CheckStaffAvailabilityAsync_SettingsNull_ThrowsValidationException
        [Test]
        public void Test_CheckStaffAvailabilityAsync_SettingsNull_ThrowsValidationException()
        {
            var venue = new Venue { Venue_Id = 100, Is_Available = true };
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(venue);
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync((PlatformSettings?)null);

            var req = new CheckStaffAvailabilityRequest { VenueId = 100, DateTime = DateTime.UtcNow };
            Assert.ThrowsAsync<ValidationException>(async () =>
                await _eventService.CheckStaffAvailabilityAsync(req));
        }
        #endregion

        #region Test_CheckStaffAvailabilityAsync_LessThanTwoAvailableStaff_ReturnsAdequateFalse
        [Test]
        public async Task Test_CheckStaffAvailabilityAsync_LessThanTwoAvailableStaff_ReturnsAdequateFalse()
        {
            var venue = new Venue
            {
                Venue_Id = 100,
                Is_Available = true,
                Region_Id = "US-EAST",
                SeatCapacities = new List<VenueSeatCapacity> { new VenueSeatCapacity { Total_Seats = 150 } }
            };
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(venue);
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Staff_Flat_Rate = 50m });
            _staffRepositoryMock.Setup(r => r.GetAvailableStaffCountAsync("US-EAST", It.IsAny<DateTime>())).ReturnsAsync(1);

            var req = new CheckStaffAvailabilityRequest { VenueId = 100, DateTime = DateTime.UtcNow };
            try
            {
                var result = await _eventService.CheckStaffAvailabilityAsync(req);
                Assert.That(result.IsAdequate, Is.False);
                Assert.That(result.AvailableStaffCount, Is.EqualTo(0));
                LogTestDetail(Service, "CheckStaffAvailabilityAsync", "Less than 2 staff returns adequate false", req, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CheckStaffAvailabilityAsync", "Less than 2 staff returns adequate false", req, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_CheckStaffAvailabilityAsync_AvailableLessThanRequired_ReturnsAdequateFalse
        [Test]
        public async Task Test_CheckStaffAvailabilityAsync_AvailableLessThanRequired_ReturnsAdequateFalse()
        {
            var venue = new Venue
            {
                Venue_Id = 100,
                Is_Available = true,
                Region_Id = "US-EAST",
                SeatCapacities = new List<VenueSeatCapacity> { new VenueSeatCapacity { Total_Seats = 450 } } // Ceiling(4.5) = 5 staff required
            };
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(venue);
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Staff_Flat_Rate = 50m });
            _staffRepositoryMock.Setup(r => r.GetAvailableStaffCountAsync("US-EAST", It.IsAny<DateTime>())).ReturnsAsync(3); // 3 available, < 5 required

            var req = new CheckStaffAvailabilityRequest { VenueId = 100, DateTime = DateTime.UtcNow };
            try
            {
                var result = await _eventService.CheckStaffAvailabilityAsync(req);
                Assert.That(result.IsAdequate, Is.False);
                Assert.That(result.StaffingCost, Is.EqualTo(150m));
                LogTestDetail(Service, "CheckStaffAvailabilityAsync", "Available less than required returns partial", req, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CheckStaffAvailabilityAsync", "Available less than required returns partial", req, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_CheckStaffAvailabilityAsync_SufficientStaff_ReturnsAdequateTrue
        [Test]
        public async Task Test_CheckStaffAvailabilityAsync_SufficientStaff_ReturnsAdequateTrue()
        {
            var venue = new Venue
            {
                Venue_Id = 100,
                Is_Available = true,
                Region_Id = "US-EAST",
                SeatCapacities = new List<VenueSeatCapacity> { new VenueSeatCapacity { Total_Seats = 150 } } // Ceiling(1.5) = 2 staff required
            };
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(venue);
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Staff_Flat_Rate = 50m });
            _staffRepositoryMock.Setup(r => r.GetAvailableStaffCountAsync("US-EAST", It.IsAny<DateTime>())).ReturnsAsync(3); // 3 available, >= 2 required

            var req = new CheckStaffAvailabilityRequest { VenueId = 100, DateTime = DateTime.UtcNow };
            try
            {
                var result = await _eventService.CheckStaffAvailabilityAsync(req);
                Assert.That(result.IsAdequate, Is.True);
                Assert.That(result.StaffingCost, Is.EqualTo(100m));
                LogTestDetail(Service, "CheckStaffAvailabilityAsync", "Sufficient staff returns adequate true", req, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CheckStaffAvailabilityAsync", "Sufficient staff returns adequate true", req, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RevertPendingEventCreationAsync_Success
        [Test]
        public async Task Test_RevertPendingEventCreationAsync_Success()
        {
            var mockEvent = new Event.Models.Event { Event_Id = 1001, Status = "Activation Pending" };
            var mockTx = new Transaction { Related_Id = 1001, Status = "Pending" };

            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(1001)).ReturnsAsync(mockEvent);
            _transactionRepositoryMock.Setup(r => r.GetPendingOrganizerUpfrontTransactionAsync(1001)).ReturnsAsync(mockTx);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(mockTx)).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(mockEvent)).Returns(Task.CompletedTask);

            try
            {
                var result = await _eventService.RevertPendingEventCreationAsync(1001);
                Assert.That(result, Is.True);
                Assert.That(mockEvent.Status, Is.EqualTo("Failed"));
                Assert.That(mockTx.Status, Is.EqualTo("Failed"));
                LogTestDetail(Service, "RevertPendingEventCreationAsync", "Revert pending event creation successfully", 1001, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RevertPendingEventCreationAsync", "Revert pending event creation successfully", 1001, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RevertPendingEventCreationAsync_EventNotFound_ThrowsNotFoundException
        [Test]
        public void Test_RevertPendingEventCreationAsync_EventNotFound_ThrowsNotFoundException()
        {
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(999)).ReturnsAsync((Event.Models.Event?)null);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _eventService.RevertPendingEventCreationAsync(999));
        }
        #endregion

        #region Test_RevertPendingEventCreationAsync_StatusNotPending_ThrowsValidationException
        [Test]
        public void Test_RevertPendingEventCreationAsync_StatusNotPending_ThrowsValidationException()
        {
            var mockEvent = new Event.Models.Event { Event_Id = 1001, Status = "Live" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(1001)).ReturnsAsync(mockEvent);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _eventService.RevertPendingEventCreationAsync(1001));
        }
        #endregion

        #region Test_RevertPendingEventCreationAsync_DatabaseError_RollbacksTransaction
        [Test]
        public void Test_RevertPendingEventCreationAsync_DatabaseError_RollbacksTransaction()
        {
            var mockEvent = new Event.Models.Event { Event_Id = 1001, Status = "Activation Pending" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(1001)).ReturnsAsync(mockEvent);
            _transactionRepositoryMock.Setup(r => r.GetPendingOrganizerUpfrontTransactionAsync(1001)).ThrowsAsync(new Exception("DB Error"));
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            Assert.ThrowsAsync<Exception>(async () =>
                await _eventService.RevertPendingEventCreationAsync(1001));
            _bookingRepositoryMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
        }
        #endregion

        #region Test_GetEventsByInterestedRegionsAsync_UserNotFound_ThrowsNotFoundException
        [Test]
        public void Test_GetEventsByInterestedRegionsAsync_UserNotFound_ThrowsNotFoundException()
        {
            _userRepositoryMock.Setup(r => r.GetUserProfileAsync(999)).ReturnsAsync((User?)null);
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _eventService.GetEventsByInterestedRegionsAsync(999));
        }
        #endregion

        #region Test_GetEventsByInterestedRegionsAsync_NoInterestedRegions_ReturnsEmpty
        [Test]
        public async Task Test_GetEventsByInterestedRegionsAsync_NoInterestedRegions_ReturnsEmpty()
        {
            var user = new User { User_Id = 5, InterestedRegions = new List<UserInterestedRegion>() };
            _userRepositoryMock.Setup(r => r.GetUserProfileAsync(5)).ReturnsAsync(user);

            try
            {
                var result = await _eventService.GetEventsByInterestedRegionsAsync(5);
                Assert.That(result, Is.Empty);
                LogTestDetail(Service, "GetEventsByInterestedRegionsAsync", "Empty region interest list returns empty", 5, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetEventsByInterestedRegionsAsync", "Empty region interest list returns empty", 5, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetEventsByInterestedRegionsAsync_WithRegions_ReturnsEvents
        [Test]
        public async Task Test_GetEventsByInterestedRegionsAsync_WithRegions_ReturnsEvents
()
        {
            var user = new User
            {
                User_Id = 5,
                InterestedRegions = new List<UserInterestedRegion> { new UserInterestedRegion { Region_Id = "US-EAST" } }
            };
            var mockEvents = new List<Event.Models.Event> { new Event.Models.Event { Event_Id = 101, Title = "Regional Gala" } };

            _userRepositoryMock.Setup(r => r.GetUserProfileAsync(5)).ReturnsAsync(user);
            _eventRepositoryMock.Setup(r => r.GetEventsByRegionsAsync(It.Is<List<string>>(list => list.Contains("US-EAST")))).ReturnsAsync(mockEvents);

            try
            {
                var result = await _eventService.GetEventsByInterestedRegionsAsync(5);
                Assert.That(result, Is.Not.Empty);
                Assert.That(result.First().Title, Is.EqualTo("Regional Gala"));
                LogTestDetail(Service, "GetEventsByInterestedRegionsAsync", "Events by user interested regions returned successfully", 5, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetEventsByInterestedRegionsAsync", "Events by user interested regions returned successfully", 5, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        // =====================================================================
        // Additional tests to increase branch/line coverage
        // =====================================================================

        #region CreateEventAsync - Validation Branches

        [Test]
        public void Test_CreateEventAsync_InvalidEventType_ThrowsValidationException()
        {
            var request = new CreateEventRequest
            {
                Title = "Bad Type",
                DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2),
                DurationHours = 1,
                EventType = "Concert",
                HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 5m } }
            };
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings());
            Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        [Test]
        public void Test_CreateEventAsync_EmptyEventType_ThrowsValidationException()
        {
            var request = new CreateEventRequest
            {
                Title = "Empty Type", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 1,
                EventType = "", HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 5m } }
            };
            Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        [Test]
        public void Test_CreateEventAsync_DateTooSoon_ThrowsValidationException()
        {
            var request = new CreateEventRequest
            {
                Title = "Too Soon", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddHours(1), DurationHours = 2,
                EventType = "Virtual", HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 0m } }
            };
            Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        [Test]
        public void Test_CreateEventAsync_OrganizerRestricted_ThrowsValidationException()
        {
            var request = new CreateEventRequest
            {
                Title = "Restricted", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 2,
                EventType = "Virtual", HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 0m } }
            };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(10001)).ReturnsAsync(new User { User_Id = 10001, Status = "Restricted" });
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Virtual_Event_Activation_Fee = 0 });
            Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        [Test]
        public void Test_CreateEventAsync_OrganizerDeactivated_ThrowsValidationException()
        {
            var request = new CreateEventRequest
            {
                Title = "Deactivated", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 2,
                EventType = "Virtual", HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 0m } }
            };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(10001)).ReturnsAsync(new User { User_Id = 10001, Status = "Deactivated" });
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Virtual_Event_Activation_Fee = 0 });
            Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        [Test]
        public void Test_CreateEventAsync_OrganizerNotFound_ThrowsNotFoundException()
        {
            var request = new CreateEventRequest
            {
                Title = "Ghost Org", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 2,
                EventType = "Virtual", HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 0m } }
            };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(99999)).ReturnsAsync((User?)null);
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Virtual_Event_Activation_Fee = 0 });
            Assert.ThrowsAsync<NotFoundException>(async () => await _eventService.CreateEventAsync(99999, request));
        }

        [Test]
        public void Test_CreateEventAsync_PhysicalNoVenueId_ThrowsValidationException()
        {
            var request = new CreateEventRequest
            {
                Title = "No Venue", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 2,
                EventType = "Physical", VenueId = null, HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 10m } }
            };
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Physical_Event_Activation_Fee = 100m });
            Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        [Test]
        public void Test_CreateEventAsync_PhysicalVenueNotFound_ThrowsNotFoundException()
        {
            var request = new CreateEventRequest
            {
                Title = "Bad Venue", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 2,
                EventType = "Physical", VenueId = 9999, HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 10m } }
            };
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Physical_Event_Activation_Fee = 100m });
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(9999)).ReturnsAsync((Venue?)null);
            Assert.ThrowsAsync<NotFoundException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        [Test]
        public void Test_CreateEventAsync_PhysicalVenueOccupied_ThrowsConflictException()
        {
            var request = new CreateEventRequest
            {
                Title = "Occupied", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 2,
                EventType = "Physical", VenueId = 10001, HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 10m } }
            };
            var mockVenue = new Venue { Venue_Id = 10001, Is_Available = true, Hourly_Price = 50m };
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Physical_Event_Activation_Fee = 100m });
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(10001)).ReturnsAsync(mockVenue);
            _venueRepositoryMock.Setup(r => r.IsVenueOccupiedAsync(10001, It.IsAny<DateTime>())).ReturnsAsync(true);
            Assert.ThrowsAsync<ConflictException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        [Test]
        public void Test_CreateEventAsync_PhysicalRequiresStaff_NoneAvailable_ThrowsConflictException()
        {
            var request = new CreateEventRequest
            {
                Title = "No Staff", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 2,
                EventType = "Physical", VenueId = 10001, RequiresStaff = true, HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 10m } }
            };
            var mockVenue = new Venue
            {
                Venue_Id = 10001, Is_Available = true, Hourly_Price = 50m, Region_Id = "US-EAST",
                SeatCapacities = new List<VenueSeatCapacity> { new VenueSeatCapacity { Total_Seats = 100 } }
            };
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Physical_Event_Activation_Fee = 100m, Staff_Flat_Rate = 50m });
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(10001)).ReturnsAsync(mockVenue);
            _venueRepositoryMock.Setup(r => r.IsVenueOccupiedAsync(10001, It.IsAny<DateTime>())).ReturnsAsync(false);
            _staffRepositoryMock.Setup(r => r.GetAvailableStaffCountAsync("US-EAST", It.IsAny<DateTime>())).ReturnsAsync(1);
            Assert.ThrowsAsync<ConflictException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        [Test]
        public async Task Test_CreateEventAsync_PhysicalRequiresStaff_ComputedPath_Success()
        {
            var request = new CreateEventRequest
            {
                Title = "Staff Computed", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 2,
                EventType = "Physical", VenueId = 10001, RequiresStaff = true, HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 10m } }
            };
            var mockVenue = new Venue
            {
                Venue_Id = 10001, Is_Available = true, Hourly_Price = 50m, Region_Id = "US-EAST",
                SeatCapacities = new List<VenueSeatCapacity> { new VenueSeatCapacity { Total_Seats = 100 } }
            };
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Physical_Event_Activation_Fee = 100m, Staff_Flat_Rate = 50m });
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(10001)).ReturnsAsync(mockVenue);
            _venueRepositoryMock.Setup(r => r.IsVenueOccupiedAsync(10001, It.IsAny<DateTime>())).ReturnsAsync(false);
            _staffRepositoryMock.Setup(r => r.GetAvailableStaffCountAsync("US-EAST", It.IsAny<DateTime>())).ReturnsAsync(5);
            _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            try
            {
                var result = await _eventService.CreateEventAsync(10001, request);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Status, Is.EqualTo("Activation Pending"));
                LogTestDetail(Service, "CreateEventAsync", "Physical with computed staff path success", request, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreateEventAsync", "Physical with computed staff path success", request, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_CreateEventAsync_Hybrid_Success()
        {
            var request = new CreateEventRequest
            {
                Title = "Hybrid Conf", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 3,
                EventType = "Hybrid", VenueId = 10001, RequiresStaff = false, HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "General", Price = 15m } }
            };
            var mockVenue = new Venue { Venue_Id = 10001, Is_Available = true, Hourly_Price = 50m };
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Physical_Event_Activation_Fee = 100m });
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(10001)).ReturnsAsync(mockVenue);
            _venueRepositoryMock.Setup(r => r.IsVenueOccupiedAsync(10001, It.IsAny<DateTime>())).ReturnsAsync(false);
            _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            try
            {
                var result = await _eventService.CreateEventAsync(10001, request);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Status, Is.EqualTo("Activation Pending"));
                LogTestDetail(Service, "CreateEventAsync", "Hybrid event creation success", request, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreateEventAsync", "Hybrid event creation success", request, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_CreateEventAsync_DatabaseError_RollsBackTransaction()
        {
            var request = new CreateEventRequest
            {
                Title = "DB Error", DescriptionUrl = "desc",
                DateTime = DateTime.UtcNow.AddDays(2), DurationHours = 2,
                EventType = "Virtual", HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 0m } }
            };
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Virtual_Event_Activation_Fee = 0 });
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event.Models.Event>())).ThrowsAsync(new Exception("DB failure"));
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            Assert.ThrowsAsync<Exception>(async () => await _eventService.CreateEventAsync(10001, request));
            _bookingRepositoryMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
        }

        #endregion

        #region ConfirmEventUpfrontPaymentAsync - Error Paths

        [Test]
        public void Test_ConfirmEventUpfrontPaymentAsync_EventNotFound_ThrowsNotFoundException()
        {
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(9999)).ReturnsAsync((Event.Models.Event?)null);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            Assert.ThrowsAsync<NotFoundException>(async () => await _eventService.ConfirmEventUpfrontPaymentAsync(9999, "tok_visa", "Card"));
        }

        [Test]
        public void Test_ConfirmEventUpfrontPaymentAsync_StatusNotPending_ThrowsValidationException()
        {
            var mockEvent = new Event.Models.Event { Event_Id = 10005, Status = "Live", Event_Type = "Virtual" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10005)).ReturnsAsync(mockEvent);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            Assert.ThrowsAsync<ValidationException>(async () => await _eventService.ConfirmEventUpfrontPaymentAsync(10005, "tok_visa", "Card"));
        }

        [Test]
        public void Test_ConfirmEventUpfrontPaymentAsync_TransactionNotFound_ThrowsNotFoundException()
        {
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10005, Status = "Activation Pending", Event_Type = "Virtual",
                Organizer = new User { Name = TestName, Email = TestEmail }
            };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10005)).ReturnsAsync(mockEvent);
            _transactionRepositoryMock.Setup(r => r.GetPendingOrganizerUpfrontTransactionAsync(10005)).ReturnsAsync((Transaction?)null);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            Assert.ThrowsAsync<NotFoundException>(async () => await _eventService.ConfirmEventUpfrontPaymentAsync(10005, "tok_visa", "Card"));
        }

        [Test]
        public void Test_ConfirmEventUpfrontPaymentAsync_ChargeFailed_ThrowsValidationException()
        {
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10005, Status = "Activation Pending", Event_Type = "Virtual",
                Organizer = new User { Name = TestName, Email = TestEmail }
            };
            var mockTx = new Transaction { Transaction_Id = 100L, Amount = 50m, Currency = "INR", Status = "Pending" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10005)).ReturnsAsync(mockEvent);
            _transactionRepositoryMock.Setup(r => r.GetPendingOrganizerUpfrontTransactionAsync(10005)).ReturnsAsync(mockTx);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            // "tok_fail" triggers mock payment service failure
            Assert.ThrowsAsync<ValidationException>(async () => await _eventService.ConfirmEventUpfrontPaymentAsync(10005, "tok_fail_card", "Card"));
        }

        [Test]
        public async Task Test_ConfirmEventUpfrontPaymentAsync_Physical_WithStaff_Success()
        {
            var mockVenue = new Venue
            {
                Venue_Id = 10001, Name = "Arena", Region_Id = "US-EAST",
                SeatCapacities = new List<VenueSeatCapacity> { new VenueSeatCapacity { Total_Seats = 200 } }
            };
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10006, Status = "Activation Pending", Event_Type = "Physical",
                Requires_Staff = true, Venue_Id = 10001, Venue = mockVenue,
                Organizer = new User { Name = TestName, Email = TestEmail }
            };
            var mockTx = new Transaction { Transaction_Id = 200L, Amount = 200m, Currency = "INR", Status = "Pending" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10006)).ReturnsAsync(mockEvent);
            _transactionRepositoryMock.Setup(r => r.GetPendingOrganizerUpfrontTransactionAsync(10006)).ReturnsAsync(mockTx);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _upfrontPaymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OrganizerUpfrontPayment>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(mockEvent)).Returns(Task.CompletedTask);
            var mockStaff = new Staff { Employee_ID = 1, IsAllocated = false };
            _staffRepositoryMock.Setup(r => r.GetAvailableStaffsAsync("US-EAST", It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Staff> { mockStaff });
            _staffRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Staff>())).Returns(Task.CompletedTask);
            try
            {
                var result = await _eventService.ConfirmEventUpfrontPaymentAsync(10006, "tok_visa", "Card");
                Assert.That(result.Status, Is.EqualTo("Live"));
                Assert.That(mockStaff.IsAllocated, Is.True);
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Physical with staff allocation success", 10006, result.Status, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Physical with staff allocation success", 10006, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_ConfirmEventUpfrontPaymentAsync_Hybrid_WithStaffAndVirtual_Success()
        {
            var mockVenue = new Venue
            {
                Venue_Id = 10001, Name = "Conference Hall", Region_Id = "IN-SOUTH",
                SeatCapacities = new List<VenueSeatCapacity> { new VenueSeatCapacity { Total_Seats = 100 } }
            };
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10007, Status = "Activation Pending", Event_Type = "Hybrid",
                Requires_Staff = true, Venue_Id = 10001, Venue = mockVenue,
                Organizer = new User { Name = TestName, Email = TestEmail }
            };
            var mockTx = new Transaction { Transaction_Id = 300L, Amount = 300m, Currency = "INR", Status = "Pending", Remarks = "Initial" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10007)).ReturnsAsync(mockEvent);
            _transactionRepositoryMock.Setup(r => r.GetPendingOrganizerUpfrontTransactionAsync(10007)).ReturnsAsync(mockTx);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _upfrontPaymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OrganizerUpfrontPayment>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(mockEvent)).Returns(Task.CompletedTask);
            var mockStaff = new Staff { Employee_ID = 2, IsAllocated = false };
            _staffRepositoryMock.Setup(r => r.GetAvailableStaffsAsync("IN-SOUTH", It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Staff> { mockStaff });
            _staffRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Staff>())).Returns(Task.CompletedTask);
            try
            {
                var result = await _eventService.ConfirmEventUpfrontPaymentAsync(10007, "tok_visa", "Card");
                Assert.That(result.Status, Is.EqualTo("Live"));
                Assert.That(mockEvent.Virtual_Url, Is.Not.Null);
                Assert.That(mockEvent.Virtual_Password_Hash, Is.Not.Null);
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Hybrid with staff+virtual success", 10007, result.Status, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Hybrid with staff+virtual success", 10007, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_ConfirmEventUpfrontPaymentAsync_Physical_NullVenue_NullVenueDto_Success()
        {
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10008, Status = "Activation Pending", Event_Type = "Physical",
                Requires_Staff = false, Venue = null,
                Organizer = new User { Name = TestName, Email = TestEmail }
            };
            var mockTx = new Transaction { Transaction_Id = 400L, Amount = 100m, Currency = "INR", Status = "Pending" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10008)).ReturnsAsync(mockEvent);
            _transactionRepositoryMock.Setup(r => r.GetPendingOrganizerUpfrontTransactionAsync(10008)).ReturnsAsync(mockTx);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _upfrontPaymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OrganizerUpfrontPayment>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(mockEvent)).Returns(Task.CompletedTask);
            try
            {
                var result = await _eventService.ConfirmEventUpfrontPaymentAsync(10008, "tok_visa", "Card");
                Assert.That(result.Status, Is.EqualTo("Live"));
                Assert.That(result.Venue, Is.Null);
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Physical null venue DTO success", 10008, result.Status, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Physical null venue DTO success", 10008, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_ConfirmEventUpfrontPaymentAsync_EmptyOrganizerEmail_SkipsEmail()
        {
            // Organizer present but with empty email — the send guard checks
            // (ev.Organizer != null && !string.IsNullOrEmpty(ev.Organizer.Email)), so email is skipped.
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10009, Status = "Activation Pending", Event_Type = "Virtual",
                Organizer = new User { User_Id = 1, Name = "No Email Org", Email = "" }
            };
            var mockTx = new Transaction { Transaction_Id = 500L, Amount = 0m, Currency = "INR", Status = "Pending" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10009)).ReturnsAsync(mockEvent);
            _transactionRepositoryMock.Setup(r => r.GetPendingOrganizerUpfrontTransactionAsync(10009)).ReturnsAsync(mockTx);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _upfrontPaymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OrganizerUpfrontPayment>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(mockEvent)).Returns(Task.CompletedTask);
            try
            {
                var result = await _eventService.ConfirmEventUpfrontPaymentAsync(10009, "tok_visa", "Card");
                Assert.That(result.Status, Is.EqualTo("Live"));
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Empty organizer email skips email send", 10009, result.Status, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ConfirmEventUpfrontPaymentAsync", "Empty organizer email skips email send", 10009, null, false, ex.Message);
                throw;
            }
        }

        #endregion

        #region CancelEventAsync - Error Paths

        [Test]
        public void Test_CancelEventAsync_EventNotFound_ThrowsNotFoundException()
        {
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(9999)).ReturnsAsync((Event.Models.Event?)null);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            Assert.ThrowsAsync<NotFoundException>(async () => await _eventService.CancelEventAsync(9999));
        }

        [Test]
        public void Test_CancelEventAsync_AlreadyCancelled_ThrowsValidationException()
        {
            var mockEvent = new Event.Models.Event { Event_Id = 10005, Status = "Cancelled" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10005)).ReturnsAsync(mockEvent);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            Assert.ThrowsAsync<ValidationException>(async () => await _eventService.CancelEventAsync(10005));
        }

        [Test]
        public async Task Test_CancelEventAsync_WithStaffAllocations_ReleasesStaff()
        {
            var mockStaff = new Staff { Employee_ID = 3, IsAllocated = true };
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10010, Status = "Live",
                Date_Time = DateTime.UtcNow.AddDays(3),
                Organizer = new User { Name = TestName, Email = TestEmail },
                StaffAllocations = new List<EventStaffAllocation> { new EventStaffAllocation { Employee_ID = 3 } }
            };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10010)).ReturnsAsync(mockEvent);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(mockEvent)).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(mockEvent.Organizer_Id))
                .ReturnsAsync(new List<Transaction>
                {
                    new Transaction
                    {
                        Related_Id = 10010, Transaction_Type = "OrganizerUpfrontPayment",
                        Status = "Success", Amount = 100m, Transaction_Reference = "ch_test_123"
                    }
                });
            _bookingRepositoryMock.Setup(r => r.GetBookingsByEventIdAsync(10010)).ReturnsAsync(new List<Booking>());
            _staffRepositoryMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(mockStaff);
            _staffRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Staff>())).Returns(Task.CompletedTask);
            try
            {
                var result = await _eventService.CancelEventAsync(10010);
                Assert.That(result, Is.True);
                Assert.That(mockStaff.IsAllocated, Is.False);
                LogTestDetail(Service, "CancelEventAsync", "Staff released on cancel", 10010, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CancelEventAsync", "Staff released on cancel", 10010, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_CancelEventAsync_DatabaseError_RollsBackTransaction()
        {
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10005)).ThrowsAsync(new Exception("DB Error"));
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            Assert.ThrowsAsync<Exception>(async () => await _eventService.CancelEventAsync(10005));
            _bookingRepositoryMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
        }

        #endregion

        #region BrowseEventsAsync - Additional Branches

        [Test]
        public async Task Test_BrowseEventsAsync_NullItems_ReturnsEmptyList()
        {
            var pagedResult = new PagedResult<Event.Models.Event>
            {
                Items = null!, TotalCount = 0, Page = 1, PageSize = 10
            };
            _eventRepositoryMock.Setup(r => r.SearchEventsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(pagedResult);
            try
            {
                var result = await _eventService.BrowseEventsAsync(null, null, null, null, 1, 10);
                Assert.That(result.Items.Count(), Is.EqualTo(0));
                LogTestDetail(Service, "BrowseEventsAsync", "Null items returns empty list", null, result.TotalCount, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "BrowseEventsAsync", "Null items returns empty list", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_BrowseEventsAsync_PastMinDateTime_UsesCutoff()
        {
            var pagedResult = new PagedResult<Event.Models.Event>
            {
                Items = new List<Event.Models.Event>(), TotalCount = 0, Page = 1, PageSize = 10
            };
            _eventRepositoryMock.Setup(r => r.SearchEventsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(pagedResult);
            try
            {
                var result = await _eventService.BrowseEventsAsync(null, null, DateTime.UtcNow.AddDays(-1), null, 1, 10);
                Assert.That(result, Is.Not.Null);
                LogTestDetail(Service, "BrowseEventsAsync", "Past minDateTime uses cutoff", DateTime.UtcNow.AddDays(-1), result.TotalCount, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "BrowseEventsAsync", "Past minDateTime uses cutoff", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_BrowseEventsAsync_EventsWithReportsAndTiers_MappedCorrectly()
        {
            var mockEvents = new List<Event.Models.Event>
            {
                new Event.Models.Event
                {
                    Event_Id = 200, Title = "Gala",
                    Organizer = new User { Name = "Org One" },
                    Venue = new Venue { Name = "Hall A", Address = "123 St", Region = new Region { Region_Name = "South" } },
                    TicketTiers = new List<EventTicketTier> { new EventTicketTier { Tier_Name = "VIP", Price = 100m, Tickets_Sold = 5 } },
                    Reports = new List<EventReport> { new EventReport { Report_Id = 1, Reporter_Id = 99, Reason = "Spam", Created_At = DateTime.UtcNow } }
                }
            };
            var pagedResult = new PagedResult<Event.Models.Event> { Items = mockEvents, TotalCount = 1, Page = 1, PageSize = 10 };
            _eventRepositoryMock.Setup(r => r.SearchEventsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(pagedResult);
            try
            {
                var result = await _eventService.BrowseEventsAsync(null, null, null, null, 1, 10);
                var item = result.Items.First();
                Assert.That(item.TicketTiers, Has.Count.EqualTo(1));
                Assert.That(item.Reports, Has.Count.EqualTo(1));
                Assert.That(item.Venue_Region_Name, Is.EqualTo("South"));
                LogTestDetail(Service, "BrowseEventsAsync", "Events with reports and tiers mapped", null, item, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "BrowseEventsAsync", "Events with reports and tiers mapped", null, null, false, ex.Message);
                throw;
            }
        }

        #endregion

        #region GetEventDetailsAsync - Not Found & Null Venue

        [Test]
        public void Test_GetEventDetailsAsync_NotFound_ThrowsNotFoundException()
        {
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(9999)).ReturnsAsync((Event.Models.Event?)null);
            Assert.ThrowsAsync<NotFoundException>(async () => await _eventService.GetEventDetailsAsync(9999));
        }

        [Test]
        public async Task Test_GetEventDetailsAsync_NullVenue_ReturnsDtoWithNullVenue()
        {
            var mockEvent = new Event.Models.Event
            {
                Event_Id = 10011, Title = "Virtual Only",
                Organizer = new User { User_Id = 1, Name = "OrgUser", Email = "org@test.com" },
                Venue = null,
                TicketTiers = new List<EventTicketTier> { new EventTicketTier { Tier_Name = "Free", Price = 0m, Tickets_Sold = 0 } }
            };
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10011)).ReturnsAsync(mockEvent);
            try
            {
                var result = await _eventService.GetEventDetailsAsync(10011);
                Assert.That(result!.Venue, Is.Null);
                Assert.That(result.TicketTiers, Has.Count.EqualTo(1));
                LogTestDetail(Service, "GetEventDetailsAsync", "Null venue returns null venue dto", 10011, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetEventDetailsAsync", "Null venue returns null venue dto", 10011, null, false, ex.Message);
                throw;
            }
        }

        #endregion

        #region GetEventsByInterestedRegionsAsync - Events With Tiers and Reports

        [Test]
        public async Task Test_GetEventsByInterestedRegionsAsync_EventsWithTiersAndReports_MappedCorrectly()
        {
            var user = new User
            {
                User_Id = 10,
                InterestedRegions = new List<UserInterestedRegion> { new UserInterestedRegion { Region_Id = "IN-NORTH" } }
            };
            var mockEvents = new List<Event.Models.Event>
            {
                new Event.Models.Event
                {
                    Event_Id = 300, Title = "North Summit",
                    Organizer = new User { Name = "Summit Org" },
                    Venue = new Venue { Name = "North Hall", Address = "456 Road", Region = new Region { Region_Name = "North" } },
                    TicketTiers = new List<EventTicketTier> { new EventTicketTier { Tier_Name = "Standard", Price = 50m, Tickets_Sold = 10 } },
                    Reports = new List<EventReport> { new EventReport { Report_Id = 2, Reporter_Id = 77, Reason = "Offensive", Created_At = DateTime.UtcNow } }
                }
            };
            _userRepositoryMock.Setup(r => r.GetUserProfileAsync(10)).ReturnsAsync(user);
            _eventRepositoryMock.Setup(r => r.GetEventsByRegionsAsync(It.IsAny<List<string>>())).ReturnsAsync(mockEvents);
            try
            {
                var result = await _eventService.GetEventsByInterestedRegionsAsync(10);
                var item = result.First();
                Assert.That(item.TicketTiers, Has.Count.EqualTo(1));
                Assert.That(item.Reports, Has.Count.EqualTo(1));
                LogTestDetail(Service, "GetEventsByInterestedRegionsAsync", "Events with tiers+reports mapped", 10, item, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetEventsByInterestedRegionsAsync", "Events with tiers+reports mapped", 10, null, false, ex.Message);
                throw;
            }
        }

        #endregion

        #region ReleaseExpiredEventCreationAsync - Edge Cases

        [Test]
        public async Task Test_ReleaseExpiredEventCreationAsync_InnerExceptionSwallowed()
        {
            // Expired event that throws internally during revert — outer loop swallows it
            var mockExpiredEvents = new List<Event.Models.Event>
            {
                new Event.Models.Event { Event_Id = 10030, Title = "Bad Expired", Status = "Activation Pending" }
            };
            _eventRepositoryMock.Setup(r => r.GetExpiredEventsAsync(It.IsAny<DateTime>())).ReturnsAsync(mockExpiredEvents);
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10030)).ThrowsAsync(new Exception("DB Error During Revert"));
            try
            {
                await _eventService.ReleaseExpiredEventCreationAsync();
                LogTestDetail(Service, "ReleaseExpiredEventCreationAsync", "Inner exception swallowed per event", null, "Completed", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ReleaseExpiredEventCreationAsync", "Inner exception swallowed per event", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_ReleaseExpiredEventCreationAsync_EmptyList_CompletesGracefully()
        {
            _eventRepositoryMock.Setup(r => r.GetExpiredEventsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Event.Models.Event>());
            try
            {
                await _eventService.ReleaseExpiredEventCreationAsync();
                LogTestDetail(Service, "ReleaseExpiredEventCreationAsync", "Empty list completes gracefully", null, "Completed", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ReleaseExpiredEventCreationAsync", "Empty list completes gracefully", null, null, false, ex.Message);
                throw;
            }
        }

        #endregion

        #region CreateEventAsync - Staff Cache Hit Paths

        [Test]
        public async Task Test_CreateEventAsync_PhysicalRequiresStaff_CacheHit_WithAvailableStaff_Success()
        {
            var cacheVenue = new Venue
            {
                Venue_Id = 20001, Is_Available = true, Hourly_Price = 50m, Region_Id = "EU-WEST",
                SeatCapacities = new List<VenueSeatCapacity> { new VenueSeatCapacity { Total_Seats = 100 } }
            };
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(20001)).ReturnsAsync(cacheVenue);
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync())
                .ReturnsAsync(new PlatformSettings { Staff_Flat_Rate = 50m, Physical_Event_Activation_Fee = 100m });
            _staffRepositoryMock.Setup(r => r.GetAvailableStaffCountAsync("EU-WEST", It.IsAny<DateTime>())).ReturnsAsync(5);

            var cacheDate = DateTime.UtcNow.AddDays(4);
            // Seed the static cache
            await _eventService.CheckStaffAvailabilityAsync(new CheckStaffAvailabilityRequest { VenueId = 20001, DateTime = cacheDate });

            _venueRepositoryMock.Setup(r => r.IsVenueOccupiedAsync(20001, It.IsAny<DateTime>())).ReturnsAsync(false);
            _eventRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);

            var request = new CreateEventRequest
            {
                Title = "Cache Hit Event", DescriptionUrl = "desc",
                DateTime = cacheDate, DurationHours = 2,
                EventType = "Physical", VenueId = 20001, RequiresStaff = true, HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 10m } }
            };
            try
            {
                var result = await _eventService.CreateEventAsync(10001, request);
                Assert.That(result.Status, Is.EqualTo("Activation Pending"));
                LogTestDetail(Service, "CreateEventAsync", "Staff cache hit path success", request, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreateEventAsync", "Staff cache hit path success", request, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_CreateEventAsync_PhysicalRequiresStaff_CacheHitZeroStaff_ThrowsConflictException()
        {
            var cacheVenue = new Venue
            {
                Venue_Id = 20002, Is_Available = true, Hourly_Price = 50m, Region_Id = "EU-NORTH",
                SeatCapacities = new List<VenueSeatCapacity> { new VenueSeatCapacity { Total_Seats = 100 } }
            };
            _venueRepositoryMock.Setup(r => r.GetByIdAsync(20002)).ReturnsAsync(cacheVenue);
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync())
                .ReturnsAsync(new PlatformSettings { Staff_Flat_Rate = 50m, Physical_Event_Activation_Fee = 100m });
            _staffRepositoryMock.Setup(r => r.GetAvailableStaffCountAsync("EU-NORTH", It.IsAny<DateTime>())).ReturnsAsync(1); // < 2 => cache stores 0

            var cacheDate = DateTime.UtcNow.AddDays(5);
            // Seed cache: available < 2, so cache entry has AvailableStaffCount = 0
            await _eventService.CheckStaffAvailabilityAsync(new CheckStaffAvailabilityRequest { VenueId = 20002, DateTime = cacheDate });

            _venueRepositoryMock.Setup(r => r.IsVenueOccupiedAsync(20002, It.IsAny<DateTime>())).ReturnsAsync(false);

            var request = new CreateEventRequest
            {
                Title = "Cache Zero Staff", DescriptionUrl = "desc",
                DateTime = cacheDate, DurationHours = 2,
                EventType = "Physical", VenueId = 20002, RequiresStaff = true, HasAcceptedPolicy = true,
                TicketTiers = new List<CreateTicketTierRequest> { new CreateTicketTierRequest { TierName = "A", Price = 10m } }
            };
            Assert.ThrowsAsync<ConflictException>(async () => await _eventService.CreateEventAsync(10001, request));
        }

        #endregion
    }
}
