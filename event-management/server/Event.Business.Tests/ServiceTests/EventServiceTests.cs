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
    }
}
