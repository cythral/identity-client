using System.Text.Json.Serialization;

namespace Brighid.Identity.Client
{
    /// <summary>
    /// Represents a json web key used to sign a JWT.
    /// </summary>
    public struct JsonWebKey
    {
        /// <summary>
        /// Gets or sets the ID of the Json Web Key.
        /// </summary>
        [JsonPropertyName("kid")]
        public string KeyId { get; init; }

        /// <summary>
        /// Gets or sets the x coordinate of the Json Web Key.
        /// </summary>
        [JsonPropertyName("x")]
        public string X { get; init; }

        /// <summary>
        /// Gets or sets the y coordinate of the Json Web Key.
        /// </summary>
        [JsonPropertyName("y")]
        public string Y { get; init; }
    }
}
