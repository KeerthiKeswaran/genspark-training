using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Models.DTOs;
using Event.Business.Exceptions;
using Event.Business.Helpers;

namespace Event.Business.Services
{
    public class AdminService : IAdminService
    {
        #region Fields

        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IBookingPaymentRepository _bookingPaymentRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly ISupportTicketRepository _supportTicketRepository;
        private readonly IAdminActionRepository _adminActionRepository;
        private readonly IEmailService _emailService;
        private readonly IEventService _eventService;
        private readonly IRegionRepository _regionRepository;
        private readonly IVenueRepository _venueRepository;
        private readonly INotificationRepository _notificationRepository;

        #endregion

        #region Constructor

        public AdminService(
            IUserRepository userRepository,
            IEventRepository eventRepository,
            ITransactionRepository transactionRepository,
            IBookingPaymentRepository bookingPaymentRepository,
            IStaffRepository staffRepository,
            ISupportTicketRepository supportTicketRepository,
            IAdminActionRepository adminActionRepository,
            IEmailService emailService,
            IEventService eventService,
            IRegionRepository regionRepository,
            IVenueRepository venueRepository,
            INotificationRepository notificationRepository)
        {
            _userRepository = userRepository;
            _eventRepository = eventRepository;
            _transactionRepository = transactionRepository;
            _bookingPaymentRepository = bookingPaymentRepository;
            _staffRepository = staffRepository;
            _supportTicketRepository = supportTicketRepository;
            _adminActionRepository = adminActionRepository;
            _emailService = emailService;
            _eventService = eventService;
            _regionRepository = regionRepository;
            _venueRepository = venueRepository;
            _notificationRepository = notificationRepository;
        }

        #endregion


        #region GetDashboardStatsAsync

        public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
        {
            // 1. Gather summary numbers
            var users = await _userRepository.GetAllAsync();
            int totalUsers = users.Count();

            var events = await _eventRepository.GetAllAsync();
            int totalLiveEvents = events.Count(e => e.Status == "Live");

            decimal grossRevenue = await _transactionRepository.GetGrossRevenueAsync();
            decimal platformCommission = await _bookingPaymentRepository.GetTotalCommissionAsync();

            // 2. Gather staff metrics
            var staffList = await _staffRepository.GetAllAsync();
            int totalStaff = staffList.Count();
            int allocatedStaffCount = staffList.Count(s => s.IsAllocated);
            double allocationPercentage = totalStaff > 0 ? ((double)allocatedStaffCount / totalStaff) * 100.0 : 0.0;

            return new AdminDashboardStatsDto
            {
                Summary = new StatsSummaryDto
                {
                    TotalUsers = totalUsers,
                    TotalLiveEvents = totalLiveEvents,
                    GrossRevenue = grossRevenue,
                    PlatformCommission = platformCommission
                },
                StaffMetrics = new StaffMetricsDto
                {
                    TotalStaff = totalStaff,
                    AllocatedStaffCount = allocatedStaffCount,
                    AllocationPercentage = Math.Round(allocationPercentage, 2)
                }
            };
        }

        #endregion

        #region GetEventsPagedAsync

