using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Business.Services;
using Event.Business.Exceptions;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class UserServiceTests : ServiceTestBase
    {
        private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private UserService _userService = null!;

        private const string Service = "UserService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";
        private const string TestName = "KeerthiKeswaran";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _userService = new UserService(_httpContextAccessorMock.Object, _userRepositoryMock.Object);
        }
        #endregion

        #region GetCurrentUserId Tests
        [Test]
        public void Test_GetCurrentUserId_FromClaims_Success()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            try
            {
                var userId = _userService.GetCurrentUserId();
                Assert.That(userId, Is.EqualTo(1));
                LogTestDetail(Service, "GetCurrentUserId", "Retrieve user ID from ClaimsPrincipal", null, userId, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetCurrentUserId", "Retrieve user ID from ClaimsPrincipal", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_GetCurrentUserId_FromHeaders_Success()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-User-Id"] = "2";
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            try
            {
                var userId = _userService.GetCurrentUserId();
                Assert.That(userId, Is.EqualTo(2));
                LogTestDetail(Service, "GetCurrentUserId", "Retrieve user ID from Request Headers", null, userId, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetCurrentUserId", "Retrieve user ID from Request Headers", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_GetCurrentUserId_MissingId_ThrowsUnauthorizedException()
        {
            var httpContext = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            try
            {
                Assert.Throws<UnauthorizedException>(() => _userService.GetCurrentUserId());
                LogTestDetail(Service, "GetCurrentUserId", "Missing identification throws exception", null, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetCurrentUserId", "Missing identification throws exception", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region SelectInterestedRegionsAsync Tests
        [Test]
        public async Task Test_SelectInterestedRegionsAsync_Success()
        {
            _userRepositoryMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _userRepositoryMock.Setup(r => r.UpdateInterestedRegionsAsync(1, It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

            try
            {
                var result = await _userService.SelectInterestedRegionsAsync(1, new[] { "US-EAST" });
                Assert.That(result, Is.True);
                LogTestDetail(Service, "SelectInterestedRegionsAsync", "Select interested regions for user successfully", new { UserId = 1, Regions = new[] { "US-EAST" } }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SelectInterestedRegionsAsync", "Select interested regions for user successfully", new { UserId = 1, Regions = new[] { "US-EAST" } }, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_SelectInterestedRegionsAsync_UserNotFound_ThrowsNotFoundException()
        {
            _userRepositoryMock.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

            try
            {
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _userService.SelectInterestedRegionsAsync(999, new[] { "US-EAST" })
                );
                LogTestDetail(Service, "SelectInterestedRegionsAsync", "Non-existent user throws not found exception", new { UserId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SelectInterestedRegionsAsync", "Non-existent user throws not found exception", new { UserId = 999 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region UpdateUserProfileAsync Tests
        [Test]
        public async Task Test_UpdateUserProfileAsync_Success()
        {
            var user = new User { User_Id = 1, Name = TestName, Email = TestEmail, Mobile_Number = "1234" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
            _userRepositoryMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

            try
            {
                var result = await _userService.UpdateUserProfileAsync(1, "Updated Name", "9876", new[] { "US-EAST" });
                Assert.That(result, Is.True);
                Assert.That(user.Name, Is.EqualTo("Updated Name"));
                LogTestDetail(Service, "UpdateUserProfileAsync", "Update user profile and regions", new { UserId = 1, Name = "Updated Name" }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "UpdateUserProfileAsync", "Update user profile and regions", new { UserId = 1, Name = "Updated Name" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region GetUserProfileAsync Tests
        [Test]
        public async Task Test_GetUserProfileAsync_Success()
        {
            var user = new User { User_Id = 1, Name = TestName, Email = TestEmail };
            _userRepositoryMock.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(user);

            try
            {
                var result = await _userService.GetUserProfileAsync(1);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Name, Is.EqualTo(TestName));
                LogTestDetail(Service, "GetUserProfileAsync", "Retrieve user profile details", 1, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetUserProfileAsync", "Retrieve user profile details", 1, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
