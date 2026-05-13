using System;
using System.Collections.Generic;
using NotificationSystem.Models;

namespace NotificationSystem.Contracts
{
    public interface INotificationRepository
    {
        User? GetUserById(string userId);
        User? GetUserByEmail(string email);
        User? GetUserByPhone(string phone);
        void AddNewUser(User user);
        void UpdateUser(User user);
        string CheckUserStatus(string userId);
        string StoreNotification(string userId, Notification notification);
        List<Notification> FilterNotifications(string userId, string status);
        List<Notification> this[string userId] { get; }
        Notification? GetNotificationById(string id, string userId);
        void DeleteNotification(string id, string userId);
        void UpdateNotification(string id, string userId, string newMessage);
    }
}