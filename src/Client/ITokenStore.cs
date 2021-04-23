using System.Threading;
using System.Threading.Tasks;

namespace Brighid.Identity.Client
{
    /// <summary>
    /// Store for storing/retrieving bearer tokens.
    /// </summary>
    public interface ITokenStore
    {
        /// <summary>
        /// Retrieves the ID token from the store or the API if it's expired/not present.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The resulting token.</returns>
        Task<string> GetIdToken(CancellationToken cancellationToken = default);
    }
}
