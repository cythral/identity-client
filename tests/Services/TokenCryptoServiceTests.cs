using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

using NUnit.Framework;

using RichardSzalay.MockHttp;

namespace Brighid.Identity.Client
{
    public class TokenCryptoServiceTests
    {

        [TestFixture]
        public class GetKeyIdForToken
        {
            [Test, Auto]
            public void ShouldReturnTheKidClaimFromTheJwtHeader(
                [Target] TokenCryptoService service
            )
            {
                var jwt = "eyJraWQiOiIxMjMifQ==.e30=.";

                var keyId = service.GetKeyIdForToken(jwt);

                keyId.Should().Be("123");
            }

            [Test, Auto]
            public void ShouldThrowIfKidClaimNotPresent(
                [Target] TokenCryptoService service
            )
            {
                var jwt = "e30=.e30=.";

                Func<string> func = () => service.GetKeyIdForToken(jwt);

                func.Should().Throw<KeyIdMissingException>();
            }
        }

        [TestFixture]
        public class FetchSigningKey
        {
            private const string Response = @"
{
    ""keys"": [
        {
            ""kid"": ""1A0DF5CA6613F6D2F0FA86C0A27326408788ED28"",
            ""use"": ""sig"",
            ""kty"": ""EC"",
            ""alg"": ""ES256"",
            ""crv"": ""P-256"",
            ""x"": ""KdWCtuMuVV54gKBZoq7PaggpdBsW35RkS6tSdjY_VEo"",
            ""y"": ""VGwkO6wDh95WGbsnUkzxu-xOsPA2xTrQK-y-LFeyp0E""
        },
        {
            ""kid"": ""DE66D56741E20A3823BD9130F742E584080B50CE"",
            ""use"": ""sig"",
            ""kty"": ""EC"",
            ""alg"": ""ES256"",
            ""crv"": ""P-256"",
            ""x"": ""r-_9UPUR7X4SN1P_uCZnZny-JZ3vGZ-OpMyltj54G28"",
            ""y"": ""NOOUDEa4j_2b5_hBFI_0zN2kTP5ZxcLZj8cjAYUVXD4""
        }
    ]
}";

            [Test, Auto]
            public async Task ShouldSendRequestToJwksEndpoint(
                Uri baseAddress,
                CancellationToken cancellationToken
            )
            {
                var mockHandler = new MockHttpMessageHandler();
                var httpClient = new HttpClient(mockHandler) { BaseAddress = baseAddress };

                mockHandler
                .Expect($"{baseAddress}.well-known/jwks")
                .Respond("application/json", Response);

                var signingKeyFetcher = new TokenCryptoService(httpClient);
                await signingKeyFetcher.FetchSigningKey("DE66D56741E20A3823BD9130F742E584080B50CE", cancellationToken);

                mockHandler.VerifyNoOutstandingExpectation();
            }

            [Test, Auto]
            public async Task ShouldReturnTheCorrectKey(
                Uri baseAddress,
                CancellationToken cancellationToken
            )
            {
                var keyId = "DE66D56741E20A3823BD9130F742E584080B50CE";
                var mockHandler = new MockHttpMessageHandler();
                var httpClient = new HttpClient(mockHandler) { BaseAddress = baseAddress };

                mockHandler
                .Expect($"{baseAddress}.well-known/jwks")
                .Respond("application/json", Response);

                var signingKeyFetcher = new TokenCryptoService(httpClient);
                var result = await signingKeyFetcher.FetchSigningKey(keyId, cancellationToken);

                result.Should().BeOfType<ECDsaSecurityKey>()
                .Which.KeyId.Should().Be(keyId);
            }

            [Test, Auto]
            public async Task ShouldThrowIfTheSigningKeyWasNotFound(
                Uri baseAddress,
                CancellationToken cancellationToken
            )
            {
                var keyId = "ABCDE";
                var mockHandler = new MockHttpMessageHandler();
                var httpClient = new HttpClient(mockHandler) { BaseAddress = baseAddress };

                mockHandler
                .Expect($"{baseAddress}.well-known/jwks")
                .Respond("application/json", Response);

                var signingKeyFetcher = new TokenCryptoService(httpClient);
                Func<Task> func = () => signingKeyFetcher.FetchSigningKey(keyId, cancellationToken);

                (await func.Should().ThrowAsync<SigningKeyNotFoundException>())
                .And.KeyId.Should().Be(keyId);
            }
        }

