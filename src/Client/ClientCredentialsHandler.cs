using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Brighid.Identity.Client
{
    public class ClientCredentialsHandler : DelegatingHandler
    {
        private const int MaxTries = 3;
        private readonly ITokenStore tokenStore;

        public ClientCredentialsHandler(ITokenStore tokenStore)
        {
            this.tokenStore = tokenStore;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default)
        {
            return await SendAsyncSemaphore(requestMessage, cancellationToken: cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsyncSemaphore(HttpRequestMessage requestMessage, int @try = 1, CancellationToken cancellationToken = default)
        {
            if (@try > MaxTries)
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
