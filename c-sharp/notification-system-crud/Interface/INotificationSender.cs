using NotificationSystem.Models;

namespace NotificationSystem.Interface
{
    public interface INotificationSender
    {
        void Send(Notification notification);
    }
}
