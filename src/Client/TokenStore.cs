using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Brighid.Identity.Client
{
    /// <inheritdoc />
    public class TokenStore : ITokenStore, IDisposable
    {
        private readonly IdentityServerClient client;
        private readonly IdentityConfig options;
        private CancellationTokenSource? cancellationTokenSource = new();
        private readonly CancellationToken cancellationToken;
        private readonly Channel<byte> requestChannel = Channel.CreateUnbounded<byte>();
        private readonly Channel<string?> responseChannel = Channel.CreateUnbounded<string?>();
        private Token? Token { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenStore" /> class.
        /// </summary>
        /// <param name="client">Client used to exchange client credentials for tokens with.</param>
        /// <param name="options">Options containing client credentials.</param>
        public TokenStore(
            IdentityServerClient client,
            IOptions<IdentityConfig> options
        )
        {
            this.client = client;
            this.options = options.Value;
            cancellationToken = cancellationTokenSource.Token;
            Run();
        }

        /// <inheritdoc />
        public async Task<string> GetIdToken(CancellationToken cancellationToken = default)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();

            await requestChannel.Writer.WriteAsync(0, cancellationToken);
            var result = await responseChannel.Reader.ReadAsync(cancellationToken);

            return result ?? throw new OperationCanceledException("Credentials could not be exchanged for a token.");
        }

        /// <summary>
        /// No more than one task should exchange a token at a time, so GetIdToken puts a request in a queue, which
        /// Run picks up and responds to.
        /// </summary>
        public async void Run()
        {
            var reader = requestChannel.Reader;
            var writer = responseChannel.Writer;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!await reader.WaitToReadAsync(cancellationToken))
                {
                    continue;
                }

                await reader.ReadAsync(cancellationToken);

                try
                {
                    if (Token == null || Token.HasExpired)
                    {
                        Token = await client.ExchangeClientCredentialsForToken(options.ClientId, options.ClientSecret, cancellationToken);
                    }

                    await writer.WriteAsync(Token.IdToken, cancellationToken);
                }
                catch (Exception)
                {
                    await writer.WriteAsync(null);
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
