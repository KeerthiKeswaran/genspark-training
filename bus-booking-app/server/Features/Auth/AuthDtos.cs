using server.Core.Enums;

namespace server.Features.Auth
{
    public record RegisterRequest(string FullName, string Email, string Phone, string Password, UserRole Role);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, string FullName, string Email, UserRole Role);
}
