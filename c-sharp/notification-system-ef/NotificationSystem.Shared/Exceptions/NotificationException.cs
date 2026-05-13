using System;

namespace NotificationSystem.Exceptions
{
    public class NotificationException : Exception
    {
        public NotificationException(string message) : base(message) { }
    }
}
