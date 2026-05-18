using System.Security.Cryptography;
using System.Text;
using LibrarySystem.Contracts.Interfaces;

namespace LibrarySystem.Business.Services;

public class AuthService : IAuthService
{
    private readonly ILibraryRepository _repository;

    public AuthService(ILibraryRepository repository)
    {
        _repository = repository;
    }

    public string HashPassword(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            throw new ArgumentException("Password cannot be empty.");

        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(plainTextPassword));

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    public bool AuthenticateUser(string userId, string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(plainTextPassword))
            return false;

        string? storedHash = _repository.GetPasswordHashByUserId(userId);
        
        if (storedHash == null)
            return false;

        string incomingHash = HashPassword(plainTextPassword);
        
        return storedHash == incomingHash;
    }
}
