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
                var success = await _userService.SelectInterestedRegionsAsync(userId, request.RegionId);
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
                var success = await _userService.UpdateUserProfileAsync(userId, request.Name, request.MobileNumber);
                if (!success)
                    return BadRequest(new { Message = "Failed to update user profile." });

                return Ok(new { Message = "Profile updated successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpGet("my-events")]
        public async Task<IActionResult> GetMyEvents()
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var events = await _userService.GetMyEventsAsync(userId);
                return Ok(events);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-events/{eventId}")]
        public async Task<IActionResult> ViewMyEvent(int eventId)
        {
            try
            {
                int userId = _userService.GetCurrentUserId();
                var ev = await _userService.GetMyEventDetailsAsync(userId, eventId);
                if (ev == null)
                    return NotFound(new { Message = "Event not found." });

                return Ok(ev);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
