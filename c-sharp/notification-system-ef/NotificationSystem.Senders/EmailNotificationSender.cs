using System;
using NotificationSystem.Contracts;
using NotificationSystem.Models;

namespace NotificationSystem.Senders
{
    public class EmailNotificationSender : INotificationSender
    {
        public void Send(Notification notification)
        {
            if (notification is EmailNotification emailNotif)
            {
                Console.WriteLine($"\n--- [EMAIL SENT] ---");
                Console.WriteLine($"Id: {emailNotif.Id}");
                Console.WriteLine($"From: {emailNotif.Sender.Name} <{emailNotif.Sender.Email}>");
                Console.WriteLine($"To: {emailNotif.Receiver.Name} <{emailNotif.Receiver.Email}>");
                Console.WriteLine($"Message: {emailNotif.Message}");
                Console.WriteLine($"--------------------");
            }
        }
    }
}