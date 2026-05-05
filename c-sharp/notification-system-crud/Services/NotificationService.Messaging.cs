using System;
using NotificationSystem.Models;

namespace NotificationSystem.Services
{
    public partial class NotificationService
    {
        public void HandleEmailNotification(User sender)
        {
            Console.Write("Enter Receiver Name: ");
            string name = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Receiver Email: ");
            string email = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Notification Message: ");
            string message = Console.ReadLine() ?? string.Empty;

            try
            {
                string id = GenerateId(Guid.NewGuid().ToString());
                string receiverId = GenerateId(name);
                User receiver = new User(receiverId, name, email, string.Empty);
                
                Notification sentNotif = new EmailNotification(id, message, DateTime.Now, receiver, sender, "Sent");
                Notification receivedNotif = new EmailNotification(id, message, DateTime.Now, receiver, sender, "Received");

                _notificationRepository.AddNotification(sender.Id, sentNotif);
                _notificationRepository.AddNotification(receiver.Id, receivedNotif);

                Send(sentNotif);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] {ex.Message}");
            }
        }

        public void HandleSmsNotification(User sender)
        {
            Console.Write("Enter Receiver Name: ");
            string name = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Receiver Phone Number: ");
            string phone = Console.ReadLine() ?? string.Empty;
            Console.Write("Enter Notification Message: ");
            string message = Console.ReadLine() ?? string.Empty;

            try
            {
                string id = GenerateId(Guid.NewGuid().ToString());
                string receiverId = GenerateId(name);
                User receiver = new User(receiverId, name, string.Empty, phone);
                
                Notification sentNotif = new SmsNotification(id, message, DateTime.Now, receiver, sender, "Sent");
                Notification receivedNotif = new SmsNotification(id, message, DateTime.Now, receiver, sender, "Received");

                _notificationRepository.AddNotification(sender.Id, sentNotif);
                _notificationRepository.AddNotification(receiver.Id, receivedNotif);

                Send(sentNotif);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] {ex.Message}");
            }
        }

        public void TriggerEmail(EmailNotification emailNotif)
        {
            Console.WriteLine($"\n--- [EMAIL SENT] ---");
            Console.WriteLine($"Id: {emailNotif.Id}");
            Console.WriteLine($"From: {emailNotif.Sender.Name} <{emailNotif.Sender.Email}>");
            Console.WriteLine($"To: {emailNotif.Receiver.Name} <{emailNotif.Receiver.Email}>");
            Console.WriteLine($"Message: {emailNotif.Message}");
            Console.WriteLine($"--------------------");
        }

        public void TriggerSMS(SmsNotification smsNotif)
        {
            Console.WriteLine($"\n--- [SMS SENT] ---");
            Console.WriteLine($"Id: {smsNotif.Id}");
            Console.WriteLine($"From: {smsNotif.Sender.Name} ({smsNotif.Sender.PhoneNumber})");
            Console.WriteLine($"To: {smsNotif.Receiver.Name} ({smsNotif.Receiver.PhoneNumber})");
            Console.WriteLine($"Message: {smsNotif.Message}");
            Console.WriteLine($"------------------");
        }

        public void Send(Notification notification)
        {
            if (notification is EmailNotification emailNotif) TriggerEmail(emailNotif);
            else if (notification is SmsNotification smsNotif) TriggerSMS(smsNotif);
        }
    }
}
