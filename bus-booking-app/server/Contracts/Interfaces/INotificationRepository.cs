using server.Core.Entities;
using System.Threading.Tasks;

namespace server.Contracts.Interfaces
{
    public interface INotificationRepository
    {
        Task AddNotificationAsync(Notification notification);
        Task SaveChangesAsync();
    }
}
