using NotificationSystem.Models;

namespace NotificationSystem.Interface
{
    public interface INotificationSender
    {
        void HandleEmailNotification(User sender);
        void HandleSmsNotification(User sender);
        void Send(Notification notification);
    }
}
