using System;
using System.Text.Json.Serialization;

namespace Brighid.Identity.Client
{
    /// <summary>
    /// JWKS Document Response from the Identity Service, contains the Json Web Keys / Public Signing Keys.
    /// </summary>
    public struct JwksResponse
    {
        public JwksResponse(
            JsonWebKey[] keys
        )
        {
            Keys = keys ?? Array.Empty<JsonWebKey>();
        }

        /// <summary>
        /// Gets or sets the signing keys.
        /// </summary>
        [JsonPropertyName("keys")]
        public JsonWebKey[] Keys { get; init; }
    }

}
