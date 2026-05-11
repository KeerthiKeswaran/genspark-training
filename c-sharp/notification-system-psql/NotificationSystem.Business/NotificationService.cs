using System;
using NotificationSystem.Models;
using NotificationSystem.Senders;
using NotificationSystem.Exceptions;
using NotificationSystem.Contracts;

namespace NotificationSystem.Business
{
    public partial class NotificationService
    {
        public void ProcessNotification(string type, User sender, string receiverName, string contact, string message)
        {
            try
            {
                INotificationSender deliveryAgent = (type == "Email") 
                    ? new EmailNotificationSender() 
                    : new SmsNotificationSender();

                NotificationValidation.ValidateNotification(type, contact, message);

                User? receiver = (type == "Email") 
                    ? _notificationRepository.GetUserById(GenerateId(contact)) 
                    : _notificationRepository.GetUserByPhone(contact);

                if (receiver == null)
                {
                    Console.WriteLine($"Cannot send {type}. The receiver with contact '{contact}' is not registered in the system.");
                    return;
                }

                // Verify receiver name matches (optional but good for consistency)
                if (!receiver.Name.Equals(receiverName, StringComparison.OrdinalIgnoreCase))
                {
                     throw new NotificationException($"Verification Failed: The contact '{contact}' belongs to '{receiver.Name}', not '{receiverName}'.");
                }

                Notification sentNotif = (type == "Email") 
                    ? new EmailNotification(Guid.NewGuid().ToString(), message, DateTime.Now, receiver, sender, "Sent")
                    : new SmsNotification(Guid.NewGuid().ToString(), message, DateTime.Now, receiver, sender, "Sent");

                // Save the notification once (it covers both sender and receiver)
                AddNotification(sender.Id, sentNotif);

                deliveryAgent.Send(sentNotif);
            }
            catch (Exception ex)
            {
                throw new NotificationException(ex.Message);
            }
        }
    }
}
