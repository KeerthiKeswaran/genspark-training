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

            Console.WriteLine("=== Notification System Simulation ===");

            Console.WriteLine("\n--- Setup Sender Details ---");
            Console.Write("Enter Sender Name: ");
            string senderName = Console.ReadLine() ?? string.Empty;

            Console.Write("Enter Sender Email: ");
            string senderEmail = Console.ReadLine() ?? string.Empty;

            Console.Write("Enter Sender Phone Number: ");
            string senderPhone = Console.ReadLine() ?? string.Empty;

            User sender = new User(senderName, senderEmail, senderPhone);

            bool isRunning = true;
            while (isRunning)
            {
                Console.WriteLine("\nSelect Notification Type:");
                Console.WriteLine("1. Email Notification");
                Console.WriteLine("2. SMS Notification");
                Console.WriteLine("3. Exit");
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
