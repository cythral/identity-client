using Brighid.Identity.Client.Stores;

namespace Brighid.Identity.Client.Utils
{
    internal class DefaultCacheUtils : ICacheUtils
    {
        private readonly ITokenStore tokenStore;
        private readonly IUserTokenStore userTokenStore;

        public DefaultCacheUtils(
            ITokenStore tokenStore,
            IUserTokenStore userTokenStore
        )
        {
            this.tokenStore = tokenStore;
            this.userTokenStore = userTokenStore;
        }

        /// <inheritdoc />
        public int UserTokenCount => userTokenStore.TokenCount;

        /// <inheritdoc />
        public void InvalidatePrimaryToken()
        {
            tokenStore.InvalidateToken();
        }

        /// <inheritdoc />
        public void InvalidateAllUserTokens()
        {
            userTokenStore.InvalidateAllUserTokens();
        }

        /// <inheritdoc />
        public void InvalidateTokensForUser(string userId)
        {
            userTokenStore.InvalidateTokensForUser(userId);
        }
    }
}
