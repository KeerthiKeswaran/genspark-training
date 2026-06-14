using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Serilog;
using Event.Business.Services;
using Event.Contracts.IServices;

namespace Event.Business.Tests
{
    public abstract class ServiceTestBase : IDisposable
    {
        static ServiceTestBase()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            string logPath = Path.Combine(projectDir, "test_results.log");

            try
            {
                if (File.Exists(logPath))
                {
                    File.Delete(logPath);
                }
            }
            catch
            {
                // Silently swallow errors if file is locked or inaccessible during static setup
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}")
                .CreateLogger();
        }

        public class FakeHttpMessageHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string path = request.RequestUri?.PathAndQuery ?? "";
                string jsonResponse = "{}";
                System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK;

                string requestBody = "";
                if (request.Content != null)
                {
                    requestBody = await request.Content.ReadAsStringAsync();
                }

                if (path.Contains("/v1/charges"))
                {
                    if (requestBody.Contains("source=tok_fail"))
                    {
                        statusCode = System.Net.HttpStatusCode.BadRequest;
                        jsonResponse = "{\"error\": {\"message\": \"Card declined\", \"type\": \"card_error\"}}";
                    }
                    else
                    {
                        jsonResponse = "{\"id\": \"ch_test_123\", \"status\": \"succeeded\"}";
                    }
                }
                else if (path.Contains("/v1/refunds"))
                {
                    if (requestBody.Contains("charge=ch_fail_declined"))
                    {
                        statusCode = System.Net.HttpStatusCode.BadRequest;
                        jsonResponse = "{\"error\": {\"message\": \"Card declined\", \"type\": \"card_error\"}}";
                    }
                    else if (requestBody.Contains("charge=ch_fail_timeout"))
                    {
                        statusCode = System.Net.HttpStatusCode.BadRequest;
                        jsonResponse = "{\"error\": {\"message\": \"Api Timeout\", \"type\": \"api_error\"}}";
                    }
                    else
                    {
                        jsonResponse = "{\"id\": \"re_test_123\", \"status\": \"succeeded\"}";
                    }
                }
                else if (path.Contains("/v1/transfers"))
                {
                    jsonResponse = "{\"id\": \"tr_test_123\"}";
                }
                else
                {
                    jsonResponse = "{\"messageId\": \"test-id\"}";
                }

                return new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(jsonResponse)
                };
            }
        }

        protected IConfiguration CreateTestConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:SecretKey", "super_secret_key_123456789012345678"},
                {"Jwt:Issuer", "EventPlatform"},
                {"Jwt:Audience", "EventPlatformUsers"},
                {"Jwt:ExpiryHours", "24"}
            };
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string apiDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Event.API"));
            string appSettingsPath = Path.Combine(apiDir, "appsettings.json");
            return new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        protected HttpClient CreateMockHttpClient()
        {
            return new HttpClient(new FakeHttpMessageHandler());
        }

        protected ICacheService CreateConcreteCacheService()
        {
            var cacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            var cache = new MemoryDistributedCache(cacheOptions);
            return new CacheService(cache);
        }

        protected IEmailService CreateConcreteEmailService(IConfiguration? config = null)
        {
            config ??= CreateTestConfiguration();
            return new EmailService(config);
        }

        protected IVirtualMeetingService CreateConcreteVirtualMeetingService()
        {
            return new VirtualMeetingService();
        }

        protected IPaymentService CreateConcretePaymentService(IConfiguration? config = null)
        {
            config ??= CreateTestConfiguration();
            var stripeClient = new Stripe.StripeClient(
                apiKey: config["Stripe:ApiKey"] ?? "sk_test_mock",
                httpClient: new Stripe.SystemNetHttpClient(CreateMockHttpClient())
            );
            Stripe.StripeConfiguration.StripeClient = stripeClient;
            return new StripePaymentService(config);
        }

        protected IQrCodeService CreateConcreteQrCodeService()
        {
            return new QrCodeService();
        }

        protected void LogSubTest(string serviceName, string testCaseName, bool success, string? details = null)
        {
            string result = success ? "Successful" : "Failed";
            Log.Information("Service: {ServiceName}, Test Case: {TestCaseName}, Result: {ResultStatus}{Details}", 
                serviceName, testCaseName, result, details != null ? $" ({details})" : "");
        }

        protected void LogTestDetail(
            string serviceName, 
            string functionName, 
            string scenario, 
            object? input, 
            object? output, 
            bool success, 
            string? errorMessage = null)
        {
            string result = success ? "SUCCESS" : "FAILED";
            
            var jsonOptions = new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = false,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };

            string inputJson = input != null ? System.Text.Json.JsonSerializer.Serialize(input, jsonOptions) : "null";
            string outputJson = output != null ? System.Text.Json.JsonSerializer.Serialize(output, jsonOptions) : "null";

            Log.Information(
                "\n========================================\n" +
                "SERVICE: {ServiceName}\n" +
                "FUNCTION: {FunctionName}\n" +
                "CASE: {Scenario}\n" +
                "INPUT: {InputData}\n" +
                "OUTPUT: {OutputData}\n" +
                "RESULT: {ResultStatus}\n" +
                (success ? "" : "ERROR: " + errorMessage + "\n") +
                "========================================\n",
                serviceName, functionName, scenario, inputJson, outputJson, result);
        }

        public virtual void Dispose()
        {
            Log.Information("----------------------------------------------------------------------\n");
        }
    }
}
