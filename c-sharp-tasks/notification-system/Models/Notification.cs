using System;

namespace NotificationSystem.Models
{
    public class Notification
    {
        public string Message { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public User Receiver { get; set; } = null!;
        public User Sender { get; set; } = null!;

        public Notification()
        {
        }

        public Notification(string message, DateTime sentDate, User receiver, User sender)
        {
            if(receiver == null)
            {
                throw new ArgumentException("Receiver is required", nameof(receiver));
            }
            if(sender == null)
            {
                throw new ArgumentException("Sender is required", nameof(sender));
            }
            Message = message;
            SentDate = sentDate;
            Receiver = receiver;
            Sender = sender;
        }
    }
}
