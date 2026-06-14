using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Event.Contracts.IServices;
using Event.Contracts.IRepositories;
using Event.Business.Exceptions;
using Event.Models.DTOs;
using System.Collections.Generic;

namespace Event.Business.Services
{
    public class OtpService
    {
        #region Fields

        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly ICacheService _cacheService;

        // OTP is valid for 10 minutes
        private const int OtpExpiryMinutes = 10;

        #endregion

        #region Constructor

        public OtpService(
            IEmailService emailService,
            IUserRepository userRepository,
            IAdminRepository adminRepository,
            ICacheService cacheService)
        {
            _emailService = emailService;
            _userRepository = userRepository;
            _adminRepository = adminRepository;
            _cacheService = cacheService;
        }

        #endregion

        #region SendEmailOtpAsync

        public async Task SendEmailOtpAsync(string email, string purpose)
        {
            // 1. Validate email address input
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email address cannot be empty.");

            // 2. Perform account checking based on purpose (registration/reset/admin-reset)
            if (purpose == "registration")
            {
                var existing = await _userRepository.GetByEmailAsync(email);
                if (existing != null)
                    throw new ConflictException("Email is already registered.");
            }
            else if (purpose == "password-reset")
            {
                var existing = await _userRepository.GetByEmailAsync(email);
                if (existing == null)
                    throw new NotFoundException("No account registered with this email address.");
            }
            else if (purpose == "admin-password-reset" || purpose == "finance-login")
            {
                var existing = await _adminRepository.GetByEmailAsync(email);
                if (existing == null)
                    throw new NotFoundException("No administrator account registered with this email address.");
            }

            // 3. Generate a secure random 6-digit OTP code
            string otp = Random.Shared.Next(100000, 999999).ToString();
            
            // 4. Cache the OTP in our Redis cache with auto-clearing expiry time
            string cacheKey = $"otp:{purpose}:{email}";
            await _cacheService.SetAsync(cacheKey, otp, TimeSpan.FromMinutes(OtpExpiryMinutes));

            // 5. Define the subject lines depending on the authentication purpose
            string subject = purpose == "registration"
                 ? "Your Event Platform Email Verification OTP"
                 : (purpose == "finance-login" ? "Your Finance Dept Login Verification OTP" : "Your Event Platform Password Reset OTP");

            // 6. Load/Compile the formatted HTML email body using the generic EmailTemplateDto
            var purposeLabel = purpose == "registration"
                ? "verify your email address and complete your registration"
                : (purpose == "finance-login" ? "complete your Finance Dept login" : "reset your account password");

            var emailDto = new EmailTemplateDto
            {
                TemplateName = "OtpEmailTemplate.html",
                Placeholders = new Dictionary<string, string>
                {
                    { "purposeLabel", purposeLabel },
                    { "otp", otp },
                    { "year", DateTime.UtcNow.Year.ToString() }
                }
            };

            string htmlBody = await _emailService.BuildEmailHtmlAsync(emailDto);

            // 7. Send the compiled email message via the EmailService
            await _emailService.SendEmailAsync(email, subject, htmlBody);
        }

        #endregion

        #region VerifyOtpAsync

        public async Task<bool> VerifyOtpAsync(string email, string otp, string purpose)
        {
            // 1. Validate the incoming input arguments
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(purpose))
            {
                return false;
            }

            // 2. Fetch the cached OTP from Redis cache using the composite key
            string cacheKey = $"otp:{purpose}:{email}";
            string? cachedOtp = await _cacheService.GetAsync<string>(cacheKey);

            // 3. Verify matching value and remove key on success
            if (cachedOtp != null && cachedOtp == otp)
            {
                await _cacheService.RemoveAsync(cacheKey);
                return true;
            }

            return false;
        }

        #endregion


    }
}
