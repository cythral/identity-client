using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Brighid.Identity.Client.Stores
{
    /// <inheritdoc />
    internal class TokenStore<TConfig> : ITokenStore, IDisposable
        where TConfig : IdentityConfig
    {
        private readonly IdentityServerClient client;
        private readonly ITokenResponseValidator validator;
        private readonly IdentityConfig options;
        private CancellationTokenSource? cancellationTokenSource = new();
        private readonly CancellationToken cancellationToken;
        private readonly Channel<TokenRequest> requestChannel = Channel.CreateUnbounded<TokenRequest>();
        private TokenResponse? TokenResponse { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenStore{TConfig}" /> class.
        /// </summary>
        /// <param name="client">Client used to exchange client credentials for tokens with.</param>
        /// <param name="validator">Service used to validate token responses received from the Identity Service.</param>
        /// <param name="options">Options containing client credentials.</param>
        public TokenStore(
            IdentityServerClient client,
            ITokenResponseValidator validator,
            IOptions<TConfig> options
        )
        {
            this.client = client;
            this.validator = validator;
            this.options = options.Value;
            cancellationToken = cancellationTokenSource.Token;
            Run();
        }

        /// <inheritdoc />
        public async Task<string> GetToken(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new TokenRequest
            {
                Promise = new TaskCompletionSource<string>(),
                CancellationToken = cancellationToken,
            };

            await requestChannel.Writer.WriteAsync(request, cancellationToken);
            return await request.Promise.Task;
        }

        /// <inheritdoc />
        public void InvalidateToken()
        {
            TokenResponse = null;
        }

        /// <summary>
        /// No more than one task should exchange a token at a time, so GetToken puts a request in a queue, which
        /// Run picks up and responds to.
        /// </summary>
        public async void Run()
        {
            var reader = requestChannel.Reader;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!await reader.WaitToReadAsync(cancellationToken))
                {
                    continue;
                }

                var request = await reader.ReadAsync(cancellationToken);
                var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, request.CancellationToken).Token;

                try
                {
                    if (TokenResponse == null || TokenResponse.HasExpired)
                    {
                        var response = await client.ExchangeClientCredentialsForToken(options.ClientId, options.ClientSecret, options.Audience, linkedCancellationToken);
                        await validator.ValidateTokenResponse(response);
                        TokenResponse = response;
                    }

                    request.Promise.SetResult(TokenResponse.AccessToken);
                }
                catch (Exception e)
                {
                    request.Promise.SetException(e);
                }
            }
        }

        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            GC.SuppressFinalize(this);
        }
    }
}
