using System;
using System.Text.Json.Serialization;

namespace Brighid.Identity.Client
{
    /// <summary>
    /// Represents a token response from the Identity Service.
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// Gets or sets the ID Token.
        /// </summary>
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Access Token.
        /// </summary>
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time in seconds that the attached tokens will expire.
        /// </summary>
        [JsonPropertyName("expires_in")]
        public uint ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the date and time that this token response was created.
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset CreationDate { get; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets a value indicating whether the attached tokens have expired.
        /// </summary>
        [JsonIgnore]
        public bool HasExpired => DateTimeOffset.Now >= (CreationDate + TimeSpan.FromSeconds(ExpiresIn));
    }
}
