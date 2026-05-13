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
                    ? _notificationRepository.GetUserByEmail(contact) 
                    : _notificationRepository.GetUserByPhone(contact);

                if (receiver == null)
                {
                    throw new NotificationException($"Cannot send {type}. The receiver with contact '{contact}' is not registered in the system.");
                }

                // Verify receiver name matches (optional but good for consistency)
                if (!receiver.Name.Equals(receiverName, StringComparison.OrdinalIgnoreCase))
                {
                     throw new NotificationException($"Verification Failed: The contact '{contact}' belongs to '{receiver.Name}', not '{receiverName}'.");
                }

                Notification sentNotif = (type == "Email") 
                    ? new EmailNotification(GenerateId(Guid.NewGuid().ToString()), message, DateTime.UtcNow, receiver.Id, sender.Id)
                    : new SmsNotification(GenerateId(Guid.NewGuid().ToString()), message, DateTime.UtcNow, receiver.Id, sender.Id);

                // Save once (Shared Record)
                AddNotification(sender.Id, sentNotif);

                deliveryAgent.Send(sentNotif);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] {ex.Message}");
            }
        }
    }
}
