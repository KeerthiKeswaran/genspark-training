using System;
using System.Text.RegularExpressions;
using NotificationSystem.Exceptions;

namespace NotificationSystem.Business
{
    public class NotificationValidation
    {
        public static void ValidateNotification(string type, string contact, string message)
        {
            ValidateContact(contact, type);
            ValidateMessage(message, type);
        }

        private static void ValidateMessage(string message, string type)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new NotificationException("Message cannot be empty.");

            if (message.Length < 5)
                throw new NotificationException("Message must be at least 5 characters long.");

            if (type == "SMS" && message.Length > 160)
                throw new NotificationException("SMS message cannot exceed 160 characters.");
        }

        private static void ValidateContact(string contact, string type)
        {
            if (string.IsNullOrWhiteSpace(contact))
                throw new NotificationException($"{type} contact information is required.");

            if (type == "Email")
            {
                if (!contact.Contains("@") || !contact.Contains("."))
                    throw new NotificationException("Invalid Email format.");
            }
            else if (type == "SMS")
            {
                if (contact.Length < 10 || !Regex.IsMatch(contact, @"^\d+$"))
                    throw new NotificationException("Invalid Phone Number. It must be at least 10 digits.");
            }
        }
    }
}
