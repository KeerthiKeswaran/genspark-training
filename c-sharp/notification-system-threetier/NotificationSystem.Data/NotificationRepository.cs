using System;
using System.Collections.Generic;
using System.Linq;
using NotificationSystem.Models;
using NotificationSystem.Contracts;

namespace NotificationSystem.Data
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly Dictionary<string, List<Notification>> _notifications = new Dictionary<string, List<Notification>>();
        private readonly Dictionary<string, User> _users = new Dictionary<string, User>();
        
        public User? GetUserById(string userId) => _users.ContainsKey(userId) ? _users[userId] : null;
        public User? GetUserByPhone(string phone) => _users.Values.FirstOrDefault(u => u.PhoneNumber == phone);

        public List<Notification> GetNotifications(string userId)
        {
            if (_notifications.ContainsKey(userId))
            {
                return _notifications[userId];
            }
            return new List<Notification>();
        }

        public List<Notification> this[string userId]
        {
            get { return GetNotifications(userId); }
        }

        public List<Notification> FilterNotifications(string userId, string notificationStatus)
        {
            return this[userId].Where(n => n.Status == notificationStatus).ToList();
        }

        public string CheckUserStatus(string userId)
        {
            return _users.ContainsKey(userId) ? "Exists" : "New";
        }

        public void AddNewUser(User user)
        {
            if (!_users.ContainsKey(user.Id))
            {
                _users.Add(user.Id, user);
                _notifications.Add(user.Id, new List<Notification>());
            }
        }

        public void UpdateUser(User user)
        {
            if (_users.ContainsKey(user.Id))
            {
                _users[user.Id] = user;
            }
        }

        public string StoreNotification(string userId, Notification notification)
        {
            if (!_notifications.ContainsKey(userId))
            {
                // This shouldn't happen with our new flow, but keeping for safety
                _notifications.Add(userId, new List<Notification>());
            }
            
            if (notification != null)
            {
                _notifications[userId].Add(notification);
            }
            
            return "Stored";
        }

        public Notification? GetNotificationById(string id, string userId)
        {
            return this[userId].FirstOrDefault(n => n.Id == id);
        }

        public void DeleteNotification(string id, string userId)
        {
            var notification = GetNotificationById(id, userId);
            if (notification != null)
            {
                this[userId].Remove(notification);
            }
        }

        public void UpdateNotification(string id, string userId, string newMessage)
        {
            var notification = GetNotificationById(id, userId);
            if (notification != null)
            {
                notification.Message = newMessage;
            }
        }
    }
}