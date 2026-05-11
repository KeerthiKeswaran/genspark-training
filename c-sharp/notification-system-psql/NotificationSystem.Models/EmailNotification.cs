using System;

namespace NotificationSystem.Models
{
    public class EmailNotification : Notification
    {
        public EmailNotification(string id, string message, DateTime sentDate, User receiver, User sender, string status)
            : base(id, message, sentDate, receiver, sender, status)
        {
            if (string.IsNullOrWhiteSpace(receiver.Email))
            {
                throw new ArgumentException("Email is required for email notifications", nameof(receiver.Email));
            }
        }
    }
}
