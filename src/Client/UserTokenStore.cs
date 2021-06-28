using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Brighid.Identity.Client
{
    /// <inheritdoc />
    public class UserTokenStore : IUserTokenStore, IDisposable
    {
        private readonly ITokenStore tokenStore;
        private readonly IdentityServerClient identityServerClient;
        private CancellationTokenSource? cancellationTokenSource = new();
        private readonly CancellationToken cancellationToken;
        private readonly Channel<TokenRequest> requestChannel = Channel.CreateUnbounded<TokenRequest>();
        private readonly ConcurrentDictionary<string, Token> userTokenCache = new();

        public UserTokenStore(
            ITokenStore tokenStore,
            IdentityServerClient identityServerClient
        )
        {
            this.tokenStore = tokenStore;
            this.identityServerClient = identityServerClient;
            cancellationToken = cancellationTokenSource.Token;
            Run();
        }

        /// <inheritdoc />
        public async Task<string> GetUserToken(string userId, string audience, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new TokenRequest
            {
                UserId = userId,
                Audience = audience,
                Promise = new TaskCompletionSource<string>(),
                CancellationToken = cancellationToken
            };

            await requestChannel.Writer.WriteAsync(request, cancellationToken);
            return await request.Promise.Task;
        }

        /// <inheritdoc />
        public void InvalidateAllUserTokens()
        {
            userTokenCache.Clear();
        }

        /// <inheritdoc />
        public void InvalidateTokensForUser(string userId)
        {
            var keyToSearchFor = $"{userId}:";
            foreach (var key in userTokenCache.Keys)
            {
                if (key.StartsWith(keyToSearchFor))
                {
                    userTokenCache.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// No more than one task should exchange a token at a time, so GetUserToken puts a request in a queue, which
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
                    var tokenKey = $"{request.UserId}:{request.Audience}";
                    if (!userTokenCache.TryGetValue(tokenKey, out var result) || result.HasExpired)
                    {
                        var accessToken = await tokenStore.GetToken(linkedCancellationToken);
                        result = await identityServerClient.ExchangeAccessTokenForImpersonateToken(
                            accessToken,
                            request.UserId!,
                            request.Audience!,
                            linkedCancellationToken
                        );

                        userTokenCache[tokenKey] = result;
                    }

                    // TODO: Verify ID Token
                    request.Promise.SetResult(result.AccessToken);
                }
                catch (Exception e)
                {
                    request.Promise.SetException(e);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            GC.SuppressFinalize(this);
        }
    }
}
