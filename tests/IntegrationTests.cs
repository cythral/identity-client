using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Brighid.Identity.Client.Utils;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using NUnit.Framework;

using RichardSzalay.MockHttp;

#pragma warning disable CS0436

namespace Brighid.Identity.Client
{
    public interface ITestIdentityService
    {
        Version DefaultRequestVersion { get; }
        HttpVersionPolicy DefaultVersionPolicy { get; }
        Task SendAsync();
        Task SendImpersonateAsync(string userId, string audience);
    }

    public interface ITestIdentityService2
    {
        Version DefaultRequestVersion { get; }
        HttpVersionPolicy DefaultVersionPolicy { get; }
        Task SendAsync();
        Task SendImpersonateAsync(string userId, string audience);
    }

    public class TestIdentityService : ITestIdentityService, ITestIdentityService2
    {
        private readonly HttpClient httpClient;

        public TestIdentityService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public Version DefaultRequestVersion => httpClient.DefaultRequestVersion;

        public HttpVersionPolicy DefaultVersionPolicy => httpClient.DefaultVersionPolicy;

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
        private const string KeyId = "6a7d1d78-e850-4aac-883f-4a9288975311";
        private const string XKeyParameter = "SmgC2as91UymLbtIm-AK4xSJ_zOSLacRXCxWAR38SYw";
        private const string YKeyParameter = "Knjm1eac5fInvzceaOObmCinmYlMaZMXsgMGICPeQVg";
        private const string DKeyParameter = "DRGZ-dQRTwB2MoK-hjniJaTTQ3TDtlpK2YUgUV1GgXY";

        private static void SetupJwksResponse(MockHttpMessageHandler handler, Uri baseIdpAddress)
        {
            handler
            .When(HttpMethod.Get, $"{baseIdpAddress}.well-known/jwks")
            .Respond("application/json", $@"
                {{ 
                    ""keys"": [
                        {{
                            ""kid"": ""{KeyId}"",
                            ""x"": ""{XKeyParameter}"",
                            ""y"": ""{YKeyParameter}""
                        }}
                    ]
                }}
            ");
        }

        private static (string idToken, string accessToken) CreateTokenPair(Uri baseIdpAddress)
        {
            var ecdsaPublicKey = new ECPoint { X = Base64UrlEncoder.DecodeBytes(XKeyParameter), Y = Base64UrlEncoder.DecodeBytes(YKeyParameter) };
            var ecdsaParams = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = ecdsaPublicKey, D = Base64UrlEncoder.DecodeBytes(DKeyParameter) };
            using var ecdsa = ECDsa.Create(ecdsaParams);
            using var hasher = SHA256.Create();

            var accessToken = Guid.NewGuid().ToString();
            var accessTokenBytes = Encoding.ASCII.GetBytes(accessToken);
            var accessTokenHashBytes = hasher.ComputeHash(accessTokenBytes);
            var accessTokenHash = Base64UrlEncoder.Encode(accessTokenHashBytes[0..(accessTokenHashBytes.Length / 2)]);

            var expirationTime = (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5)).ToUnixTimeSeconds();
            var idTokenHeader = Base64UrlEncoder.Encode(@$"{{""alg"":""ES256"",""kid"":""{KeyId}"",""typ"":""JWT""}}");
            var idTokenBody = Base64UrlEncoder.Encode($@"{{""at_hash"":""{accessTokenHash}"",""exp"":{expirationTime},""iss"":""{baseIdpAddress}""}}");
            var jws = $"{idTokenHeader}.{idTokenBody}";
            var jwsBytes = Encoding.ASCII.GetBytes(jws);
            var idTokenSignatureBytes = ecdsa.SignData(jwsBytes, HashAlgorithmName.SHA256);
            var idTokenSignature = Base64UrlEncoder.Encode(idTokenSignatureBytes);
            var idToken = $"{jws}.{idTokenSignature}";

            return (idToken, accessToken);
        }

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
        public void Http2ShouldBeUsedIfIndicated(
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
            serviceCollection.UseBrighidIdentityWithHttp2<ITestIdentityService, TestIdentityService>(baseServiceAddress);

            var provider = serviceCollection.BuildServiceProvider();
            var identityService = provider.GetRequiredService<ITestIdentityService>();

            identityService.DefaultRequestVersion.Should().Be(new Version(2, 0));
            identityService.DefaultVersionPolicy.Should().Be(HttpVersionPolicy.RequestVersionExact);
        }

