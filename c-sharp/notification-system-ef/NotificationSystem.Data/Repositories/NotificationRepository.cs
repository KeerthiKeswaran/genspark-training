using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using NotificationSystem.Models;
using NotificationSystem.Contracts;
using NotificationSystem.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace NotificationSystem.Data
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationContext _context;

        public NotificationRepository(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<NotificationContext>();
            optionsBuilder.UseNpgsql(connectionString);
            
            _context = new NotificationContext(optionsBuilder.Options);
        }
        
        public User? GetUserById(string userId) => _context.Users.Find(userId);
        public User? GetUserByEmail(string email) => _context.Users.FirstOrDefault(u => u.Email == email);
        public User? GetUserByPhone(string phone) => _context.Users.FirstOrDefault(u => u.PhoneNumber == phone);

        public List<Notification> GetNotifications(string userId)
        {
            return _context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Receiver)
                .Where(n => n.SenderId == userId || n.ReceiverId == userId)
                .OrderByDescending(n => n.SentDate)
                .ToList();
        }

        public List<Notification> this[string userId]
        {
            get { return GetNotifications(userId); }
        }

        public List<Notification> FilterNotifications(string userId, string notificationStatus)
        {
            var query = _context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Receiver)
                .AsQueryable();

            if (notificationStatus == "Sent")
            {
                query = query.Where(n => n.SenderId == userId);
            }
            else if (notificationStatus == "Received")
            {
                query = query.Where(n => n.ReceiverId == userId);
            }

            return query.OrderByDescending(n => n.SentDate).ToList();
        }

        public string CheckUserStatus(string userId)
        {
            return _context.Users.Any(u => u.Id == userId) ? "Exists" : "New";
        }

        public void AddNewUser(User user)
        {
            if (!_context.Users.Any(u => u.Id == user.Id))
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            }
        }

        public void UpdateUser(User user)
        {
            var existingUser = _context.Users.Find(user.Id);
            if (existingUser != null)
            {
                _context.Entry(existingUser).CurrentValues.SetValues(user);
                _context.SaveChanges();
            }
        }

        public string StoreNotification(string userId, Notification notification)
        {
            if (notification != null)
            {
                try
                {
                    _context.Notifications.Add(notification);
                    _context.SaveChanges();
                    return "Stored";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[Database Error] {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[Details] {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
            return "Failed";
        }

        public Notification? GetNotificationById(string id, string userId)
        {
            return _context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Receiver)
                .FirstOrDefault(n => n.Id == id);
        }

        public void DeleteNotification(string id, string userId)
        {
            var notification = _context.Notifications.Find(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                _context.SaveChanges();
            }
        }

        public void UpdateNotification(string id, string userId, string newMessage)
        {
            var notification = _context.Notifications.Find(id);
            if (notification != null)
            {
                notification.Message = newMessage;
                _context.SaveChanges();
            }
        }
    }
}