using System;
using NotificationSystem.Models;
using NotificationSystem.Business;
using NotificationSystem.Exceptions;

namespace NotificationSystem.Presentation
{
    class Program
    {
        static void Main(string[] args)
        {
            NotificationService service = new NotificationService();
            User sender = null!;

            Console.WriteLine("=== Notification System Simulation ===");

            bool Authenticate()
            {
                try 
                {
                    Console.WriteLine("\n--- User Authentication ---");
                    Console.WriteLine("1. Register");
                    Console.WriteLine("2. Login");
                    Console.Write("Choice: ");
                    string authChoice = Console.ReadLine() ?? string.Empty;

                    if (authChoice != "1" && authChoice != "2")
                    {
                        Console.WriteLine("Invalid choice.");
                        return false;
                    }

                    bool isRegistering = (authChoice == "1");

                    Console.Write("Enter Name: ");
                    string name = Console.ReadLine() ?? string.Empty;
                    Console.Write("Enter Email: ");
                    string email = Console.ReadLine() ?? string.Empty;
                    Console.Write("Enter Phone Number: ");
                    string phone = Console.ReadLine() ?? string.Empty;

                    string userId = service.GenerateId(email);
                    User tempUser = new User(userId, name, email, phone);

                    var (status, existingUser) = service.AuthenticateUser(tempUser, isRegistering);

                    switch (status)
                    {
                        case "NameMismatch":
                            Console.WriteLine($"\n[Conflict] Name mismatch detected.");
                            Console.WriteLine($"Stored Name: {existingUser!.Name}");
                            Console.WriteLine($"Current Name: {name}");
                            Console.Write("Do you want to continue with the (1) current name or the (2) previous name? ");
                            string nameChoice = Console.ReadLine() ?? string.Empty;
                            
                            if (nameChoice == "1") 
                            {
                                existingUser.Name = name;
                                service.CompleteUserUpdate(existingUser);
                                Console.WriteLine("Name updated to current.");
                            }
                            else 
                            {
                                Console.WriteLine("Continuing with previous name.");
                            }
                            sender = existingUser;
                            return true;

                        case "Success":
                            Console.WriteLine($"\nWelcome back, {existingUser!.Name}!");
                            sender = existingUser;
                            return true;

                        case "Registered":
                            Console.WriteLine($"\nRegistration successful! Welcome, {name}!");
                            sender = tempUser;
                            return true;

                        default:
                            return false;
                    }
                }
                catch (NotificationException ex)
                {
                    throw new NotificationException("Auth Error :" + ex.Message);
                }
                catch (Exception ex)
                {
                    throw new NotificationException("System Failure: " + ex.Message);
                }
            }

            void GetEmailInput() {
                Console.Write("Enter Receiver Name: ");
                string receiverName = Console.ReadLine() ?? string.Empty;
                Console.Write("Enter Receiver Email: ");
                string email = Console.ReadLine() ?? string.Empty;
                Console.Write("Enter Notification Message: ");
                string message = Console.ReadLine() ?? string.Empty;
                service.ProcessNotification("Email", sender, receiverName, email, message);
            }

            void GetSmsInput() {
                Console.Write("Enter Receiver Name: ");
                string receiverName = Console.ReadLine() ?? string.Empty;
                Console.Write("Enter Receiver Phone Number: ");
                string phone = Console.ReadLine() ?? string.Empty;
                Console.Write("Enter Notification Message: ");
                string message = Console.ReadLine() ?? string.Empty;
                service.ProcessNotification("SMS", sender, receiverName, phone, message);
            }

            void HandleFilterInput() {
                Console.WriteLine("\n---- Choose Filter Criteria ----:");
                Console.WriteLine("1. Filter with Email");
                Console.WriteLine("2. Filter with Contact");
                Console.Write("Choice: ");
                string filterType = Console.ReadLine() ?? string.Empty;
                
                string prompt = (filterType == "1") ? "Enter Email: " : "Enter Contact: ";
                Console.Write(prompt);
                string filterTerm = Console.ReadLine() ?? string.Empty;

                var results = service.FilterNotifications(sender, filterType, filterTerm);
                DisplayNotificationList("FILTER RESULTS", results);
            }

            void HandleDeleteInput() {
                try 
                {
                    Console.Write("Enter the Id of the notification to delete: ");
                    string id = Console.ReadLine() ?? string.Empty;
                    service.RemoveNotification(sender, id);
                    Console.WriteLine("Notification deleted successfully.");
                }
                catch (NotificationException ex)
                {
                    Console.WriteLine($"\n[Error] {ex.Message}");
                }
            }

            void DisplayNotificationList(string title, List<Notification> notifications)
            {
                Console.WriteLine($"\n=== {title} ===");
                if (notifications == null || !notifications.Any())
                {
                    Console.WriteLine("No notifications found.");
                    return;
                }

                notifications.Sort();

                foreach (var n in notifications)
                {
                    string contactInfo = n is EmailNotification ? n.Sender.Email : n.Sender.PhoneNumber;
                    string type = n is EmailNotification ? $"EMAIL ({contactInfo})" : $"SMS ({contactInfo})";
                    Console.WriteLine($"[{n.Id}] {type} | From: {n.Sender.Name} | To: {n.Receiver.Name} | Status: {n.Status} | Date: {n.SentDate}");
                    Console.WriteLine($"      Message: {n.Message}");
                    Console.WriteLine("---------------------------------");
                }
                Console.WriteLine("==============================");
            }

            void HandleEditInput() {
                try 
                {
                    Console.Write("Enter the Id of the notification to edit: ");
                    string id = Console.ReadLine() ?? string.Empty;
                    Console.Write("Enter the new message: ");
                    string newMessage = Console.ReadLine() ?? string.Empty;

                    service.EditNotification(sender, id, newMessage);
                    Console.WriteLine("Notification updated successfully.");
                }
                catch (NotificationException ex)
                {
                    Console.WriteLine($"\n[Error] {ex.Message}");
                }
            }

            bool userLoggedIn = false;
            bool isRunning = true;
            while (isRunning)
            {
                if (!userLoggedIn)
                {
                    if (!Authenticate()) continue;
                    userLoggedIn = true;
                }
                Console.WriteLine("\n--- Main Menu ---");
                Console.WriteLine("1. Send Email Notification");
                Console.WriteLine("2. Send SMS Notification");
                Console.WriteLine("3. Get Received Notifications");
                Console.WriteLine("4. Get Sent Notifications");
                Console.WriteLine("5. Filter Notification");
                Console.WriteLine("6. Delete Notification");
                Console.WriteLine("7. Edit Notification Message");
                Console.WriteLine("8. Logout");
                Console.WriteLine("9. Exit");
                Console.Write("Choice: ");
                
                string choice = Console.ReadLine() ?? string.Empty;
                
                switch (choice)
                {
                    case "1":
                        GetEmailInput();
                        break;
                    case "2":
                        GetSmsInput();
                        break;
                    case "3":
                        var received = service.GetReceivedNotifications(sender);
                        DisplayNotificationList("RECEIVED NOTIFICATIONS", received);
                        break;
                    case "4":
                        var sent = service.GetSentNotifications(sender);
                        DisplayNotificationList("SENT NOTIFICATIONS", sent);
                        break;
                    case "5":
                        HandleFilterInput();
                        break;
                    case "6":
                        HandleDeleteInput();
                        break;
                    case "7":
                        HandleEditInput();
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
