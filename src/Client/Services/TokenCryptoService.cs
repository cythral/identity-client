using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

#pragma warning disable CA1822

namespace Brighid.Identity.Client
{
    /// <inheritdoc />
    public class TokenCryptoService : ITokenCryptoService
    {
        private readonly HttpClient httpClient;

        [ThreadStatic]
        private Dictionary<string, string>? keyIdCache;

        public TokenCryptoService(
            HttpClient httpClient
        )
        {
            this.httpClient = httpClient;
        }

        public Dictionary<string, string> KeyIdCache => keyIdCache ??= new(7);

        /// <inheritdoc />
        public string GetKeyIdForToken(string token)
        {
            var endIndex = token.IndexOf('.');
            if (endIndex == -1)
            {
                throw new ArgumentException("Token is not a valid JWT.");
            }

            var headerRange = token[0..endIndex];

            if (KeyIdCache.TryGetValue(headerRange, out var cachedKeyId))
            {
                return cachedKeyId;
            }

            var headerBytes = Base64UrlEncoder.DecodeBytes(headerRange);
            var header = JsonSerializer.Deserialize<JwtHeader>(headerBytes);
            var keyId = header.Kid ?? throw new KeyIdMissingException();
            KeyIdCache[headerRange] = keyId;
            KeyIdCache.TrimExcess();
            return keyId;
        }

        /// <inheritdoc />
        public async Task<SecurityKey> FetchSigningKey(string keyId, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetFromJsonAsync<JwksResponse>("/.well-known/jwks", cancellationToken);

            foreach (var key in response.Keys)
            {
                if (key.KeyId == keyId)
                {
                    var ecdsa = ECDsa.Create(new ECParameters
                    {
                        Curve = ECCurve.NamedCurves.nistP256,
                        Q = new ECPoint
                        {
                            X = Base64UrlEncoder.DecodeBytes(key.X),
                            Y = Base64UrlEncoder.DecodeBytes(key.Y),
                        },
                    });

                    return new ECDsaSecurityKey(ecdsa)
                    {
                        KeyId = key.KeyId,
                    };
                }
            }

            throw new SigningKeyNotFoundException(keyId);
        }

        /// <inheritdoc />
        public string GetAccessTokenHash(ClaimsPrincipal principal)
        {
            var identity = principal.Identity as ClaimsIdentity;
            var claim = identity?.FindFirst(claim => claim.Type == IdentityClientConstants.ClaimTypes.AccessTokenHash);
            return claim?.Value ?? string.Empty;
        }

        /// <inheritdoc />
        public void ValidateAccessTokenHash(string algorithm, string hash, string accessToken)
        {
            using var hashContext = HashAlgorithm.Create(algorithm);
            var accessTokenBytes = Encoding.ASCII.GetBytes(accessToken);
            var actualHashBytes = hashContext?.ComputeHash(accessTokenBytes) ?? Array.Empty<byte>();
            var halfwayIndex = actualHashBytes.Length / 2;
            var actualHash = Base64UrlEncoder.Encode(actualHashBytes[0..halfwayIndex]);

            if (actualHash != hash)
            {
                throw new InvalidAccessTokenHashException();
            }
        }
    }
}
