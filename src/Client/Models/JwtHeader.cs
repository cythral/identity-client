using System.Text.Json.Serialization;

namespace Brighid.Identity.Client
{
    /// <summary>
    /// The decoded JWT Header.  We only care about the KID here so that we can fetch the correct public signing key.
    /// </summary>
    public struct JwtHeader
    {
        /// <summary>
        /// Gets or sets the ID of the public signing key.
        /// </summary>
        [JsonPropertyName("kid")]
        public string? Kid { get; set; }
    }
}
