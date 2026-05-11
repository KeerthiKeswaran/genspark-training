using System;

namespace NotificationSystem.Models
{
    public class SmsNotification : Notification
    {
        public SmsNotification(string id, string message, DateTime sentDate, User receiver, User sender, string status)
            : base(id, message, sentDate, receiver, sender, status)
        {
            if (string.IsNullOrWhiteSpace(receiver.PhoneNumber))
            {
                throw new ArgumentException("Phone number is required for SMS notifications", nameof(receiver.PhoneNumber));
            }
        }
    }
}
