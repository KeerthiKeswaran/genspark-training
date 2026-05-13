using System;

namespace NotificationSystem.Models
{
    public class SmsNotification : Notification
    {
        public SmsNotification() : base() { }

        public SmsNotification(string id, string message, DateTime sentDate, string receiverId, string senderId)
            : base(id, message, sentDate, receiverId, senderId) { }

        public SmsNotification(string id, string message, DateTime sentDate, User receiver, User sender)
            : base(id, message, sentDate, receiver, sender)
        {
            if (receiver != null && string.IsNullOrWhiteSpace(receiver.PhoneNumber))
            {
                throw new ArgumentException("Phone number is required for SMS notifications", nameof(receiver.PhoneNumber));
            }
        }
    }
}
