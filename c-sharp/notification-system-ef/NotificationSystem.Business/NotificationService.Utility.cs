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
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLower();
            }
        }



    }
}
