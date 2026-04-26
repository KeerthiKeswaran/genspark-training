using server.Core.Entities;
using server.Core.Interfaces;
using server.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace server.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public Task SendEmailAsync(string toEmail, string subject, string body)
        {
            Console.WriteLine($"\n--- EMAIL NOTIFICATION ---\nTo: {toEmail}\nSubject: {subject}\nBody: {body}\n--------------------------\n");
            return Task.CompletedTask;
        }

        public Task SendSmsAsync(string toPhone, string message)
        {
            Console.WriteLine($"\n--- SMS NOTIFICATION ---\nTo: {toPhone}\nMessage: {message}\n------------------------\n");
            return Task.CompletedTask;
        }

        public async Task NotifyUserAsync(Guid userId, string title, string message, string type = "General", string? relatedEntityId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntityId = relatedEntityId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"\n--- IN-APP NOTIFICATION ---\nUser: {userId}\nTitle: {title}\nMessage: {message}\n---------------------------\n");
        }
    }
}
