using System;

namespace NotificationSystem.Models
{
    public class Notification : IComparable<Notification>
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public User Receiver { get; set; } = null!;
        public User Sender { get; set; } = null!;
        public string Status { get; set; } = string.Empty;

        public Notification()
        {
        }

        public Notification(string id, string message, DateTime sentDate, User receiver, User sender, string status)
        {
            if(receiver == null)
            {
                throw new ArgumentException("Receiver is required", nameof(receiver));
            }
            if(sender == null)
            {
                throw new ArgumentException("Sender is required", nameof(sender));
            }

            Id = id;
            Message = message;
            SentDate = sentDate;
            Receiver = receiver;
            Sender = sender;
            Status = status;

        }

        public int CompareTo(Notification? other)
        {
            if (other == null) return 1;
            // Sort descending (newest first)
            return other.SentDate.CompareTo(this.SentDate);
        }
    }
}