        public async Task<PagedResult<EventDetailEto>> GetEventsPagedAsync(
            string? keyword,
            string? eventType,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            int page,
            int size)
        {
            var pagedEvents = await _eventRepository.GetEventsPagedAsync(keyword, eventType, status, startDate, endDate, sortBy, page, size);

            var mappedItems = pagedEvents.Items.Select(e =>
            {
                int totalSeats = e.Venue?.SeatCapacities?.Sum(c => c.Total_Seats) ?? 0;
                int requiredStaff = e.Requires_Staff && e.Venue != null ? Math.Max(1, (int)Math.Ceiling(totalSeats / 100.0)) : 0;
                double allocatedStaffPercentage = 100.0;
                if (e.Requires_Staff && requiredStaff > 0)
                {
                    int allocatedStaffCount = e.StaffAllocations?.Count ?? 0;
                    allocatedStaffPercentage = ((double)allocatedStaffCount / requiredStaff) * 100.0;
                }

                return new EventDetailEto
                {
                    EventId = e.Event_Id,
                    Title = e.Title,
                    EventType = e.Event_Type,
                    DateTime = e.Date_Time,
                    VenueName = e.Venue?.Name ?? "N/A (Virtual)",
                    OrganizerName = e.Organizer?.Name ?? "N/A",
                    AllocatedStaffCount = e.StaffAllocations?.Count ?? 0,
                    Status = e.Status,
                    AllocatedStaffPercentage = Math.Round(allocatedStaffPercentage, 2),
                    DescriptionUrl = e.Description_Url,
                    ImageUrl = e.Image_Url
                };
            }).ToList();

            return new PagedResult<EventDetailEto>(mappedItems, pagedEvents.TotalCount, pagedEvents.Page, pagedEvents.PageSize);
        }

        #endregion

        #region GetSupportTicketsAsync

        public async Task<IEnumerable<SupportTicket>> GetSupportTicketsAsync()
        {
            return await _supportTicketRepository.GetAllAsync();
        }

        #endregion

        #region RespondToTicketAsync

        public async Task<bool> RespondToTicketAsync(int ticketId, string responseText)
        {
            // 1. Fetch support ticket and validate existence
            var ticket = await _supportTicketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                throw new NotFoundException($"Support ticket with ID {ticketId} not found.");
            }

            // 2. Fetch associated user to get name and email
            var user = await _userRepository.GetByIdAsync(ticket.User_Id);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {ticket.User_Id} associated with support ticket {ticketId} not found.");
            }

            if (string.IsNullOrEmpty(ticket.ConcernUrl))
            {
                throw new ValidationException("Support ticket does not have a concern URL path.");
            }

            // 3. Edit the local JSON file containing Subject, Message, and Response
            string rootPath = Directory.GetCurrentDirectory();
            string folderName = "Event.Business";
            if (AppDomain.CurrentDomain.FriendlyName.Contains("Tests") || 
                AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                Directory.GetCurrentDirectory().Contains("Tests"))
            {
                folderName = "Event.Business.Tests";
            }

            if (rootPath.Contains("bin"))
            {
                rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
            {
                rootPath = Path.GetFullPath(Path.Combine(rootPath, ".."));
            }

            string relativeConcern = ticket.ConcernUrl.TrimStart('/');
            if (relativeConcern.StartsWith("assets/"))
            {
                relativeConcern = relativeConcern.Substring("assets/".Length);
            }
            string filePath = Path.Combine(rootPath, folderName, "assets", relativeConcern);

            string? dirPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string subject = "No Subject";
            string message = "No Message";

            if (File.Exists(filePath))
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var ticketData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                if (ticketData != null)
                {
                    if (ticketData.ContainsKey("Subject")) subject = ticketData["Subject"];
                    if (ticketData.ContainsKey("Message")) message = ticketData["Message"];

                    ticketData["Response"] = responseText;
                    
                    var updatedJson = JsonSerializer.Serialize(ticketData, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, updatedJson);
                }
            }
            else
            {
                // Fallback in case JSON file doesn't exist, create it
                var ticketData = new Dictionary<string, string>
                {
                    { "Subject", subject },
                    { "Message", message },
                    { "Response", responseText }
                };
                var updatedJson = JsonSerializer.Serialize(ticketData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, updatedJson);
            }

            // 4. Update the ticket status in database to Resolved
            ticket.Status = "Resolved";
            await _supportTicketRepository.UpdateAsync(ticket);

            // 5. Build and send the response email
            var emailDto = new EmailTemplateDto
            {
                TemplateName = "SupportTicketResponseTemplate.html",
                Placeholders = new Dictionary<string, string>
                {
                    { "userName", user.Name },
                    { "ticketId", ticketId.ToString() },
                    { "subject", subject },
                    { "message", message },
                    { "response", responseText },
                    { "year", DateTime.UtcNow.Year.ToString() }
                }
            };

