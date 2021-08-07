using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

namespace Brighid.Identity.Client
{
    public interface ITokenCryptoService
    {
        /// <summary>
        /// Fetches the ID of the key used to sign the <paramref name="token" />.
        /// </summary>
        /// <param name="token">The token to get a key ID for.</param>
        /// <exception cref="KeyIdMissingException">Thrown if the token does not contain a kid claim in the JWT header.</exception>
        /// <returns>The key ID of the token.</returns>
        string GetKeyIdForToken(string token);

        /// <summary>
        /// Fetches signing keys from the Identity Service.
        /// </summary>
        /// <param name="keyId">The ID of the key to fetch from the Identity Service.</param>
        /// <param name="cancellationToken">Token used to cancel the operation with.</param>
        /// <returns>The signing keys from the Identity Service.</returns>
        Task<SecurityKey> FetchSigningKey(string keyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the access token hash from the given <paramref name="principal" />.
        /// </summary>
        /// <param name="principal">The principal to get the access token hash for.</param>
        /// <returns>The hash of the access token.</returns>
        string GetAccessTokenHash(ClaimsPrincipal principal);

        /// <summary>
        /// Validates that the hash of the first half of the access token matches the given <paramref name="hash" />.
        /// </summary>
        /// <param name="algorithm">The algorithm to validate hashes against.</param>
        /// <param name="hash">The expected hash of the first half of the access token.</param>
        /// <param name="accessToken">The access token to validate a hash for.</param>
        void ValidateAccessTokenHash(string algorithm, string hash, string accessToken);
    }
}
