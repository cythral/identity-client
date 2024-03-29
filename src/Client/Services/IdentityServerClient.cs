using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Brighid.Identity.Client
{
    public class IdentityServerClient
    {
        private static readonly Uri tokenUri = new("/oauth2/token", UriKind.Relative);

        private readonly HttpClient httpClient;

        public IdentityServerClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public Uri? BaseAddress
        {
            get => httpClient.BaseAddress;
            set => httpClient.BaseAddress = value;
        }

        public virtual async Task<TokenResponse> ExchangeClientCredentialsForToken(string clientId, string clientSecret, string? audience = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var formData = new Dictionary<string, string?>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = IdentityClientConstants.GrantTypes.ClientCredentials,
            };

            if (audience != null)
            {
                formData["audience"] = audience;
            }

#pragma warning disable IDE0004 // Cast is necessary
            return await PerformExchange((IEnumerable<KeyValuePair<string?, string?>>)formData, cancellationToken);
#pragma warning restore IDE0004
        }

        public virtual async Task<TokenResponse> ExchangeAccessTokenForImpersonateToken(string accessToken, string userId, string audience, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var formData = (IEnumerable<KeyValuePair<string?, string?>>)new Dictionary<string, string?>
            {
                ["access_token"] = accessToken,
                ["user_id"] = userId,
                ["audience"] = audience,
                ["grant_type"] = IdentityClientConstants.GrantTypes.Impersonate,
            };

            return await PerformExchange(formData, cancellationToken);
        }

        private async Task<TokenResponse> PerformExchange(IEnumerable<KeyValuePair<string?, string?>> formData, CancellationToken cancellationToken)
        {
            using var requestContent = new FormUrlEncodedContent(formData);
            var response = await httpClient.PostAsync(tokenUri, requestContent, cancellationToken);
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);

            return token switch
            {
                TokenResponse => token,
                _ => throw new Exception("Token unexpectedly deserialized to null value."),
            };
        }
    }
}
