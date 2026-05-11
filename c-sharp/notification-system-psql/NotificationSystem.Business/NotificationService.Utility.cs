using System;
using System.Linq;
using System.Collections.Generic;
using NotificationSystem.Models;
using NotificationSystem.Contracts;
using NotificationSystem.Data;

namespace NotificationSystem.Business
{
    public partial class NotificationService
    {
        public string GenerateId(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return new Guid(hash).ToString();
            }
        }

        private Notification CloneWithStatus(Notification original, string status)
        {
            if (original is EmailNotification)
                return new EmailNotification(original.Id, original.Message, original.SentDate, original.Receiver, original.Sender, status);
            
            return new SmsNotification(original.Id, original.Message, original.SentDate, original.Receiver, original.Sender, status);
        }

    }
}
