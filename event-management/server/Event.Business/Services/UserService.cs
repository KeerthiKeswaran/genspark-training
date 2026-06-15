using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Event.Models;
using Event.Models.DTOs;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Exceptions;

namespace Event.Business.Services
{
    public class UserService : IUserService
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;

        #endregion

        #region Constructor

        public UserService(
            IHttpContextAccessor httpContextAccessor, 
            IUserRepository userRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
        }

        #endregion

        #region GetCurrentUserId

        public int GetCurrentUserId()
        {
            // 1. Retrieve the HTTP context associated with the current request
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("HTTP Context is not available outside a request.");
            }

            // 2. Extract user ID value from claims names token identifier
            var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }

            // 3. Fallback: Parse user ID value from request header keys
            if (httpContext.Request.Headers.TryGetValue("X-User-Id", out var headerId) && int.TryParse(headerId, out int parsedId))
            {
                return parsedId;
            }

            // 4. Throw unauthorized exception if identification cannot be resolved
            throw new UnauthorizedException("User identification not found in claims or headers.");
        }

        #endregion

        #region SelectInterestedRegionsAsync

        public async Task<bool> SelectInterestedRegionsAsync(int userId, IEnumerable<string> regionIds)
        {
            // 1. Retrieve the target user and validate existence
            var userExists = await _userRepository.ExistsAsync(userId);
            if (!userExists)
                throw new NotFoundException($"User with ID {userId} not found.");

            // 2. Update interested regions using the repository
            await _userRepository.UpdateInterestedRegionsAsync(userId, regionIds);
            return true;
        }

        #endregion

        #region UpdateUserProfileAsync

        public async Task<bool> UpdateUserProfileAsync(int userId, string name, string mobileNumber, IEnumerable<string> interestedRegions)
        {
            // 1. Retrieve the target user profile
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException($"User with ID {userId} not found.");

            // 2. Modify properties with updated profile values and save to database
            user.Name = name;
            user.Mobile_Number = mobileNumber;
            await _userRepository.UpdateAsync(user);

            // 3. Update associated user interest regions
            await SelectInterestedRegionsAsync(userId, interestedRegions);
            return true;
        }

        #endregion

        #region GetUserProfileAsync

        public async Task<UserProfileResponse?> GetUserProfileAsync(int userId)
        {
            // 1. Query user record with eager interest regions loading from repository
            var user = await _userRepository.GetUserProfileAsync(userId);

            // 2. Validate that user exists
            if (user == null)
                throw new NotFoundException($"User with ID {userId} not found.");

            var interestedRegions = new List<string>();
            if (user.InterestedRegions != null)
            {
                foreach (var ir in user.InterestedRegions)
                {
                    if (ir.Region_Id != null)
                    {
                        interestedRegions.Add(ir.Region_Id);
                    }
                }
            }

            return new UserProfileResponse
            {
                User_Id = user.User_Id,
                Name = user.Name,
                Email = user.Email,
                Mobile_Number = user.Mobile_Number,
                Status = user.Status,
                InterestedRegions = interestedRegions
            };
        }

        #endregion
    }
}
