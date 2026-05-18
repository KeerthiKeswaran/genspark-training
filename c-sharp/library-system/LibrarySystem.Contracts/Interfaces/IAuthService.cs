using System;

namespace LibrarySystem.Contracts.Interfaces;

public interface IAuthService
{
    string HashPassword(string plainTextPassword);
    bool AuthenticateUser(string userId, string plainTextPassword);
}