        [Test, Auto]
        public async Task ClientCredentialsFlow(
            Uri baseIdpAddress,
            Uri baseServiceAddress,
            string clientId,
            string clientSecret,
            string audience
        )
        {
            var (idToken, accessToken) = CreateTokenPair(baseIdpAddress);
            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Identity:IdentityServerUri"] = baseIdpAddress.ToString(),
                ["Identity:ClientId"] = clientId,
                ["Identity:ClientSecret"] = clientSecret,
                ["Identity:Audience"] = audience,
            }).Build();

            var mockHandler = new MockHttpMessageHandler(BackendDefinitionBehavior.Always);
            SetupJwksResponse(mockHandler, baseIdpAddress);

            mockHandler
            .Expect(HttpMethod.Post, $"{baseIdpAddress}oauth2/token")
            .WithHeaders("user-agent", $"BrighidIdentityClient/{ThisAssembly.AssemblyInformationalVersion}")
            .WithFormData("client_id", clientId)
            .WithFormData("client_secret", clientSecret)
            .WithFormData("grant_type", "client_credentials")
            .WithFormData("audience", audience)
            .Respond("application/json", $@"{{""id_token"":""{idToken}"",""access_token"":""{accessToken}""}}");

            mockHandler
            .Expect(HttpMethod.Get, $"{baseServiceAddress}")
            .WithHeaders("user-agent", $"BrighidIdentityClient/{ThisAssembly.AssemblyInformationalVersion}")
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
            string clientId,
            string clientSecret,
            string userId,
            string audience
        )
        {
            var (idToken, accessToken) = CreateTokenPair(baseIdpAddress);
            var (impersonateIdToken, impersonateAccessToken) = CreateTokenPair(baseIdpAddress);

            var serviceCollection = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Identity:IdentityServerUri"] = baseIdpAddress.ToString(),
                ["Identity:ClientId"] = clientId,
                ["Identity:ClientSecret"] = clientSecret,
            }).Build();

            var mockHandler = new MockHttpMessageHandler(BackendDefinitionBehavior.Always);
            SetupJwksResponse(mockHandler, baseIdpAddress);

            mockHandler
            .Expect(HttpMethod.Post, $"{baseIdpAddress}oauth2/token")
            .WithHeaders("user-agent", $"BrighidIdentityClient/{ThisAssembly.AssemblyInformationalVersion}")
            .WithFormData("client_id", clientId)
            .WithFormData("client_secret", clientSecret)
            .WithFormData("grant_type", "client_credentials")
            .Respond("application/json", $@"{{""id_token"":""{idToken}"",""access_token"":""{accessToken}""}}");

            mockHandler
            .Expect(HttpMethod.Post, $"{baseIdpAddress}oauth2/token")
            .WithHeaders("user-agent", $"BrighidIdentityClient/{ThisAssembly.AssemblyInformationalVersion}")
            .WithFormData("access_token", accessToken)
            .WithFormData("user_id", userId)
            .WithFormData("audience", audience)
            .WithFormData("grant_type", "impersonate")
            .Respond("application/json", $@"{{""id_token"":""{impersonateIdToken}"",""access_token"":""{impersonateAccessToken}""}}");

            mockHandler
            .Expect(HttpMethod.Get, $"{baseServiceAddress}")
            .WithHeaders("authorization", $"Bearer {impersonateAccessToken}")
            .Respond("application/text", "OK");

            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.ConfigureBrighidIdentity<CustomIdentityConfig>(configuration.GetSection("Identity"), mockHandler);
            serviceCollection.UseBrighidIdentity<ITestIdentityService, TestIdentityService>(baseServiceAddress);
            serviceCollection.UseBrighidIdentity<ITestIdentityService2, TestIdentityService>(baseServiceAddress);
            serviceCollection.Configure<CustomIdentityConfig>(configuration.GetSection("Identity"));

            var provider = serviceCollection.BuildServiceProvider();
            var service = provider.GetRequiredService<ITestIdentityService>();

            await service.SendImpersonateAsync(userId, audience);

            Action func = () => provider.GetRequiredService<ITestIdentityService2>();
            func.Should().NotThrow();
            mockHandler.VerifyNoOutstandingExpectation();
        }
    }
}
