using System;
using System.Linq;
using System.Collections.Generic;
using NotificationSystem.Models;

namespace NotificationSystem.Services
{
    public partial class NotificationService
    {
        public void GetReceivedNotifications(User user)
        {
            var notifications = _notificationRepository[user.Id]
                .Where(n => n.Status == "Received").ToList();
            DisplayNotificationList("RECEIVED NOTIFICATIONS", notifications);
        }

        public void GetSentNotifications(User user)
        {
            var notifications = _notificationRepository[user.Id]
                .Where(n => n.Status == "Sent").ToList();
            DisplayNotificationList("SENT NOTIFICATIONS", notifications);
        }

        public void SearchNotifications(User user)
        {
            Console.WriteLine("\n---- Choose Search Criteria ----:");
            Console.WriteLine("1. Search with Email");
            Console.WriteLine("2. Search with Contact");
            Console.Write("Choice: ");
            string choice = Console.ReadLine() ?? string.Empty;

            var allNotifs = _notificationRepository[user.Id];
            List<Notification> results = new List<Notification>();

            if (choice == "1")
            {
                Console.Write("Enter Email: ");
                string email = Console.ReadLine() ?? string.Empty;
                results = allNotifs.Where(n => n.Receiver.Email == email || n.Sender.Email == email).ToList();
            }
            else if (choice == "2")
            {
                Console.Write("Enter Contact: ");
                string contact = Console.ReadLine() ?? string.Empty;
                results = allNotifs.Where(n => n.Receiver.PhoneNumber == contact || n.Sender.PhoneNumber == contact).ToList();
            }

            DisplayNotificationList("SEARCH RESULTS", results);
        }

        public void DeleteNotification(User user)
        {
            Console.Write("Enter user email to filter: ");
            string email = Console.ReadLine() ?? string.Empty;
            var userNotifs = _notificationRepository[user.Id]
                .Where(n => n.Receiver.Email == email || n.Sender.Email == email).ToList();

            if (!userNotifs.Any())
            {
                Console.WriteLine("No notifications found for this email.");
                return;
            }

            DisplayNotificationList("SELECT NOTIFICATION TO DELETE", userNotifs);
            Console.Write("Enter the Id of the notification to delete: ");
            string id = Console.ReadLine() ?? string.Empty;

            if (userNotifs.Any(n => n.Id == id))
            {
                _notificationRepository.DeleteNotification(id, user.Id);
                Console.WriteLine("Notification deleted successfully.");
            }
            else
            {
                Console.WriteLine("Invalid Id. Access denied or not found.");
            }
        }

        public void EditNotification(User user)
        {
            Console.Write("Enter user email to filter: ");
            string email = Console.ReadLine() ?? string.Empty;
            var userNotifs = _notificationRepository[user.Id]
                .Where(n => n.Receiver.Email == email || n.Sender.Email == email).ToList();

            if (!userNotifs.Any())
            {
                Console.WriteLine("No notifications found for this email.");
                return;
            }

            DisplayNotificationList("SELECT NOTIFICATION TO EDIT", userNotifs);
            Console.Write("Enter the Id of the notification to edit: ");
            string id = Console.ReadLine() ?? string.Empty;

            if (userNotifs.Any(n => n.Id == id))
            {
                Console.Write("Enter new message: ");
                string newMessage = Console.ReadLine() ?? string.Empty;
                // Update in both potential storage locations
                _notificationRepository.UpdateNotification(id, user.Email, newMessage);
                _notificationRepository.UpdateNotification(id, user.PhoneNumber, newMessage);
                Console.WriteLine("Notification updated successfully.");
            }
            else
            {
                Console.WriteLine("Invalid Id. Access denied or not found.");
            }
        }

        private void DisplayNotificationList(string title, List<Notification> notifications)
        {
            Console.WriteLine($"\n=== {title} ===");
            if (!notifications.Any())
            {
                Console.WriteLine("No notifications found.");
                return;
            }

            notifications.Sort();

            foreach (var n in notifications)
            {
                string type = n is EmailNotification ? "EMAIL" : "SMS";
                Console.WriteLine($"[{n.Id}] {type} | From: {n.Sender.Name} | To: {n.Receiver.Name} | Status: {n.Status} | Date: {n.SentDate}");
                Console.WriteLine($"      Message: {n.Message}");
                Console.WriteLine("---------------------------------");
            }
            Console.WriteLine("==============================");
        }
    }
}
