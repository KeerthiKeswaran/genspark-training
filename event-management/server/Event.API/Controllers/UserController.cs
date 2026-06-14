using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Event.Contracts.IServices;
using Event.Models.DTOs;

namespace Event.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("select-regions")]
        public async Task<IActionResult> SelectRegions([FromBody] SelectRegionsRequest request)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var success = await _userService.SelectInterestedRegionsAsync(userId, request.RegionIds);
                if (!success)
                    return BadRequest(new { Message = "Failed to update interested regions." });

                return Ok(new { Message = "Interested regions updated successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var profile = await _userService.GetUserProfileAsync(userId);
                if (profile == null)
                    return NotFound(new { Message = "User profile not found." });

                return Ok(profile);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var success = await _userService.UpdateUserProfileAsync(userId, request.Name, request.MobileNumber, request.InterestedRegions);
                if (!success)
                    return BadRequest(new { Message = "Failed to update user profile." });

                return Ok(new { Message = "Profile updated successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }
    }
}
