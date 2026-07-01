using System;
using System.Collections.Generic;
using System.IO;
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
    public class FinanceService : IFinanceService
    {
        #region Fields

        private readonly IAdminActionRepository _adminActionRepository;
        private readonly ISupportTicketRepository _supportTicketRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRefundService _refundService;
        private readonly IEmailService _emailService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IEventRepository _eventRepository;

        #endregion

        #region Constructor

        public FinanceService(
            IAdminActionRepository adminActionRepository,
            ISupportTicketRepository supportTicketRepository,
            IUserRepository userRepository,
            IRefundService refundService,
            IEmailService emailService,
            INotificationRepository notificationRepository,
            ITransactionRepository transactionRepository,
            IEventRepository eventRepository)
        {
            _adminActionRepository = adminActionRepository;
            _supportTicketRepository = supportTicketRepository;
            _userRepository = userRepository;
            _refundService = refundService;
            _emailService = emailService;
            _notificationRepository = notificationRepository;
            _transactionRepository = transactionRepository;
            _eventRepository = eventRepository;
        }

        #endregion

        #region GetAdminActionsAsync

        public async Task<IEnumerable<object>> GetAdminActionsAsync()
        {
            var actions = await _adminActionRepository.GetAllAsync();
            var result = new List<object>();

            foreach (var action in actions)
            {
                object? details = null;

                if (action.TicketId.HasValue)
                {
                    // It is a support ticket! Read the support ticket JSON details
                    var ticket = await _supportTicketRepository.GetByIdAsync(action.TicketId.Value);
                    if (ticket != null)
                    {
                        details = GetSupportTicketDetails(ticket.ConcernUrl);
                    }
                }
                else
                {
                    // It is an event report escalation!
                    // Let's get the event reports for ReferenceId (which is the event ID)
                    var reports = await _eventRepository.GetAllReportsAsync() ?? new List<EventReport>();
                    var eventReports = System.Linq.Enumerable.ToList(
                        System.Linq.Enumerable.Where(reports, r => r.Event_Id == action.ReferenceId)
                    );

                    var reportList = new List<object>();
                    foreach (var r in eventReports)
                    {
                        reportList.Add(new
                        {
                            reportId = r.Report_Id,
                            reporterId = r.Reporter_Id,
                            reason = GetReportReason(r.ReportUrl),
                            createdAt = r.Created_At
                        });
                    }
                    details = new { Reports = reportList };
                }

                result.Add(new
                {
                    actionId = action.ActionId,
                    adminId = action.AdminId,
                    actionType = action.ActionType,
                    targetType = action.TargetType,
                    targetId = action.TargetId,
                    referenceId = action.ReferenceId,
                    ticketId = action.TicketId,
                    actionStatus = action.ActionStatus,
                    remarks = action.Remarks,
                    createdAt = action.CreatedAt,
                    details = details
                });
            }

            return result;
        }

        #endregion

        #region DeclineActionAsync

        public async Task<bool> DeclineActionAsync(int actionId, string remarks)
        {
            var action = await _adminActionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                throw new NotFoundException($"AdminAction with ID {actionId} not found.");
            }

            action.ActionStatus = "Declined";
            action.Remarks = remarks;
            await _adminActionRepository.UpdateAsync(action);
            return true;
        }

        #endregion

        #region ApproveActionAsync

        public async Task<bool> ApproveActionAsync(int actionId, string refundType, string refundMessage)
        {
            var action = await _adminActionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                throw new NotFoundException($"AdminAction with ID {actionId} not found.");
            }

            if (!string.Equals(action.ActionType, "REF", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException($"Action type {action.ActionType} is not supported for refund approval.");
            }

            string mappedRefundType;
            switch (refundType?.ToUpper())
            {
                case "FUL":
                    mappedRefundType = "Full";
                    break;
                case "DYN":
                    mappedRefundType = "Dynamic";
                    break;
                case "REM":
                    mappedRefundType = "Remaining";
                    break;
                case "NOR":
                    mappedRefundType = "NoRefund";
                    break;
                default:
                    throw new ValidationException("Invalid refund type. Allowed values are FUL, DYN, REM, or NOR.");
            }

            // Assign status to "Processing" before the payment service call
            action.ActionStatus = "Processing";
            await _adminActionRepository.UpdateAsync(action);

            string finalRemarks = string.Empty;

            if (string.Equals(action.TargetType, "ATD", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(action.TargetType, "ADT", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _refundService.RefundAttendeeAsync(action.ReferenceId, mappedRefundType, refundMessage: refundMessage);
                finalRemarks = $"Approved refund of type {mappedRefundType}. Amount: {result.RefundAmount}. {result.Remarks}";
            }
            else if (string.Equals(action.TargetType, "ORG", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _refundService.RefundOrganizerAsync(action.ReferenceId, mappedRefundType, refundMessage: refundMessage);
                finalRemarks = $"Approved organizer refund of type {mappedRefundType}. Organizer Refund: {result.OrganizerRefundAmount}. {result.OrganizerRemarks}";
            }
            else
            {
                throw new ValidationException($"Target type {action.TargetType} is not recognized. Must be ATD/ADT or ORG.");
            }

            // Update status to "Processed" once the payment has been done
            action.ActionStatus = "Processed";
            action.Remarks = finalRemarks;
            await _adminActionRepository.UpdateAsync(action);
            return true;
        }

        #endregion


        #region RespondToTicketAsync

        public async Task<bool> RespondToTicketAsync(int ticketId, string responseText)
        {
            var ticket = await _supportTicketRepository.GetByIdAsync(ticketId);
            if (ticket == null)
            {
                throw new NotFoundException($"Support ticket with ID {ticketId} not found.");
            }

            var user = await _userRepository.GetByIdAsync(ticket.User_Id);
            if (user == null)
            {
                throw new NotFoundException($"User with ID {ticket.User_Id} associated with support ticket {ticketId} not found.");
            }

            if (string.IsNullOrEmpty(ticket.ConcernUrl))
            {
                throw new ValidationException("Support ticket does not have a concern URL path.");
            }

            string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string folderName = "Event.Business";
            if (AppDomain.CurrentDomain.FriendlyName.Contains("Tests") || 
                AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                Directory.GetCurrentDirectory().Contains("Tests"))
            {
                folderName = "Event.Business.Tests";
            }

            if (rootPath.Contains("bin"))
            {
                rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
            {
                rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
                var ticketData = new Dictionary<string, string>
                {
                    { "Subject", subject },
                    { "Message", message },
                    { "Response", responseText }
                };
                var updatedJson = JsonSerializer.Serialize(ticketData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, updatedJson);
            }

            ticket.Status = "Resolved";
            await _supportTicketRepository.UpdateAsync(ticket);

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
                $"Support Ticket #{ticketId} | Finance Teams",
                htmlBody
            );

            return true;
        }

        #endregion

        #region GetTransactionsPagedAsync

        public async Task<PagedResult<Transaction>> GetTransactionsPagedAsync(
            string? keyword,
            string? transactionType,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            int page,
            int size)
        {
            // 1. Query the transaction repository with paged parameters and filters
            return await _transactionRepository.GetTransactionsPagedAsync(
                keyword,
                transactionType,
                status,
                startDate,
                endDate,
                sortBy,
                page,
                size
            );
        }

        private object GetSupportTicketDetails(string? concernUrl)
        {
            if (string.IsNullOrEmpty(concernUrl)) return new { Subject = "", Message = "", Response = "" };

            try
            {
                string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string folderName = "Event.Business";
                if (AppDomain.CurrentDomain.FriendlyName.Contains("Tests") || 
                    AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                    Directory.GetCurrentDirectory().Contains("Tests"))
                {
                    folderName = "Event.Business.Tests";
                }

                if (rootPath.Contains("bin"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }

                string relativePath = concernUrl.TrimStart('/');
                if (relativePath.StartsWith("assets/"))
                {
                    relativePath = relativePath.Substring("assets/".Length);
                }
                string filePath = Path.Combine(rootPath, folderName, "assets", relativePath);

                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    if (data != null)
                    {
                        return new
                        {
                            Subject = data.ContainsKey("Subject") ? data["Subject"] : "",
                            Message = data.ContainsKey("Message") ? data["Message"] : "",
                            Response = data.ContainsKey("Response") ? data["Response"] : ""
                        };
                    }
                }
            }
            catch { }

            return new { Subject = "", Message = "Details in JSON file", Response = "" };
        }

        private string GetReportReason(string? reportUrl)
        {
            if (string.IsNullOrEmpty(reportUrl)) return string.Empty;

            try
            {
                string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string folderName = "Event.Business";
                if (AppDomain.CurrentDomain.FriendlyName.Contains("Tests") || 
                    AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                    Directory.GetCurrentDirectory().Contains("Tests"))
                {
                    folderName = "Event.Business.Tests";
                }

                if (rootPath.Contains("bin"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }

                string relativePath = reportUrl.TrimStart('/');
                if (relativePath.StartsWith("assets/"))
                {
                    relativePath = relativePath.Substring("assets/".Length);
                }
                string filePath = Path.Combine(rootPath, folderName, "assets", relativePath);

                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    if (data != null && data.ContainsKey("Reason"))
                    {
                        return data["Reason"];
                    }
                }
            }
            catch { }

            return "Details in JSON file";
        }

        #endregion
    }
}
