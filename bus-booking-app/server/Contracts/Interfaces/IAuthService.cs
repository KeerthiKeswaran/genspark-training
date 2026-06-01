using server.Core.Entities;

namespace server.Contracts.Interfaces
{
    public interface IAuthService
    {
        string GenerateToken(User user);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}
