using server.Core.Interfaces;

namespace server.Infrastructure.Services
{
    public class MockNotificationService : INotificationService
    {
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

        public Task NotifyUserAsync(Guid userId, string title, string message, string type = "General", string? relatedEntityId = null)
        {
            Console.WriteLine($"\n--- MOCK IN-APP NOTIFICATION ---\nUser: {userId}\nTitle: {title}\nMessage: {message}\n-------------------------------\n");
            return Task.CompletedTask;
        }
    }
}
