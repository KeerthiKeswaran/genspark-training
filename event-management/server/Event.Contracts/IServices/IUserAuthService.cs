using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IServices
{
    public interface IUserAuthService
    {
        Task<string?> RegisterUserAsync(User user, string password, string otp);
        Task<string?> LoginUserAsync(string email, string password);
        Task<string> ResetUserPasswordAsync(string email, string otp, string newPassword);
    }
}
