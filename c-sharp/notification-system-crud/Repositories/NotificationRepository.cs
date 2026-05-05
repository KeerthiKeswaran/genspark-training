using System;
using System.Collections.Generic;
using System.Linq;
using NotificationSystem.Models;
using NotificationSystem.Interface;

namespace NotificationSystem.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly Dictionary<string, List<Notification>> _notifications = new Dictionary<string, List<Notification>>();

        public string AddSender(string userId)
        {
            if (!_notifications.ContainsKey(userId))
            {
                _notifications.Add(userId, new List<Notification>());
                return "New";
            }else{
                return "Exists";
            }
        }

        public void AddNotification(string userId, Notification notification)
        {
            if (!_notifications.ContainsKey(userId))
            {
                AddSender(userId);
            }
            _notifications[userId].Add(notification);
        }

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

        public Notification? GetNotificationById(string id, string userId)
        {
            return GetNotifications(userId).FirstOrDefault(n => n.Id == id);
        }

        public void DeleteNotification(string id, string userId)
        {
            var notification = GetNotificationById(id, userId);
            if (notification != null)
            {
                _notifications[userId].Remove(notification);
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