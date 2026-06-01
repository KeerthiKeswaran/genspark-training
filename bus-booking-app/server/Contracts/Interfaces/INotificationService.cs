namespace server.Contracts.Interfaces
{
    public interface INotificationService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendSmsAsync(string toPhone, string message);
        Task NotifyUserAsync(Guid userId, string title, string message, string type = "General", string? relatedEntityId = null);
    }
}
