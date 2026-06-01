using server.Core.Enums;

namespace server.Features.Auth
{
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
    }
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public Guid Id { get; set; }
        public bool IsApproved { get; set; } = true;
        public string Status { get; set; } = "Approved";
        public string? RejectionReason { get; set; }

        public AuthResponse() { }

        public AuthResponse(string token, string fullName, string email, UserRole role, Guid id, bool isApproved = true, string status = "Approved", string? rejectionReason = null)
        {
            Token = token;
            FullName = fullName;
            Email = email;
            Role = role;
            Id = id;
            IsApproved = isApproved;
            Status = status;
            RejectionReason = rejectionReason;
        }
    }
}