            string htmlBody = await _emailService.BuildEmailHtmlAsync(emailDto);
            await NotificationHelper.SendAndSaveNotificationAsync(
                _notificationRepository,
                _emailService,
                user.Email,
                $"Support Ticket #{ticketId} Responded",
                htmlBody
            );

            return true;
        }

        #endregion

        #region EscalateTicketAsync

        public async Task<bool> EscalateTicketAsync(int ticketId, string adminId, EscalateTicketRequest request)
        {
            // 1. Fetch support ticket and validate existence
            var ticket = await _supportTicketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                throw new NotFoundException($"Support ticket with ID {ticketId} not found.");
            }

            // 2. Add an AdminAction record with ActionStatus as "Pending" using values from request DTO
            var action = new AdminAction
            {
                AdminId = adminId,
                ActionType = request.ActionType,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                ReferenceId = request.ReferenceId,
                TicketId = ticketId,
                ActionStatus = "Pending",
                Remarks = $"Ticket #{ticketId} escalated.",
                CreatedAt = DateTime.UtcNow
            };
            await _adminActionRepository.AddAsync(action);

            // 3. Mark the escalation status as Escalated
            ticket.EsclationStatus = "Escalated";
            await _supportTicketRepository.UpdateAsync(ticket);

            return true;
        }

        #endregion

        #region GetFlaggedEventsReportsAsync

        public async Task<object> GetFlaggedEventsReportsAsync()
        {
            var reports = await _eventRepository.GetAllReportsAsync();
            
            var grouped = new System.Collections.Generic.Dictionary<int, object>();
            var reportsByEvent = System.Linq.Enumerable.GroupBy(reports, r => r.Event_Id);
            
            foreach (var group in reportsByEvent)
            {
                int eventId = group.Key;
                var list = new System.Collections.Generic.List<object>();
                
                foreach (var r in group)
                {
                    list.Add(new
                    {
                        reportId = r.Report_Id,
                        reporterId = r.Reporter_Id,
                        reporterName = r.Reporter?.Name ?? "Unknown",
                        reason = r.Reason,
                        responseAction = r.ResponseAction,
                        createdAt = r.Created_At
                    });
                }
                
                grouped[eventId] = new
                {
                    countOfReports = group.Count(),
                    reports = list
                };
            }
            
            return grouped;
        }

        #endregion

        #region DismissEventReportAsync

        public async Task<bool> DismissEventReportAsync(int reportId)
        {
            var report = await _eventRepository.GetReportByIdAsync(reportId);
            if (report == null)
                throw new NotFoundException($"EventReport with ID {reportId} not found.");

            report.ResponseAction = "Dismissed";
            await _eventRepository.UpdateReportAsync(report);
            return true;
        }

        #endregion

        #region UpholdEventReportAsync

        public async Task<bool> UpholdEventReportAsync(int reportId, string adminId, string actionReason, string organizerAction)
        {
            // 1. Fetch report details
            var report = await _eventRepository.GetReportByIdAsync(reportId);
            if (report == null)
                throw new NotFoundException($"EventReport with ID {reportId} not found.");

            var ev = report.Event;
            if (ev == null)
                throw new NotFoundException($"Event associated with report {reportId} not found.");

            // 2. Update organizer status based on organizerAction status
            var organizer = await _userRepository.GetByIdAsync(ev.Organizer_Id);
            if (organizer != null)
            {
                if (string.Equals(organizerAction, "Restrict", StringComparison.OrdinalIgnoreCase))
                {
                    organizer.Status = "Restricted";
                    await _userRepository.UpdateAsync(organizer);
                }
                else if (string.Equals(organizerAction, "Deactivate", StringComparison.OrdinalIgnoreCase))
                {
                    organizer.Status = "Deactivated";
                    await _userRepository.UpdateAsync(organizer);
                }
                // If "No Action", do not change status.
            }

            // 3. Create ticket concern file
            string filename = $"escalation_report_{reportId}.json";
            string rootPath = Directory.GetCurrentDirectory();
            string folderName = "Event.Business";
            if (AppDomain.CurrentDomain.FriendlyName.Contains("Tests") || 
                AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                Directory.GetCurrentDirectory().Contains("Tests"))
            {
                folderName = "Event.Business.Tests";
            }

            if (rootPath.Contains("bin"))
            {
                rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
            {
                rootPath = Path.GetFullPath(Path.Combine(rootPath, ".."));
            }

            string absoluteDir = Path.Combine(rootPath, folderName, "assets", "esclation");
            if (!Directory.Exists(absoluteDir))
            {
                Directory.CreateDirectory(absoluteDir);
            }

            string absolutePath = Path.Combine(absoluteDir, filename);
            var ticketData = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Subject", "Escalated Policy Violation: Event Flagged" },
                { "Message", $"This ticket was automatically escalated because Event #{ev.Event_Id} ('{ev.Title}') was flagged and upheld. Action: {actionReason}." },
                { "Response", $"Organizer Action: {organizerAction}." }
            };

            var jsonText = JsonSerializer.Serialize(ticketData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(absolutePath, jsonText);

            // 4. Save SupportTicket record in database
            var ticket = new SupportTicket
            {
                User_Id = ev.Organizer_Id,
                ConcernUrl = $"/assets/esclation/{filename}",
                RequestType = "REF",
                Status = "Open",
                EsclationStatus = "Escalated"
            };
            await _supportTicketRepository.AddAsync(ticket);

            // 5. Save AdminAction in database
            var action = new AdminAction
            {
                AdminId = adminId,
                ActionType = "REF",
                TargetType = "ORG",
                TargetId = ev.Organizer_Id,
                ReferenceId = ev.Event_Id,
                TicketId = ticket.Ticket_Id,
                ActionStatus = "Pending",
                Remarks = $"Event #{ev.Event_Id} flagged and report upheld. Escalated for refund.",
                CreatedAt = DateTime.UtcNow
            };
            await _adminActionRepository.AddAsync(action);

            // 6. Update report state to Upholds
            report.ResponseAction = "Upholds";
            await _eventRepository.UpdateReportAsync(report);

            return true;
        }

        #endregion

        #region GetAllRegionsAsync

        public async Task<IEnumerable<Region>> GetAllRegionsAsync()
        {
            return await _regionRepository.GetAllAsync();
        }

        #endregion

        #region GetAllVenuesAsync

        public async Task<IEnumerable<VenueResponse>> GetAllVenuesAsync()
        {
            var venues = await _venueRepository.GetAllWithDetailsAsync();

            return venues.Select(v => new VenueResponse
            {
                Venue_Id     = v.Venue_Id,
                Region_Id    = v.Region_Id,
                Name         = v.Name,
                Address      = v.Address,
                Hourly_Price = v.Hourly_Price,
                Is_Available = v.Is_Available,
                SeatTiers    = v.SeatCapacities.Select(sc => new SeatTierResponse
                {
                    Tier_Name   = sc.Tier_Name,
                    Total_Seats = sc.Total_Seats
                }).ToList()
            }).ToList();
        }

        #endregion

        #region CreateVenueAsync

        public async Task<VenueResponse> CreateVenueAsync(CreateVenueRequest request)
        {
            // 1. Validate seat tiers — must include all three required tiers
            var validTiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Elite", "Gold", "Silver" };
            var submittedTiers = request.SeatTiers.Select(t => t.Tier_Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!validTiers.SetEquals(submittedTiers))
                throw new ValidationException("Venue must include all three seat tiers: Elite, Gold, and Silver.");

            // 2. Ensure the Region exists; create it if not
            var region = await _regionRepository.GetByRegionIdAsync(request.Region_Id);
            if (region == null)
            {
                region = new Region
                {
                    Region_Id    = request.Region_Id,
                    No_Of_Staffs = 0
                };
                await _regionRepository.AddAsync(region);
            }

            // 3. Build and persist the Venue
            var venue = new Venue
            {
                Region_Id    = request.Region_Id,
                Name         = request.Name,
                Address      = request.Address,
                Hourly_Price = request.Hourly_Price,
                Is_Available = request.Is_Available
            };
            await _venueRepository.AddAsync(venue);

            // 4. Persist each seat tier capacity linked to the new Venue_Id
            foreach (var tierReq in request.SeatTiers)
            {
                var seatCapacity = new VenueSeatCapacity
                {
                    Venue_Id    = venue.Venue_Id,
                    Tier_Name   = tierReq.Tier_Name,
                    Total_Seats = tierReq.Total_Seats
                };
                await _venueRepository.AddSeatCapacityAsync(seatCapacity);
            }

            // 5. Re-fetch the venue with navigation properties for the response
            var created = (await _venueRepository.GetAllWithDetailsAsync())
                .First(v => v.Venue_Id == venue.Venue_Id);

            return new VenueResponse
            {
                Venue_Id     = created.Venue_Id,
                Region_Id    = created.Region_Id,
                Name         = created.Name,
                Address      = created.Address,
                Hourly_Price = created.Hourly_Price,
                Is_Available = created.Is_Available,
                SeatTiers    = created.SeatCapacities.Select(sc => new SeatTierResponse
                {
                    Tier_Name   = sc.Tier_Name,
                    Total_Seats = sc.Total_Seats
                }).ToList()
            };
        }

        #endregion

        #region GetStaffDirectoryAsync
        
        public async Task<IEnumerable<StaffResponse>> GetStaffDirectoryAsync()
        {
            var staffs = await _staffRepository.GetAllAsync();
            return staffs.Select(s => new StaffResponse
            {
                Employee_ID = s.Employee_ID,
                Region_Id = s.Region_Id,
                IsAllocated = s.IsAllocated
            });
        }

        #endregion

        #region AllocateStaffToEventAsync

        public async Task<bool> AllocateStaffToEventAsync(int eventId, int employeeId)
        {
            // 1. Fetch event
            var ev = await _eventRepository.GetEventDetailsAsync(eventId);
            if (ev == null)
            {
                throw new NotFoundException($"Event with ID {eventId} not found.");
            }

            // 2. Validate event type
            if (!string.Equals(ev.Event_Type, "Physical", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ev.Event_Type, "Hybrid", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Staff allocation is only allowed for Physical or Hybrid events.");
            }

            // 3. Fetch staff
            var staff = await _staffRepository.GetByIdAsync(employeeId);
            if (staff == null)
            {
                throw new NotFoundException($"Staff member with ID {employeeId} not found.");
            }

            // 4. Validate working region matches event venue region
            if (ev.Venue == null)
            {
                throw new ValidationException("Event does not have a venue assigned.");
            }
            if (!string.Equals(staff.Region_Id, ev.Venue.Region_Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"Staff member works in region {staff.Region_Id} but event venue is in region {ev.Venue.Region_Id}.");
            }

            // 5. Check if staff is already allocated to this event
            var isAlreadyAllocated = ev.StaffAllocations.Any(sa => sa.Employee_ID == employeeId);
            if (isAlreadyAllocated)
            {
                throw new ValidationException("Staff member is already allocated to this event.");
            }

            // 6. Map staff to event
            var allocation = new EventStaffAllocation
            {
                Event_Id = eventId,
                Employee_ID = employeeId
            };
            ev.StaffAllocations.Add(allocation);
            await _eventRepository.UpdateAsync(ev);

            // 7. Update staff status to allocated
            staff.IsAllocated = true;
            await _staffRepository.UpdateAsync(staff);

            return true;
        }

        #endregion
    }
}
