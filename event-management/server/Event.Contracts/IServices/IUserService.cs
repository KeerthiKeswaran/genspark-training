using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IUserService
    {
        int GetCurrentUserId();
        Task<bool> SelectInterestedRegionsAsync(int userId, IEnumerable<string> regionIds);
        Task<bool> UpdateUserProfileAsync(int userId, string name, string mobileNumber, IEnumerable<string> interestedRegions);
        Task<UserProfileResponse?> GetUserProfileAsync(int userId);
    }
}
