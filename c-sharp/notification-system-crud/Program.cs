using System;
using NotificationSystem.Models;
using NotificationSystem.Services;

namespace NotificationSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            NotificationService service = new NotificationService();
            User sender = null!;

            Console.WriteLine("=== Notification System Simulation ===");

            void RegisterUser() {
                Console.WriteLine("\n--- Setup Sender Details ---");
                Console.Write("Enter Sender Name: ");
                string senderName = Console.ReadLine() ?? string.Empty;

                Console.Write("Enter Sender Email: ");
                string senderEmail = Console.ReadLine() ?? string.Empty;

                Console.Write("Enter Sender Phone Number: ");
                string senderPhone = Console.ReadLine() ?? string.Empty;

                string userId = service.GenerateId(senderName);

                sender = new User(userId, senderName, senderEmail, senderPhone);
                service.AddSender(sender);
            }

            bool userLoggedIn = false;
            bool isRunning = true;
            while (isRunning)
            {
                if (!userLoggedIn){
                    RegisterUser();
                    userLoggedIn = true;
                }
                Console.WriteLine("\n--- Main Menu ---");
                Console.WriteLine("1. Send Email Notification");
                Console.WriteLine("2. Send SMS Notification");
                Console.WriteLine("3. Get Received Notifications");
                Console.WriteLine("4. Get Sent Notifications");
                Console.WriteLine("5. Search Notification");
                Console.WriteLine("6. Delete Notification");
                Console.WriteLine("7. Edit Notification Message");
                Console.WriteLine("8. Logout");
                Console.WriteLine("9. Exit");
                Console.Write("Choice: ");
                
                string choice = Console.ReadLine() ?? string.Empty;
                
                switch (choice)
                {
                    case "1":
                        service.HandleEmailNotification(sender);
                        break;
                    case "2":
                        service.HandleSmsNotification(sender);
                        break;
                    case "3":
                        service.GetReceivedNotifications(sender);
                        break;
                    case "4":
                        service.GetSentNotifications(sender);
                        break;
                    case "5":
                        service.SearchNotifications(sender);
                        break;
                    case "6":
                        service.DeleteNotification(sender);
                        break;
                    case "7":
                        service.EditNotification(sender);
                        break;
                    case "8":
                        userLoggedIn = false;
                        Console.WriteLine("Logged out successfully.");
                        break;
                    case "9":
                        Console.WriteLine("Exiting simulation...");
                        isRunning = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }
    }
}
