using System;
using System.IO;
using System.Threading.Tasks;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Models.DTOs;

namespace Event.Business.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly ITermsAndConditionsRepository _termsRepository;

        public PolicyService(ITermsAndConditionsRepository termsRepository)
        {
            _termsRepository = termsRepository;
        }

        public async Task<PolicyResponse?> GetPolicyByTypeAsync(string type)
        {
            // 1. Fetch the active policy of the specified type from the database
            var policy = await _termsRepository.GetActiveTermsByTypeAsync(type);
            if (policy == null)
            {
                return null;
            }

            // 2. Resolve the target file path in the assets directory
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "policies", $"{policy.Terms_Id}.md");
            string content = string.Empty;

            // 3. Read the policy markdown content if the file exists
            if (File.Exists(filePath))
            {
                content = await File.ReadAllTextAsync(filePath);
            }

            // 4. Construct and return the policy response DTO
            return new PolicyResponse
            {
                TermsId = policy.Terms_Id,
                Version = policy.Version,
                Type = policy.Type,
                Content = content
            };
        }
    }
}
