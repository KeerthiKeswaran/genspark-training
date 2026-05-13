using System;
using System.Linq;
using System.Collections.Generic;
using NotificationSystem.Models;
using NotificationSystem.Contracts;
using NotificationSystem.Data;
using NotificationSystem.Exceptions;

namespace NotificationSystem.Business
{
    public partial class NotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        public INotificationRepository NotificationRepository => _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public (string Status, User? ExistingUser) AuthenticateUser(User user, bool isRegistering)
        {
            var userByEmail = _notificationRepository.GetUserById(user.Id);
            var userByPhone = _notificationRepository.GetUserByPhone(user.PhoneNumber);

            if (isRegistering)
            {
                if (userByEmail != null)
                    throw new NotificationException($"Registration Failed: User already exists with email {user.Email}.");
                
                if (userByPhone != null)
                    throw new NotificationException($"Registration Failed: User already exists with phone number {user.PhoneNumber}.");

                _notificationRepository.AddNewUser(user);
                return ("Registered", user);
            }
            else // Login Logic
            {
                if (userByEmail != null && userByEmail.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase) && userByEmail.PhoneNumber != user.PhoneNumber)
                {
                    throw new NotificationException($"Login Failed: Already a user registered with email {user.Email} but a different phone number.");
                }

                if (userByPhone != null && userByPhone.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase) && userByPhone.Email != user.Email)
                {
                    throw new NotificationException($"Login Failed: Already a user registered with phone {user.PhoneNumber} but a different email.");
                }

                if (userByEmail != null && userByPhone != null && userByEmail.Id == userByPhone.Id)
                {
                    if (!userByEmail.Name.Equals(user.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return ("NameMismatch", userByEmail);
                    }
                    return ("Success", userByEmail);
                }

                if (userByEmail == null)
                    throw new NotificationException("Login Failed: User does not exist. Please register first.");

                throw new NotificationException("Login Failed: Provided details do not match our records.");
            }
        }

        public void CompleteUserUpdate(User user)
        {
            _notificationRepository.UpdateUser(user);
        }

        public string AddNotification(string userId, Notification notification)
        {
            return _notificationRepository.StoreNotification(userId, notification);
        }
        
        public List<Notification> GetReceivedNotifications(User user)
        {
            return _notificationRepository.FilterNotifications(user.Id, "Received");
        }

        public List<Notification> GetSentNotifications(User user)
        {
            return _notificationRepository.FilterNotifications(user.Id, "Sent");
        }

        public List<Notification> FilterNotifications(User user, string filterType, string filterTerm)
        {
            var allNotifs = _notificationRepository[user.Id];
            List<Notification> results = new List<Notification>();

            if (filterType == "1")
            {
                results = allNotifs.Where(n => n.Receiver.Email == filterTerm || n.Sender.Email == filterTerm).ToList();
            }
            else if (filterType == "2")
            {
                results = allNotifs.Where(n => n.Receiver.PhoneNumber == filterTerm || n.Sender.PhoneNumber == filterTerm).ToList();
            }

            return results;
        }

        public void RemoveNotification(User user, string id)
        {
            var userNotifs = _notificationRepository[user.Id];
            var notification = userNotifs.FirstOrDefault(n => n.Id == id);
            
            if (notification == null)
            {
                throw new NotificationException($"Action Failed: Notification with Id '{id}' not found for your account.");
            }

            _notificationRepository.DeleteNotification(id, user.Id);
        }

        public void EditNotification(User user, string id, string newMessage)
        {
            var userNotifs = _notificationRepository[user.Id];
            var notification = userNotifs.FirstOrDefault(n => n.Id == id);

            if (notification == null)
            {
                throw new NotificationException($"Action Failed: Notification with Id '{id}' not found for your account.");
            }

            // Rule: User cannot edit received notifications
            if (notification.SenderId != user.Id)
            {
                throw new NotificationException("Access Denied: You cannot edit a received notification. Only sent messages can be modified.");
            }

            _notificationRepository.UpdateNotification(id, user.Id, newMessage);
        }
    }
}
