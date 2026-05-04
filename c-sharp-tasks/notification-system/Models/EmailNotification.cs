using System;

namespace NotificationSystem.Models
{
    public class EmailNotification : Notification
    {
        public EmailNotification(string message, DateTime sentDate, User receiver, User sender)
        {
            if(receiver.Email == null || string.IsNullOrWhiteSpace(receiver.Email))
            {
                throw new ArgumentException("Email is required", nameof(receiver.Email));
            }
            Message = message;
            SentDate = sentDate;
            Receiver = receiver;
            Sender = sender;
        }
    }
}
