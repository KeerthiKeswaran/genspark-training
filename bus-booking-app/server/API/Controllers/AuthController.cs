using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Core.Entities;
using server.Core.Enums;
using server.Infrastructure.Data;

using server.Contracts.Interfaces;

namespace server.Features.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;

        public AuthController(AppDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("User with this email already exists.");

            User user;
            
            // Create specialized entity based on role
            switch (request.Role)
            {
                case UserRole.Admin:
                    user = new server.Core.Entities.Admin { FullName = request.FullName, Email = request.Email, Phone = request.Phone, Role = request.Role };
                    break;
                case UserRole.Operator:
                    user = new BusOperator 
                    { 
                        FullName = request.FullName, 
                        Email = request.Email, 
                        Phone = request.Phone, 
                        Role = request.Role,
                        CompanyName = request.CompanyName ?? request.FullName,
                        Address = request.Address ?? string.Empty,
                        Status = ApprovalStatus.Pending,
                        IsApproved = false
                    };
                    break;
                default:
                    user = new User { FullName = request.FullName, Email = request.Email, Phone = request.Phone, Role = request.Role };
                    break;
            }

            user.PasswordHash = _authService.HashPassword(request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Registration successful" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var token = _authService.GenerateToken(user);
            
            bool isApproved = true;
            string status = "Approved";
            string? rejectionReason = null;

            if (user.Role == UserRole.Operator)
            {
                var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == user.Id);
                if (op != null)
                {
                    isApproved = op.IsApproved;
                    status = op.Status.ToString();
                    rejectionReason = op.RejectionReason;
                }
            }

            return Ok(new AuthResponse(token, user.FullName, user.Email, user.Role, user.Id, isApproved, status, rejectionReason));
        }
    }
}
