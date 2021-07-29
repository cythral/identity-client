using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

using Brighid.Identity.Client.Stores;

using NSubstitute;

using NUnit.Framework;

using static NSubstitute.Arg;

namespace Brighid.Identity.Client.Utils
{
    internal class DefaultCacheUtilsTests
    {
        [Test, Auto]
        public void InvalidatePrimaryToken_ShouldCallTokenStore_InvalidateToken(
            [Frozen, Substitute] ITokenStore tokenStore,
            [Target] DefaultCacheUtils cacheUtils
        )
        {
            cacheUtils.InvalidatePrimaryToken();

            tokenStore.Received().InvalidateToken();
        }

        [Test, Auto]
        public void InvalidateAllUserTokens_ShouldCallUserTokenStore_InvalidateAllUserTokens(
            [Frozen, Substitute] IUserTokenStore tokenStore,
            [Target] DefaultCacheUtils cacheUtils
        )
        {
            cacheUtils.InvalidateAllUserTokens();

            tokenStore.Received().InvalidateAllUserTokens();
        }

        [Test, Auto]
        public void InvalidateTokensForUser_ShouldCallUserTokenStore_InvalidateTokensForUser(
            string userId,
            [Frozen, Substitute] IUserTokenStore tokenStore,
            [Target] DefaultCacheUtils cacheUtils
        )
        {
            cacheUtils.InvalidateTokensForUser(userId);

            tokenStore.Received().InvalidateTokensForUser(Is(userId));
        }
    }
}
