using System;

namespace NotificationSystem.Models
{
    public class EmailNotification : Notification
    {
        public EmailNotification() : base() { }

        public EmailNotification(string id, string message, DateTime sentDate, string receiverId, string senderId)
            : base(id, message, sentDate, receiverId, senderId) { }

        public EmailNotification(string id, string message, DateTime sentDate, User receiver, User sender)
            : base(id, message, sentDate, receiver, sender)
        {
            if (receiver != null && string.IsNullOrWhiteSpace(receiver.Email))
            {
                throw new ArgumentException("Email is required for email notifications", nameof(receiver.Email));
            }
        }
    }
}
