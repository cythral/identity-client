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
        public class GetIdToken
        {
            [Test, Auto]
            public async Task ShouldFetchTokenIfItsNotInTheCache(
                Token token,
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] IdentityConfig clientCredentials,
                [Target] TokenStore store
            )
            {
                var cancellationToken = new CancellationToken();
                client.ExchangeClientCredentialsForToken(Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                var result = await store.GetIdToken(cancellationToken);

                result.Should().Be(token.IdToken);
                await client.Received().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Any<CancellationToken>());
            }

            [Test, Auto]
            public async Task ShouldNotFetchTokenIfItsInTheCache(
                Token token,
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] IdentityConfig clientCredentials,
                [Target] TokenStore store
            )
            {
                var cancellationToken = new CancellationToken();
                client.ExchangeClientCredentialsForToken(Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                await store.GetIdToken(cancellationToken);
                client.ClearReceivedCalls();

                var result = await store.GetIdToken(cancellationToken);
                result.Should().Be(token.IdToken);
                await client.DidNotReceive().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Any<CancellationToken>());
            }

            [Test, Auto]
            public async Task ShouldFetchTokenIfItsInTheCacheButHasExpired(
                Token token,
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] IdentityConfig clientCredentials,
                [Target] TokenStore store
            )
            {
                token.ExpiresIn = 0;
                var cancellationToken = new CancellationToken();
                client.ExchangeClientCredentialsForToken(Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                await store.GetIdToken(cancellationToken);
                client.ClearReceivedCalls();

                var result = await store.GetIdToken(cancellationToken);
                result.Should().Be(token.IdToken);
                await client.Received().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Any<CancellationToken>());
            }

            [Test, Auto, Timeout(1000)]
            public async Task ShouldThrowOperationCanceledExceptionIfExchangeFails(
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] IdentityConfig clientCredentials,
                [Target] TokenStore store
            )
            {
                var cancellationToken = new CancellationToken();
                client.ExchangeClientCredentialsForToken(Any<string>(), Any<string>(), Any<CancellationToken>()).Returns<Token>(x => throw new Exception());
                Func<Task> func = () => store.GetIdToken(cancellationToken);

                await func.Should().ThrowAsync<OperationCanceledException>();
                await client.Received().ExchangeClientCredentialsForToken(Is(clientCredentials.ClientId), Is(clientCredentials.ClientSecret), Any<CancellationToken>());
            }
        }
    }
}
