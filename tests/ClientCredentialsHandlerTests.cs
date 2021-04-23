using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

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
    }
}
