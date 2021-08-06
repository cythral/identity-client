using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Brighid.Identity.Client.Stores;

using Microsoft.Extensions.Options;

namespace Brighid.Identity.Client
{
    internal class ClientCredentialsHandler<TConfig> : DelegatingHandler
        where TConfig : IdentityConfig
    {
        private readonly ITokenStore tokenStore;
        private readonly IUserTokenStore userTokenStore;
        private readonly TConfig config;

        public ClientCredentialsHandler(
            ITokenStore tokenStore,
            IUserTokenStore userTokenStore,
            IOptions<TConfig> config
        )
        {
            this.tokenStore = tokenStore;
            this.userTokenStore = userTokenStore;
            this.config = config.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default)
        {
            return requestMessage.Headers.Contains("authorization")
                ? await base.SendAsync(requestMessage, cancellationToken)
                : await SendAsyncSemaphore(requestMessage, cancellationToken: cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsyncSemaphore(HttpRequestMessage requestMessage, int @try = 1, CancellationToken cancellationToken = default)
        {
            if (@try > config.MaxRefreshAttempts)
            {
                throw new TokenRefreshException();
            }

            var token = await GetTokenForRequest(requestMessage, cancellationToken);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await base.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                tokenStore.InvalidateToken();
                return await SendAsyncSemaphore(requestMessage, @try + 1, cancellationToken);
            }

            return response;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<string> GetTokenForRequest(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (requestMessage.Headers.TryGetValues("x-impersonate-userId", out var userIdHeaderValues) &&
                requestMessage.Headers.TryGetValues("x-impersonate-audience", out var audienceHeaderValues))
            {
                var userId = userIdHeaderValues.First();
                var audience = audienceHeaderValues.First();
                return await userTokenStore.GetUserToken(userId, audience, cancellationToken);
            }

            return await tokenStore.GetToken(cancellationToken);
        }
    }
}
