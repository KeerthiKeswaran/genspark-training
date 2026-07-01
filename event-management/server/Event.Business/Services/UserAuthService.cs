using System.Threading.Tasks;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Helpers;
using Event.Business.Exceptions;

namespace Event.Business.Services
{
    public class UserAuthService : IUserAuthService
    {
        #region Fields

        private readonly IUserRepository _userRepository;
        private readonly OtpService _otpService;
        private readonly JwtTokenGenerator _jwtGenerator;
        private readonly ITermsAndConditionsRepository _termsRepository;

        #endregion

        #region Constructor

        public UserAuthService(
            IUserRepository userRepository,
            OtpService otpService,
            JwtTokenGenerator jwtGenerator,
            ITermsAndConditionsRepository termsRepository)
        {
            _userRepository = userRepository;
            _otpService     = otpService;
            _jwtGenerator   = jwtGenerator;
            _termsRepository = termsRepository;
        }

        #endregion

        #region RegisterUserAsync

        public async Task<string?> RegisterUserAsync(User user, string password)
        {
            // 1. Check whether the OTP verification has expired (or was never performed)
            if (await _otpService.IsOtpVerificationExpiredAsync(user.Email, "registration"))
                throw new UnauthorizedException("OTP verification has expired or was not performed. Please verify your OTP again.");

            // 2. Validate that the user is consenting to the active terms and conditions
            var activeTerms = await _termsRepository.GetActiveTermsAsync();
            if (activeTerms == null)
                throw new ValidationException("No active Terms and Conditions are defined on the platform.");

            if (user.Consented_Terms_Id != activeTerms.Terms_Id)
                throw new ValidationException("You must accept the latest Terms and Conditions to register.");

            // 3. Guard against duplicate registration
            var existing = await _userRepository.GetByEmailAsync(user.Email);
            if (existing != null)
                throw new ConflictException("Email is already registered.");

            // 4. Hash the password and save the new user record
            user.Password_Hash    = PasswordHasher.Hash(password);

            await _userRepository.AddAsync(user);

            // Consume the OTP verification marker
            await _otpService.ConsumeOtpVerificationAsync(user.Email, "registration");

            // 5. Return signed JWT for immediate login session activation
            return _jwtGenerator.GenerateUserToken(user.User_Id, user.Email);
        }

        #endregion

        #region LoginUserAsync

        public async Task<string?> LoginUserAsync(string email, string password)
        {
            // 1. Retrieve the user record by email
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new UnauthorizedException("Invalid email or password.");

            // 2. Verify the submitted password hash
            if (!PasswordHasher.Verify(password, user.Password_Hash))
                throw new UnauthorizedException("Invalid email or password.");

            // 3. Generate and return a signed JWT token
            return _jwtGenerator.GenerateUserToken(user.User_Id, user.Email);
        }

        #endregion

        #region ResetUserPasswordAsync

        public async Task<string> ResetUserPasswordAsync(string email, string otp, string newPassword)
        {
            // 1. Verify the OTP details
            if (!await _otpService.VerifyOtpAsync(email, otp, "password-reset"))
                throw new UnauthorizedException("Invalid or expired OTP.");

            // 2. Locate target user account
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new NotFoundException("No account registered with this email address.");

            // 3. Hash the new password and update database
            user.Password_Hash        = PasswordHasher.Hash(newPassword);

            await _userRepository.UpdateAsync(user);
            return "Password reset successfully.";
        }

        #endregion
    }
}
