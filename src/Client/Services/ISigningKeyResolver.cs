using System.Threading;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

namespace Brighid.Identity.Client
{
    public interface ISigningKeyResolver
    {
        /// <summary>
        /// Resolves the signing key used for a JWT.
        /// </summary>
        /// <param name="token">The token to resolve the signing key for.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The resulting security key.</returns>
        Task<SecurityKey> ResolveSigningKey(string token, CancellationToken cancellationToken = default);
    }
}
