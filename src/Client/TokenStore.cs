using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Brighid.Identity.Client
{
    /// <inheritdoc />
    public class TokenStore<TConfig> : ITokenStore, IDisposable
        where TConfig : IdentityConfig
    {
        private readonly IdentityServerClient client;
        private readonly IdentityConfig options;
        private CancellationTokenSource? cancellationTokenSource = new();
        private readonly CancellationToken cancellationToken;
        private readonly Channel<TokenRequest> requestChannel = Channel.CreateUnbounded<TokenRequest>();
        private readonly Channel<string?> responseChannel = Channel.CreateUnbounded<string?>();
        private Token? Token { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenStore{TConfig}" /> class.
        /// </summary>
        /// <param name="client">Client used to exchange client credentials for tokens with.</param>
        /// <param name="options">Options containing client credentials.</param>
        public TokenStore(
            IdentityServerClient client,
            IOptions<TConfig> options
        )
        {
            this.client = client;
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
            Token = null;
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
                    if (Token == null || Token.HasExpired)
                    {
                        Token = await client.ExchangeClientCredentialsForToken(options.ClientId, options.ClientSecret, options.Audience, linkedCancellationToken);
                    }

                    // TODO: Verify ID Token
                    request.Promise.SetResult(Token.AccessToken);
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
