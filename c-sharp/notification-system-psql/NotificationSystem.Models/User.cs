using System;

namespace NotificationSystem.Models
{
    public class User : IEquatable<User>
    {
        public string Id {get; set;}
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public User(string id, string name, string email, string phoneNumber)
        {
            Id = id;
            Name = name;
            Email = email;
            PhoneNumber = phoneNumber;
        }

        public bool Equals(User? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }
    }
}
