using System;
using System.Collections.Generic;
using NotificationSystem.Models;

namespace NotificationSystem.Interface
{
    public interface INotificationRepository
    {
        string AddSender(string userId);
        void AddNotification(string userId, Notification notification);
        List<Notification> GetNotifications(string userId);
        List<Notification> this[string userId] { get; }
        Notification? GetNotificationById(string id, string userId);
        void DeleteNotification(string id, string userId);
        void UpdateNotification(string id, string userId, string newMessage);
    }
}