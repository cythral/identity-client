using System.Threading;
using System.Threading.Tasks;

namespace Brighid.Identity.Client
{
    public struct TokenRequest
    {
        public string? UserId { get; init; }
        public string? Audience { get; init; }
        public TaskCompletionSource<string> Promise { get; init; }
        public CancellationToken CancellationToken { get; init; }
    }
}
