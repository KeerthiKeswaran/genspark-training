using System;

namespace NotificationSystem.Models
{
    public abstract class Notification : IComparable<Notification>
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        
        public string ReceiverId { get; set; } = string.Empty;
        public virtual User Receiver { get; set; } = null!;
        
        public string SenderId { get; set; } = string.Empty;
        public virtual User Sender { get; set; } = null!;
        
        protected Notification()
        {
        }

        protected Notification(string id, string message, DateTime sentDate, string receiverId, string senderId)
        {
            Id = id;
            Message = message;
            SentDate = sentDate;
            ReceiverId = receiverId;
            SenderId = senderId;
        }

        protected Notification(string id, string message, DateTime sentDate, User receiver, User sender)
            : this(id, message, sentDate, receiver?.Id ?? string.Empty, sender?.Id ?? string.Empty)
        {
            Receiver = receiver!;
            Sender = sender!;
        }

        public int CompareTo(Notification? other)
        {
            if (other == null) return 1;
            return other.SentDate.CompareTo(this.SentDate);
        }
    }
}
