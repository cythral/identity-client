using System.Threading;
using System.Threading.Tasks;

namespace Brighid.Identity.Client
{
    /// <summary>
    /// Represents a request to retrieve a token.
    /// </summary>
    public readonly struct TokenRequest
    {
        /// <summary>
        /// Gets or sets the ID of the user to impersonate (impersonate exchanges only).
        /// </summary>
        public string? UserId { get; init; }

        /// <summary>
        /// Gets or sets the audience of the token to include in the request.
        /// </summary>
        public string? Audience { get; init; }

        /// <summary>
        /// Gets or sets the promise / task completion source for this request.
        /// </summary>
        public TaskCompletionSource<string> Promise { get; init; }

        /// <summary>
        /// Gets or sets the cancellation token for this request.
        /// </summary>
        public CancellationToken CancellationToken { get; init; }
    }
}
