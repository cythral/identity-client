using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

using static Brighid.Identity.Client.IdentityClientConstants;

namespace Brighid.Identity.Client
{
    /// <inheritdoc />
    public class SigningKeyResolver : ISigningKeyResolver
    {
        private readonly IMemoryCache memoryCache;
        private readonly ITokenCryptoService signingKeyService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SigningKeyResolver" /> class.
        /// </summary>
        /// <param name="memoryCache">Cache to keep resolved signing keys in.</param>
        /// <param name="signingKeyService">Service to fetch signing keys and key ids with.</param>
        public SigningKeyResolver(
            IMemoryCache memoryCache,
            ITokenCryptoService signingKeyService
        )
        {
            this.memoryCache = memoryCache;
            this.signingKeyService = signingKeyService;
        }

        /// <inheritdoc />
        public async Task<SecurityKey> ResolveSigningKey(string token, CancellationToken cancellationToken = default)
        {
            var keyId = signingKeyService.GetKeyIdForToken(token);
            var signingKey = await memoryCache.GetOrCreateAsync(
                CachePrefixes.SigningKeys + keyId,
                (_) => signingKeyService.FetchSigningKey(keyId, cancellationToken)
            ) ?? throw new Exception("Could not resolve signing key");

            return signingKey;
        }
    }
}
