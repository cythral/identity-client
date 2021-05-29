using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using RichardSzalay.MockHttp;

namespace Brighid.Identity.Client
{
    public class CustomIdentityConfig : IdentityConfig
    {
        public override string ClientId { get; set; } = string.Empty;

        public override string ClientSecret { get; set; } = string.Empty;
    }

    public interface ITestIdentityService
    {
        Task SendAsync();
    }

    public class TestIdentityService : ITestIdentityService
    {
        private readonly HttpClient httpClient;

        public TestIdentityService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public Task SendAsync()
        {
            return httpClient.SendAsync(new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = new Uri("", UriKind.Relative) });
        }
    }


    public class IntegrationTests
    {
        [Test, Auto]
        public async Task PreconfiguredFlow(
            Uri baseIdpAddress,
            Uri baseServiceAddress,
            string accessToken,
            string clientId,
            string clientSecret
        )
        {
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Identity:IdentityServerUri"] = baseIdpAddress.ToString(),
                ["Identity:ClientId"] = clientId,
                ["Identity:ClientSecret"] = clientSecret,
            }).Build();

            var mockHandler = new MockHttpMessageHandler();
            mockHandler
            .Expect(HttpMethod.Post, $"{baseIdpAddress}oauth2/token")
            .WithFormData("client_id", clientId)
            .WithFormData("client_secret", clientSecret)
            .WithFormData("grant_type", "client_credentials")
            .Respond("application/json", $@"{{""access_token"":""{accessToken}""}}");

            mockHandler
            .Expect(HttpMethod.Get, $"{baseServiceAddress}")
            .WithHeaders("authorization", $"Bearer {accessToken}")
            .Respond("application/text", "OK");

            serviceCollection.AddSingleton<HttpMessageHandler>(mockHandler);
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddSingleton<ITestIdentityService, TestIdentityService>();
            serviceCollection.ConfigureBrighidIdentity<CustomIdentityConfig>("Identity");
            serviceCollection.UseBrighidIdentity<ITestIdentityService, TestIdentityService>(baseServiceAddress);
            serviceCollection.Configure<CustomIdentityConfig>(configuration.GetSection("Identity"));

            var provider = serviceCollection.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestIdentityService>();

            await service.SendAsync();

            mockHandler.VerifyNoOutstandingExpectation();
        }
    }
}
