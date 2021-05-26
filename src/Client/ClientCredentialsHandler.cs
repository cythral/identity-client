using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Brighid.Identity.Client
{
    public class ClientCredentialsHandler : DelegatingHandler
    {
        private readonly ITokenStore tokenStore;
        private readonly IdentityConfig config;

        public ClientCredentialsHandler(
            ITokenStore tokenStore,
            IOptions<IdentityConfig> config
        )
        {
            this.tokenStore = tokenStore;
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

            var token = await tokenStore.GetIdToken(cancellationToken);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await base.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                tokenStore.InvalidateToken();
                return await SendAsyncSemaphore(requestMessage, @try + 1, cancellationToken);
            }

            return response;
        }
    }
}
