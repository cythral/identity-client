using System;
using System.Threading;
using System.Threading.Tasks;

using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

using FluentAssertions;

using NSubstitute;

using NUnit.Framework;

using static NSubstitute.Arg;

namespace Brighid.Identity.Client
{
    [Category("Unit")]
    public class TokenStoreTests
    {
        [TestFixture]
        [Category("Unit")]
        public class GetTokenTests
        {
            [Test, Auto]
            public async Task ShouldFetchTokenIfItsNotInTheCache(
                Token token,
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] IdentityConfig clientCredentials,
                [Target] TokenStore<IdentityConfig> store
            )
            {
                var cancellationToken = new CancellationToken();
                client.ExchangeClientCredentialsForToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                var result = await store.GetToken(cancellationToken);

                result.Should().Be(token.AccessToken);
                await client.Received().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Is(clientCredentials.Audience), Any<CancellationToken>());
            }

            [Test, Auto]
            public async Task ShouldNotFetchTokenIfItsInTheCache(
                Token token,
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] IdentityConfig clientCredentials,
                [Target] TokenStore<IdentityConfig> store
            )
            {
                var cancellationToken = new CancellationToken();
                client.ExchangeClientCredentialsForToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                await store.GetToken(cancellationToken);
                client.ClearReceivedCalls();

                var result = await store.GetToken(cancellationToken);
                result.Should().Be(token.AccessToken);
                await client.DidNotReceive().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Is(clientCredentials.Audience), Any<CancellationToken>());
            }

            [Test, Auto]
            public async Task ShouldFetchTokenIfItsInTheCacheButHasExpired(
                Token token,
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] IdentityConfig clientCredentials,
                [Target] TokenStore<IdentityConfig> store
            )
            {
                token.ExpiresIn = 0;
                var cancellationToken = new CancellationToken();
                client.ExchangeClientCredentialsForToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                await store.GetToken(cancellationToken);
                client.ClearReceivedCalls();

                var result = await store.GetToken(cancellationToken);
                result.Should().Be(token.AccessToken);
                await client.Received().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Is(clientCredentials.Audience), Any<CancellationToken>());
            }

            [Test, Auto, Timeout(1000)]
            public async Task ShouldThrowPropagateExceptionIfExchangeFails(
                string message,
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] IdentityConfig clientCredentials,
                [Target] TokenStore<IdentityConfig> store
            )
            {
                var cancellationToken = new CancellationToken();
                var exception = new Exception(message);
                client.ExchangeClientCredentialsForToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns<Token>(x => throw exception);
                Func<Task> func = () => store.GetToken(cancellationToken);

                (await func.Should().ThrowAsync<Exception>()).And.Message.Should().Be(message);
                await client.Received().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Is(clientCredentials.Audience), Any<CancellationToken>());
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class InvalidateTokenTests
        {
            [Test, Auto]
            public async Task ShouldCauseSubsequentRequestsToRefetchToken(
                Token token,
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] IdentityConfig clientCredentials,
                [Target] TokenStore<IdentityConfig> store
            )
            {
                client.ExchangeClientCredentialsForToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                var cancellationToken = new CancellationToken();
                await store.GetToken(cancellationToken);

                await client.Received().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Is(clientCredentials.Audience), Any<CancellationToken>());
                client.ClearReceivedCalls();

                store.InvalidateToken();
                await store.GetToken(cancellationToken);
                await client.Received().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Is(clientCredentials.Audience), Any<CancellationToken>());
            }
        }
    }
}