        [TestFixture]
        public class GetAccessTokenHash
        {
            [Test, Auto]
            public void ShouldReturnTheAccessTokenHashClaim(
                string hash,
                string name,
                [Target] TokenCryptoService service
            )
            {
                var identity = new ClaimsIdentity();
                var claimsPrincipal = new ClaimsPrincipal(identity);
                identity.AddClaim(new Claim(ClaimTypes.Name, name));
                identity.AddClaim(new Claim(IdentityClientConstants.ClaimTypes.AccessTokenHash, hash));


                var result = service.GetAccessTokenHash(claimsPrincipal);

                result.Should().Be(hash);
            }
        }

        [TestFixture]
        public class ValidateAccessTokenHash
        {
            [Test, Auto]
            public void ShouldThrowIfAccessTokenHashIsNotValid(
                [Target] TokenCryptoService service
            )
            {
                var algo = "SHA256";
                var hash = "abcdefg";
                var accessToken = "eyJhbGciOiJFUzI1NiIsImtpZCI6IjFBMERGNUNBNjYxM0Y2RDJGMEZBODZDMEEyNzMyNjQwODc4OEVEMjgiLCJ0eXAiOiJhdCtqd3QifQ.eyJuYW1lIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwic3ViIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwicm9sZSI6WyJCYXNpYyJdLCJvaV9wcnN0IjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0IiwiY2xpZW50X2lkIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwib2lfdGtuX2lkIjoiYTk0ZDNlNzAtNDJiZS00YjEzLWIxN2YtZWJjY2E2NDM3OGNjIiwiYXVkIjoiaWRlbnRpdHkuYnJpZ2guaWQiLCJzY29wZSI6Im9wZW5pZCIsImV4cCI6MTYyODEzODMyNiwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS5icmlnaC5pZC8iLCJpYXQiOjE2MjgxMzQ3MjZ9.QOcdciV9nviOBQJ3Q5n2c-MMVFX51JlBUCZeWUe3Pq2SpyaNzerS3A2iTwUTqPwmxjjVajO5BbNCVh84thk12w";

                Action func = () => service.ValidateAccessTokenHash(algo, hash, accessToken);

                func.Should().Throw<InvalidAccessTokenHashException>();
            }

            [Test, Auto]
            public void ShouldNotThrowIfAccessTokenHashIsValid(
                [Target] TokenCryptoService service
            )
            {
                var algo = "SHA256";
                var hash = "YjVtYQJSP6lYafgGBENHMg";
                var accessToken = "eyJhbGciOiJFUzI1NiIsImtpZCI6IjFBMERGNUNBNjYxM0Y2RDJGMEZBODZDMEEyNzMyNjQwODc4OEVEMjgiLCJ0eXAiOiJhdCtqd3QifQ.eyJuYW1lIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwic3ViIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwicm9sZSI6WyJCYXNpYyJdLCJvaV9wcnN0IjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0IiwiY2xpZW50X2lkIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwib2lfdGtuX2lkIjoiYTk0ZDNlNzAtNDJiZS00YjEzLWIxN2YtZWJjY2E2NDM3OGNjIiwiYXVkIjoiaWRlbnRpdHkuYnJpZ2guaWQiLCJzY29wZSI6Im9wZW5pZCIsImV4cCI6MTYyODEzODMyNiwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS5icmlnaC5pZC8iLCJpYXQiOjE2MjgxMzQ3MjZ9.QOcdciV9nviOBQJ3Q5n2c-MMVFX51JlBUCZeWUe3Pq2SpyaNzerS3A2iTwUTqPwmxjjVajO5BbNCVh84thk12w";

                Action func = () => service.ValidateAccessTokenHash(algo, hash, accessToken);

                func.Should().NotThrow<InvalidAccessTokenHashException>();
            }
        }
    }
}
