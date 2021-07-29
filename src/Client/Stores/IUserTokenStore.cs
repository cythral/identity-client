using System.Threading;
using System.Threading.Tasks;

namespace Brighid.Identity.Client.Stores
{
    /// <summary>
    /// Store to retrieve and store user tokens in.
    /// </summary>
    internal interface IUserTokenStore
    {
        /// <summary>
        /// Gets a user impersonation token for the given <paramref name="userId" /> and <paramref name="audience" />.
        /// </summary>
        /// <param name="userId">ID of the user to impersonate.</param>
        /// <param name="audience">The audience to use in the token.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The resulting impersonation token.</returns>
        Task<string> GetUserToken(string userId, string audience, CancellationToken cancellationToken = default);

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
