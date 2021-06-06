using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using NUnit.Framework;

using RichardSzalay.MockHttp;

using static System.Text.Json.JsonSerializer;

namespace Brighid.Identity.Client
{
    public class IdentityServerClientTests
    {
        [TestFixture]
        [Category("Unit")]

        public class ExchangeClientCredentialsForTokenTests
        {
            [Test, Auto]
            public async Task ShouldSendRequestWithClientId(
            Uri baseAddress,
            string clientId,
            string clientSecret,
            Token response
        )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .WithFormData("client_id", clientId)
                .Respond("application/json", Serialize(response));

                await client.ExchangeClientCredentialsForToken(clientId, clientSecret);

                handler.VerifyNoOutstandingExpectation();
            }

            [Test, Auto]
            public async Task ShouldSendRequestWithClientSecret(
                Uri baseAddress,
                string clientId,
                string clientSecret,
                Token response
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .WithFormData("client_secret", clientSecret)
                .Respond("application/json", Serialize(response));

                await client.ExchangeClientCredentialsForToken(clientId, clientSecret);

                handler.VerifyNoOutstandingExpectation();
            }

            [Test, Auto]
            public async Task ShouldSendRequestWithGrantTypeClientCredentials(
                Uri baseAddress,
                string clientId,
                string clientSecret,
                Token response
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .WithFormData("grant_type", "client_credentials")
                .Respond("application/json", Serialize(response));

                await client.ExchangeClientCredentialsForToken(clientId, clientSecret);

                handler.VerifyNoOutstandingExpectation();
            }

            [Test, Auto]
            public async Task ShouldReturnToken(
                Uri baseAddress,
                string clientId,
                string clientSecret,
                Token token
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .Respond("application/json", Serialize(token));

                var response = await client.ExchangeClientCredentialsForToken(clientId, clientSecret);

                response.Should().BeEquivalentTo(token, options =>
                    options.Excluding(t => t.CreationDate)
                );

                handler.VerifyNoOutstandingExpectation();
            }

            [Test, Auto]
            public async Task ShouldThrowIfTokenIsNull(
                Uri baseAddress,
                string clientId,
                string clientSecret
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .Respond("application/json", "null");

                Func<Task<Token>> func = async () => await client.ExchangeClientCredentialsForToken(clientId, clientSecret);
                await func.Should().ThrowAsync<Exception>();

                handler.VerifyNoOutstandingExpectation();
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class ExchangeAccessTokenForImpersonateTokenTests
        {
            [Test, Auto]
            public async Task ShouldSendRequestWithAccessToken(
                Uri baseAddress,
                string accessToken,
                string userId,
                string audience,
                Token response,
                CancellationToken cancellationToken
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .WithFormData("access_token", accessToken)
                .Respond("application/json", Serialize(response));

                await client.ExchangeAccessTokenForImpersonateToken(accessToken, userId, audience, cancellationToken);

                handler.VerifyNoOutstandingExpectation();
            }

            [Test, Auto]
            public async Task ShouldSendRequestWithUserId(
                Uri baseAddress,
                string accessToken,
                string userId,
                string audience,
                Token response,
                CancellationToken cancellationToken
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .WithFormData("user_id", userId)
                .Respond("application/json", Serialize(response));

                await client.ExchangeAccessTokenForImpersonateToken(accessToken, userId, audience, cancellationToken);

                handler.VerifyNoOutstandingExpectation();
            }

            [Test, Auto]
            public async Task ShouldSendRequestWithAudience(
                Uri baseAddress,
                string accessToken,
                string userId,
                string audience,
                Token response,
                CancellationToken cancellationToken
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .WithFormData("audience", audience)
                .Respond("application/json", Serialize(response));

                await client.ExchangeAccessTokenForImpersonateToken(accessToken, userId, audience, cancellationToken);

                handler.VerifyNoOutstandingExpectation();
            }

            [Test, Auto]
            public async Task ShouldSendRequestWithImpersonateGrantType(
                Uri baseAddress,
                string accessToken,
                string userId,
                string audience,
                Token response,
                CancellationToken cancellationToken
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .WithFormData("grant_type", "impersonate")
                .Respond("application/json", Serialize(response));

                await client.ExchangeAccessTokenForImpersonateToken(accessToken, userId, audience, cancellationToken);

                handler.VerifyNoOutstandingExpectation();
            }

            [Test, Auto]
            public async Task ShouldReturnToken(
                Uri baseAddress,
                string accessToken,
                string userId,
                string audience,
                Token response,
                CancellationToken cancellationToken
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .Respond("application/json", Serialize(response));

                var result = await client.ExchangeAccessTokenForImpersonateToken(accessToken, userId, audience, cancellationToken);

                result.Should().BeEquivalentTo(response, options =>
                    options.Excluding(token => token.CreationDate)
                );
            }

            [Test, Auto]
            public async Task ShouldThrowIfTokenIsNull(
                Uri baseAddress,
                string accessToken,
                string userId,
                string audience,
                CancellationToken cancellationToken
            )
            {
                using var handler = new MockHttpMessageHandler();
                using var httpClient = new HttpClient(handler) { BaseAddress = baseAddress };
                var client = new IdentityServerClient(httpClient);

                handler
                .Expect(HttpMethod.Post, $"{baseAddress}oauth2/token")
                .Respond("application/json", "null");

                Func<Task<Token>> func = async () => await client.ExchangeAccessTokenForImpersonateToken(accessToken, userId, audience, cancellationToken);
                await func.Should().ThrowAsync<Exception>();

                handler.VerifyNoOutstandingExpectation();
            }
        }
    }
}
