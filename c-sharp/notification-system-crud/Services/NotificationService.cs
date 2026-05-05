using System;
using System.Linq;
using System.Collections.Generic;
using NotificationSystem.Models;
using NotificationSystem.Interface;
using NotificationSystem.Repository;

namespace NotificationSystem.Services
{
    public partial class NotificationService : INotificationSender
    {
        private readonly INotificationRepository _notificationRepository = new NotificationRepository();
        public INotificationRepository NotificationRepository => _notificationRepository;

        public void AddSender(User sender)
        {
            string userLoginStatus = _notificationRepository.AddSender(sender.Id);
            if(userLoginStatus == "New"){
                Console.WriteLine($"\n[System] Sender '{sender.Name}' & Id : '{sender.Id}'added to the system.");
            }else{
                Console.WriteLine($"\nWelcome Back '{sender.Name}' !");
            }
        }

        public string GenerateId(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLower();
            }
        }
    }
}
