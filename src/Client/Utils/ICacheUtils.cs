namespace Brighid.Identity.Client.Utils
{
    public interface ICacheUtils
    {
        /// <summary>
        /// Invalidates / clears out the primary token from the Token Store, causing a new one to be fetched on the next request.
        /// </summary>
        void InvalidatePrimaryToken();

        /// <summary>
        /// Invalidates all user tokens stored in the cache.
        /// </summary>
        void InvalidateAllUserTokens();

        /// <summary>
        /// Invalidate all tokens for the given <paramref name="userId" />.
        /// </summary>
        /// <param name="userId">The userId to invalidate a user token for.</param>
        void InvalidateTokensForUser(string userId);
    }
}
