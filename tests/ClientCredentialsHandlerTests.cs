using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

using FluentAssertions;

using NSubstitute;

using NUnit.Framework;

using RichardSzalay.MockHttp;

using static NSubstitute.Arg;

namespace Brighid.Identity.Client
{
#pragma warning disable IDE0055
    [Category("Unit")]
    public class ClientCredentialsHandlerTests
    {
        [Test, Auto]
        public async Task SendAsync_ShouldUseDefaultToken_IfGiven(
            string response,
            Uri uri,
            string token,
            HttpRequestMessage requestMessage,
            [NotNull, Target] ClientCredentialsHandler handler
        )
        {
            var cancellationToken = new CancellationToken(false);
            using var mockHttp = new MockHttpMessageHandler();
            handler.InnerHandler = mockHttp;
            mockHttp
            .Expect(uri.ToString())
            .WithHeaders("Authorization", $"Bearer {token}")
            .Respond("text/plain", response);

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            requestMessage.RequestUri = uri;
            await client.SendAsync(requestMessage, cancellationToken);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Test, Auto]
        public async Task SendAsync_AttachesIdentityToken_ToRequests(
            string response,
            Uri uri,
            string token,
            HttpRequestMessage requestMessage,
            [NotNull, Substitute, Frozen] ITokenStore tokenStore,
            [NotNull, Target] ClientCredentialsHandler handler
        )
        {
            var cancellationToken = new CancellationToken(false);
            using var mockHttp = new MockHttpMessageHandler();
            handler.InnerHandler = mockHttp;
            mockHttp
            .Expect(uri.ToString())
            .WithHeaders("Authorization", $"Bearer {token}")
            .Respond("text/plain", response);

            using var invoker = new HttpMessageInvoker(handler);
            tokenStore.GetIdToken(Any<CancellationToken>()).Returns(token);

            requestMessage.RequestUri = uri;
            await invoker.SendAsync(requestMessage, cancellationToken);

            await tokenStore.Received().GetIdToken(Is(cancellationToken));
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Test, Auto]
        public async Task SendAsync_InvalidatesAndRefreshesToken_If401WasReceived(
            Uri uri,
            string outdatedToken,
            string upToDateToken,
            HttpRequestMessage requestMessage,
            [NotNull, Substitute, Frozen] ITokenStore tokenStore,
            [NotNull, Target] ClientCredentialsHandler handler
        )
        {
            var cancellationToken = new CancellationToken(false);
            using var mockHttp = new MockHttpMessageHandler();
            handler.InnerHandler = mockHttp;
            mockHttp
            .When(uri.ToString())
            .WithHeaders("Authorization", $"Bearer {outdatedToken}")
            .Respond(HttpStatusCode.Unauthorized);

            mockHttp
            .When(uri.ToString())
            .WithHeaders("Authorization", $"Bearer {upToDateToken}")
            .Respond(HttpStatusCode.OK);

            using var invoker = new HttpMessageInvoker(handler);
            tokenStore.GetIdToken(Any<CancellationToken>()).Returns(outdatedToken, upToDateToken);

            requestMessage.RequestUri = uri;
            var response = await invoker.SendAsync(requestMessage, cancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            Received.InOrder(() =>
            {
                tokenStore.GetIdToken(Is(cancellationToken));
                tokenStore.InvalidateToken();
                tokenStore.GetIdToken(Is(cancellationToken));
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Test, Auto]
        public async Task SendAsync_ShouldThrowRefreshTokenException_IfMaxAttemptsIsReached(
            Uri uri,
            string outdatedToken,
            HttpRequestMessage requestMessage,
            [Frozen] IdentityConfig config,
            [NotNull, Substitute, Frozen] ITokenStore tokenStore,
            [NotNull, Target] ClientCredentialsHandler handler
        )
        {
            config.MaxRefreshAttempts = 3;
            
            var cancellationToken = new CancellationToken(false);
            using var mockHttp = new MockHttpMessageHandler();
            handler.InnerHandler = mockHttp;
            mockHttp
            .When(uri.ToString())
            .WithHeaders("Authorization", $"Bearer {outdatedToken}")
            .Respond(HttpStatusCode.Unauthorized);

            using var invoker = new HttpMessageInvoker(handler);
            tokenStore.GetIdToken(Any<CancellationToken>()).Returns(outdatedToken);

            requestMessage.RequestUri = uri;
            Func<Task> func = () => invoker.SendAsync(requestMessage, cancellationToken);

            await func.Should().ThrowAsync<TokenRefreshException>();

            Received.InOrder(() =>
            {
                tokenStore.GetIdToken(Is(cancellationToken));
                tokenStore.InvalidateToken();
                tokenStore.GetIdToken(Is(cancellationToken));
                tokenStore.InvalidateToken();
                tokenStore.GetIdToken(Is(cancellationToken));
                tokenStore.InvalidateToken();
            });

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
