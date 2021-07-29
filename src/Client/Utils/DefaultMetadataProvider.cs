using System;

namespace Brighid.Identity.Client.Utils
{
    /// <inheritdoc />
    internal class DefaultMetadataProvider : IMetadataProvider
    {
        public DefaultMetadataProvider(
            IdentityConfig config
        )
        {
            IdentityServerUri = config.IdentityServerUri;
        }

        public Uri IdentityServerUri { get; }
    }
}
