using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Brighid.Identity.Client.Utils;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        Task SendImpersonateAsync(string userId, string audience);
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

        public Task SendImpersonateAsync(string userId, string audience)
        {
            var message = new HttpRequestMessage { Method = HttpMethod.Get, RequestUri = new Uri("", UriKind.Relative) };
            message.Headers.Add("x-impersonate-userId", userId);
            message.Headers.Add("x-impersonate-audience", audience);
            return httpClient.SendAsync(message);
        }
    }

    [TestFixture]
    [Category("Integration")]
    public class IntegrationTests
    {
        [Test, Auto]
        public void CacheUtilsShouldBeInjected(
            Uri baseIdpAddress,
            Uri baseServiceAddress,
            string clientId,
            string clientSecret
        )
        {
            var serviceCollection = new ServiceCollection();
            var mockHandler = new MockHttpMessageHandler();
            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Identity:IdentityServerUri"] = baseIdpAddress.ToString(),
                ["Identity:ClientId"] = clientId,
                ["Identity:ClientSecret"] = clientSecret,
            }).Build();

            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.ConfigureBrighidIdentity<CustomIdentityConfig>(configuration.GetSection("Identity"), mockHandler);
            serviceCollection.UseBrighidIdentity<ITestIdentityService, TestIdentityService>(baseServiceAddress);

            var provider = serviceCollection.BuildServiceProvider();
            var cacheUtils = provider.GetService<ICacheUtils>();

            cacheUtils.Should().NotBeNull();
        }

        [Test, Auto]
        public void OptionsShouldBeInjected(
            Uri baseIdpAddress,
            Uri baseServiceAddress,
            string clientId,
            string clientSecret
        )
        {
            var serviceCollection = new ServiceCollection();
            var mockHandler = new MockHttpMessageHandler();
            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Identity:IdentityServerUri"] = baseIdpAddress.ToString(),
                ["Identity:ClientId"] = clientId,
                ["Identity:ClientSecret"] = clientSecret,
            }).Build();

            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.ConfigureBrighidIdentity<CustomIdentityConfig>(configuration.GetSection("Identity"), mockHandler);
            serviceCollection.UseBrighidIdentity<ITestIdentityService, TestIdentityService>(baseServiceAddress);

            var provider = serviceCollection.BuildServiceProvider();
            var identityOptions = provider.GetService<IOptions<CustomIdentityConfig>>();

            identityOptions.Should().NotBeNull();
        }

        [Test, Auto]
        public async Task ClientCredentialsFlow(
            Uri baseIdpAddress,
            Uri baseServiceAddress,
            string accessToken,
            string clientId,
            string clientSecret,
            string audience
        )
        {
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Identity:IdentityServerUri"] = baseIdpAddress.ToString(),
                ["Identity:ClientId"] = clientId,
                ["Identity:ClientSecret"] = clientSecret,
                ["Identity:Audience"] = audience,
            }).Build();

            var mockHandler = new MockHttpMessageHandler();
            mockHandler
            .Expect(HttpMethod.Post, $"{baseIdpAddress}oauth2/token")
            .WithFormData("client_id", clientId)
            .WithFormData("client_secret", clientSecret)
            .WithFormData("grant_type", "client_credentials")
            .WithFormData("audience", audience)
            .Respond("application/json", $@"{{""access_token"":""{accessToken}""}}");

            mockHandler
            .Expect(HttpMethod.Get, $"{baseServiceAddress}")
            .WithHeaders("authorization", $"Bearer {accessToken}")
            .Respond("application/text", "OK");

            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.ConfigureBrighidIdentity<CustomIdentityConfig>(configuration.GetSection("Identity"), mockHandler);
            serviceCollection.UseBrighidIdentity<ITestIdentityService, TestIdentityService>(baseServiceAddress);
            serviceCollection.Configure<CustomIdentityConfig>(configuration.GetSection("Identity"));

            var provider = serviceCollection.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestIdentityService>();

            await service.SendAsync();

            mockHandler.VerifyNoOutstandingExpectation();
        }

        [Test, Auto]
        public async Task ImpersonateFlow(
            Uri baseIdpAddress,
            Uri baseServiceAddress,
            string accessToken,
            string impersonateToken,
            string clientId,
            string clientSecret,
            string userId,
            string audience
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
            .Expect(HttpMethod.Post, $"{baseIdpAddress}oauth2/token")
            .WithFormData("access_token", accessToken)
            .WithFormData("user_id", userId)
            .WithFormData("audience", audience)
            .WithFormData("grant_type", "impersonate")
            .Respond("application/json", $@"{{""access_token"":""{impersonateToken}""}}");

            mockHandler
            .Expect(HttpMethod.Get, $"{baseServiceAddress}")
            .WithHeaders("authorization", $"Bearer {impersonateToken}")
            .Respond("application/text", "OK");

            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.ConfigureBrighidIdentity<CustomIdentityConfig>(configuration.GetSection("Identity"), mockHandler);
            serviceCollection.UseBrighidIdentity<ITestIdentityService, TestIdentityService>(baseServiceAddress);
            serviceCollection.Configure<CustomIdentityConfig>(configuration.GetSection("Identity"));

            var provider = serviceCollection.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestIdentityService>();

            await service.SendImpersonateAsync(userId, audience);

            mockHandler.VerifyNoOutstandingExpectation();
        }
    }
}
