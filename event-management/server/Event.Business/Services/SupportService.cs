using System.Threading.Tasks;
using Event.Contracts.IServices;
using Event.Contracts.IRepositories;
using Event.Models;
using Event.Business.Exceptions;

namespace Event.Business.Services
{
    public class SupportService : ISupportService
    {
        #region Fields

        private readonly IUserRepository _userRepository;
        private readonly ISupportTicketRepository _supportTicketRepository;

        #endregion

        #region Constructor

        public SupportService(IUserRepository userRepository, ISupportTicketRepository supportTicketRepository)
        {
            _userRepository = userRepository;
            _supportTicketRepository = supportTicketRepository;
        }

        #endregion

        #region SubmitSupportTicketAsync

        public async Task<bool> SubmitSupportTicketAsync(int userId, string subject, string message, string requestType, int? relatedId = null)
        {
            // 1. Validate that target user exists
            var userExists = await _userRepository.ExistsAsync(userId);
            if (!userExists)
                throw new NotFoundException($"User with ID {userId} not found.");

            string? escalationStatus = null;
            if (string.Equals(requestType, "REF", System.StringComparison.OrdinalIgnoreCase))
            {
                escalationStatus = "Available";
            }
            else{
                escalationStatus = "Unavailable";
            }

            // 2. Generate and save the support ticket details in a local JSON file
            string rootPath = System.IO.Directory.GetCurrentDirectory();
            string folderName = "Event.Business";
            if (System.AppDomain.CurrentDomain.FriendlyName.Contains("Tests") || 
                System.AppDomain.CurrentDomain.BaseDirectory.Contains("Tests") ||
                System.IO.Directory.GetCurrentDirectory().Contains("Tests"))
            {
                folderName = "Event.Business.Tests";
            }

            if (rootPath.Contains("bin"))
            {
                rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
            {
                rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootPath, ".."));
            }

            string folderPath = System.IO.Path.Combine(rootPath, folderName, "assets", "support_tickets");
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            string fileName = $"ticket_{System.Guid.NewGuid()}.json";
            string filePath = System.IO.Path.Combine(folderPath, fileName);

            var ticketData = new
            {
                Subject = subject,
                Message = message,
                Response = (string?)null
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(ticketData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, jsonContent);

            // 3. Instantiate and persist new support ticket with "Open" status
            var ticket = new SupportTicket
            {
                User_Id = userId,
                ConcernUrl = $"/assets/support_tickets/{fileName}",
                RequestType = requestType,
                Status = "Open",
                EsclationStatus = escalationStatus,
                RelatedId = relatedId
            };
            await _supportTicketRepository.AddAsync(ticket);
            return true;
        }

        #endregion
    }
}
