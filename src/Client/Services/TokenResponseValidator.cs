using System;
using System.Threading;
using System.Threading.Tasks;

using Brighid.Identity.Client.Utils;

using Microsoft.IdentityModel.Tokens;

namespace Brighid.Identity.Client
{
    /// <inheritdoc />
    public class TokenResponseValidator : ITokenResponseValidator
    {
        private readonly IMetadataProvider metadataProvider;
        private readonly ISigningKeyResolver signingKeyResolver;
        private readonly ITokenCryptoService tokenCryptoService;
        private readonly ISecurityTokenValidator securityTokenValidator;
        private readonly string[] validAlgorithms = new[] { "ES256" };

        public TokenResponseValidator(
            IMetadataProvider metadataProvider,
            ISigningKeyResolver signingKeyResolver,
            ITokenCryptoService tokenCryptoService,
            ISecurityTokenValidator securityTokenValidator
        )
        {
            this.metadataProvider = metadataProvider;
            this.signingKeyResolver = signingKeyResolver;
            this.tokenCryptoService = tokenCryptoService;
            this.securityTokenValidator = securityTokenValidator;
        }

        /// <inheritdoc />
        public async Task ValidateTokenResponse(TokenResponse response, CancellationToken cancellationToken = default)
        {
            var signingKey = await signingKeyResolver.ResolveSigningKey(response.IdToken, cancellationToken);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                ValidateIssuer = true,
                ValidIssuer = metadataProvider.IdentityServerUri.ToString(),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidAlgorithms = validAlgorithms,
                RequireSignedTokens = true,
            };

            var principal = securityTokenValidator.ValidateToken(response.IdToken, tokenValidationParameters, out _);
            var accessTokenHash = tokenCryptoService.GetAccessTokenHash(principal);
            tokenCryptoService.ValidateAccessTokenHash("SHA256", accessTokenHash, response.AccessToken);
        }
    }
}
