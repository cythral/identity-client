using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Brighid.Identity.Client
{
    public class ClientCredentialsHandler : DelegatingHandler
    {
        private readonly ITokenStore tokenStore;

        public ClientCredentialsHandler(ITokenStore tokenStore)
        {
            this.tokenStore = tokenStore;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default)
        {
            var token = await tokenStore.GetIdToken(cancellationToken);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        }
    }
}
