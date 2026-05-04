using System;

namespace NotificationSystem.Models
{
    public class SmsNotification : Notification
    {
        public SmsNotification(string message, DateTime sentDate, User receiver, User sender)
        {
            if (receiver.PhoneNumber == null || string.IsNullOrWhiteSpace(receiver.PhoneNumber))
            {
                throw new ArgumentException("Phone number is required", nameof(receiver.PhoneNumber));
            }
            Message = message;
            SentDate = sentDate;
            Receiver = receiver;
            Sender = sender;
        }
    }
}
