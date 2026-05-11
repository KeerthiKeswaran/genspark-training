using System;
using NotificationSystem.Contracts;
using NotificationSystem.Models;

namespace NotificationSystem.Senders
{
    public class SmsNotificationSender : INotificationSender
    {
        public void Send(Notification notification)
        {
            if (notification is SmsNotification smsNotif)
            {
                Console.WriteLine($"\n--- [SMS SENT] ---");
                Console.WriteLine($"Id: {smsNotif.Id}");
                Console.WriteLine($"From: {smsNotif.Sender.Name} ({smsNotif.Sender.PhoneNumber})");
                Console.WriteLine($"To: {smsNotif.Receiver.Name} ({smsNotif.Receiver.PhoneNumber})");
                Console.WriteLine($"Message: {smsNotif.Message}");
                Console.WriteLine($"------------------");
            }
        }
    }
}
