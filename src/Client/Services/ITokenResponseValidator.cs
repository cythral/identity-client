using System.Threading;
using System.Threading.Tasks;

namespace Brighid.Identity.Client
{
    /// <summary>
    /// Service that validates token responses from the Identity Service.
    /// </summary>
    public interface ITokenResponseValidator
    {
        /// <summary>
        /// Validates the given token response.
        /// </summary>
        /// <param name="response">The response to validate.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task ValidateTokenResponse(TokenResponse response, CancellationToken cancellationToken = default);
    }
}
