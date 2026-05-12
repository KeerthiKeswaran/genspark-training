using System;

namespace WordleGame.Models
{
    public class UserModel
    {
        public string UserId { get; set; } = string.Empty; // 5 character ID
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
