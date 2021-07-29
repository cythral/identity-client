using System;
using System.Threading;
using System.Threading.Tasks;

using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

using FluentAssertions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using NUnit.Framework;

using static NSubstitute.Arg;

namespace Brighid.Identity.Client.Stores
{
    internal class UserTokenStoreTests
    {
        [TestFixture]
        [Category("Unit")]
        public class GetUserTokenTests
        {
            [Test, Auto]
            public async Task ShouldFetchTokenIfItsNotInTheCache(
                string userId,
                string audience,
                string accessToken,
                Token token,
                [Frozen, Substitute] ITokenStore tokenStore,
                [Frozen, Substitute] IdentityServerClient client,
                [Target] UserTokenStore store
            )
            {
                var cancellationToken = new CancellationToken();
                tokenStore.GetToken(Any<CancellationToken>()).Returns(accessToken);
                client.ExchangeAccessTokenForImpersonateToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                var result = await store.GetUserToken(userId, audience, cancellationToken);

                result.Should().Be(token.AccessToken);
                await tokenStore.Received().GetToken(Any<CancellationToken>());
                await client.Received().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId), Is(audience), Any<CancellationToken>());
            }

            [Test, Auto]
            public async Task ShouldNotFetchTokenIfItsInTheCache(
                string userId,
                string audience,
                string accessToken,
                Token token,
                [Frozen, Substitute] ITokenStore tokenStore,
                [Frozen, Substitute] IdentityServerClient client,
                [Target] UserTokenStore store
            )
            {
                var cancellationToken = new CancellationToken();
                tokenStore.GetToken(Any<CancellationToken>()).Returns(accessToken);
                client.ExchangeAccessTokenForImpersonateToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                await store.GetUserToken(userId, audience, cancellationToken);
                client.ClearReceivedCalls();

                var result = await store.GetUserToken(userId, audience, cancellationToken);
                result.Should().Be(token.AccessToken);

                await client.DidNotReceive().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId), Is(audience), Any<CancellationToken>());
            }

            [Test, Auto]
            public async Task ShouldFetchNewTokenIfUserIdIsTheSameButAudienceIsDifferent(
                string userId,
                string audience1,
                string audience2,
                string accessToken,
                Token token,
                [Frozen, Substitute] ITokenStore tokenStore,
                [Frozen, Substitute] IdentityServerClient client,
                [Target] UserTokenStore store
            )
            {
                var cancellationToken = new CancellationToken();
                tokenStore.GetToken(Any<CancellationToken>()).Returns(accessToken);
                client.ExchangeAccessTokenForImpersonateToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                await store.GetUserToken(userId, audience1, cancellationToken);
                client.ClearReceivedCalls();

                var result = await store.GetUserToken(userId, audience2, cancellationToken);
                result.Should().Be(token.AccessToken);

                await client.Received().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId), Is(audience2), Any<CancellationToken>());
            }

            [Test, Auto]
            public async Task ShouldFetchTokenIfItsInTheCacheButHasExpired(
                string userId,
                string audience,
                string accessToken,
                Token token,
                [Frozen, Substitute] ITokenStore tokenStore,
                [Frozen, Substitute] IdentityServerClient client,
                [Target] UserTokenStore store
            )
            {
                token.ExpiresIn = 0;
                var cancellationToken = new CancellationToken();
                tokenStore.GetToken(Any<CancellationToken>()).Returns(accessToken);
                client.ExchangeAccessTokenForImpersonateToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                await store.GetUserToken(userId, audience, cancellationToken);
                client.ClearReceivedCalls();

                var result = await store.GetUserToken(userId, audience, cancellationToken);
                result.Should().Be(token.AccessToken);
                await client.Received().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId), Is(audience), Any<CancellationToken>());
            }

            [Test, Auto, Timeout(1000)]
            public async Task ShouldThrowPropagateExceptionIfExchangeFails(
                string userId,
                string audience,
                string message,
                string accessToken,
                [Frozen, Substitute] IdentityServerClient client,
                [Frozen, Substitute] ITokenStore tokenStore,
                [Target] UserTokenStore store
            )
            {
                var cancellationToken = new CancellationToken();
                var exception = new Exception(message);
                tokenStore.GetToken(Any<CancellationToken>()).Returns(accessToken);
                client.ExchangeAccessTokenForImpersonateToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Throws(exception);
                Func<Task> func = () => store.GetUserToken(userId, audience, cancellationToken);

                (await func.Should().ThrowAsync<Exception>()).And.Message.Should().Be(message);
                await client.Received().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId), Is(audience), Any<CancellationToken>());
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class InvalidateAllUserTokens
        {
            [Test, Auto]
            public async Task ShouldInvalidateTokensForAllUsers(
                string userId1,
                string userId2,
                string audience,
                string accessToken,
                Token token,
                [Frozen, Substitute] ITokenStore tokenStore,
                [Frozen, Substitute] IdentityServerClient client,
                [Target] UserTokenStore store
            )
            {
                var cancellationToken = new CancellationToken();
                tokenStore.GetToken(Any<CancellationToken>()).Returns(accessToken);
                client.ExchangeAccessTokenForImpersonateToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                await store.GetUserToken(userId1, audience, cancellationToken);
                await store.GetUserToken(userId2, audience, cancellationToken);
                store.InvalidateAllUserTokens();
                client.ClearReceivedCalls();

                await store.GetUserToken(userId1, audience, cancellationToken);
                await store.GetUserToken(userId2, audience, cancellationToken);

                await client.Received().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId1), Is(audience), Any<CancellationToken>());
                await client.Received().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId2), Is(audience), Any<CancellationToken>());
            }
        }

        [TestFixture]
        [Category("Unit")]
        public class InvalidateUserToken
        {
            [Test, Auto]
            public async Task ShouldInvalidateAllTokensForTheGivenUserId(
                string userId1,
                string userId2,
                string audience1,
                string audience2,
                string accessToken,
                Token token,
                [Frozen, Substitute] ITokenStore tokenStore,
                [Frozen, Substitute] IdentityServerClient client,
                [Target] UserTokenStore store
            )
            {
                var cancellationToken = new CancellationToken();
                tokenStore.GetToken(Any<CancellationToken>()).Returns(accessToken);
                client.ExchangeAccessTokenForImpersonateToken(Any<string>(), Any<string>(), Any<string>(), Any<CancellationToken>()).Returns(token);

                await store.GetUserToken(userId1, audience1, cancellationToken);
                await store.GetUserToken(userId1, audience2, cancellationToken);
                await store.GetUserToken(userId2, audience1, cancellationToken);
                store.InvalidateTokensForUser(userId1);
                client.ClearReceivedCalls();

                await store.GetUserToken(userId1, audience1, cancellationToken);
                await store.GetUserToken(userId1, audience2, cancellationToken);
                await store.GetUserToken(userId2, audience1, cancellationToken);

                await client.Received().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId1), Is(audience1), Any<CancellationToken>());
                await client.Received().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId1), Is(audience2), Any<CancellationToken>());
                await client.DidNotReceive().ExchangeAccessTokenForImpersonateToken(Is(accessToken), Is(userId2), Is(audience1), Any<CancellationToken>());
            }
        }
    }
}
