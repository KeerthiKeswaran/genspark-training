using NotificationSystem.Models;

namespace NotificationSystem.Contracts
{
    public interface INotificationSender
    {
        void Send(Notification notification);
    }
}
