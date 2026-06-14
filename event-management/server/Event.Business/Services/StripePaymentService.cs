using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Stripe;
using Event.Contracts.IServices;

namespace Event.Business.Services
{
    public class StripePaymentService : IPaymentService
    {
        #region Constructor

        public StripePaymentService(IConfiguration configuration)
        {
            // 1. Fetch Stripe API key from settings and assign it to global configuration
            var apiKey = configuration["Stripe:ApiKey"] 
                ?? throw new InvalidOperationException("Stripe:ApiKey is not configured in settings.");
            StripeConfiguration.ApiKey = apiKey;
        }

        #endregion

        #region CreateChargeAsync

        public async Task<(bool Success, string TransactionReference, string ErrorMessage)> CreateChargeAsync(decimal amount, string currency, string paymentMethodToken, string description)
        {
            try
            {
                // 1. Build charge creation settings converting amount to cents
                var options = new ChargeCreateOptions
                {
                    Amount = (long)(amount * 100),
                    Currency = currency.ToLower(),
                    Source = paymentMethodToken,
                    Description = description,
                };

                // 2. Dispatch creation request to Stripe Charge API
                var service = new ChargeService();
                var charge = await service.CreateAsync(options);

                // 3. Match Stripe response status and return references
                if (charge.Status == "succeeded")
                {
                    return (true, charge.Id, string.Empty);
                }

                return (false, string.Empty, $"Charge status: {charge.Status}. Message: {charge.FailureMessage}");
            }
            catch (StripeException ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        #endregion

        #region CreateRefundAsync

        public async Task<(bool Success, string RefundReference, string ErrorMessage)> CreateRefundAsync(string transactionReference, decimal amount)
        {
            try
            {
                // 1. Build refund options with referenced charge ID and amount in cents
                var options = new RefundCreateOptions
                {
                    Charge = transactionReference,
                    Amount = (long)(amount * 100)
                };

                // 2. Dispatch refund creation request to Stripe Refund API
                var service = new Stripe.RefundService();
                var refund = await service.CreateAsync(options);

                // 3. Verify successful refund response status
                if (refund.Status == "succeeded" || refund.Status == "pending")
                {
                    return (true, refund.Id, string.Empty);
                }

                return (false, string.Empty, $"Refund status: {refund.Status}.");
            }
            catch (StripeException ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        #endregion

        #region CreatePayoutAsync

        public async Task<(bool Success, string TransferReference, string ErrorMessage)> CreatePayoutAsync(string destinationConnectAccountId, decimal amount, string currency)
        {
            try
            {
                // 1. Build transfer parameters for payouts
                var options = new TransferCreateOptions
                {
                    Amount = (long)(amount * 100),
                    Currency = currency.ToLower(),
                    Destination = destinationConnectAccountId,
                    Description = "Organizer event payout"
                };

                // 2. Dispatch transfer creation request to Stripe Transfer API
                var service = new TransferService();
                var transfer = await service.CreateAsync(options);

                // 3. Return successfully generated transfer ID reference
                return (true, transfer.Id, string.Empty);
            }
            catch (StripeException ex)
            {
                return (false, string.Empty, ex.Message);
            }
        }

        #endregion
    }
}
