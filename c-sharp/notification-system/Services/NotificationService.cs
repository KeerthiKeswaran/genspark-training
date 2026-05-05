using System;
using NotificationSystem.Models;
using NotificationSystem.Interface;

namespace NotificationSystem.Services
{
    public class NotificationService : INotificationSender
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
                User receiver = new User(name, email, string.Empty);
                Notification notification = new EmailNotification(message, DateTime.Now, receiver, sender);
                
                Send(notification);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"\n[Error] {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] An unexpected error occurred: {ex.Message}");
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
                User receiver = new User(name, string.Empty, phone);
                Notification notification = new SmsNotification(message, DateTime.Now, receiver, sender);
                
                Send(notification);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"\n[Error] {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error] An unexpected error occurred: {ex.Message}");
            }
        }

        public void TriggerEmail(EmailNotification emailNotif){
            Console.WriteLine($"\n--- [EMAIL SENT] ---");
            Console.WriteLine($"From: {emailNotif.Sender.Name} <{emailNotif.Sender.Email}>");
            Console.WriteLine($"To: {emailNotif.Receiver.Name} <{emailNotif.Receiver.Email}>");
            Console.WriteLine($"Date: {emailNotif.SentDate}");
            Console.WriteLine($"Message: {emailNotif.Message}");
            Console.WriteLine($"--------------------");
        }

        public void TriggerSMS(SmsNotification smsNotif){
            Console.WriteLine($"\n--- [SMS SENT] ---");
            Console.WriteLine($"From: {smsNotif.Sender.Name} ({smsNotif.Sender.PhoneNumber})");
            Console.WriteLine($"To: {smsNotif.Receiver.Name} ({smsNotif.Receiver.PhoneNumber})");
            Console.WriteLine($"Date: {smsNotif.SentDate}");
            Console.WriteLine($"Message: {smsNotif.Message}");
            Console.WriteLine($"------------------");
        }

        public void Send(Notification notification)
        {

            if (notification is EmailNotification emailNotif)
            {
                TriggerEmail(emailNotif);   
            }
            else if (notification is SmsNotification smsNotif)
            {
                TriggerSMS(smsNotif);   
            }
            else
            {
                Console.WriteLine("Error: Unknown notification format.");
            }
        }
    }
}
