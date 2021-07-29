using System;

namespace Brighid.Identity.Client.Utils
{
    /// <summary>
    /// Utility that provides runtime metadata to internal services.  Unlike the Identity Config which can contain sensitive data, 
    /// this contains safe data and is injected into the service collection.
    /// </summary>
    internal interface IMetadataProvider
    {
        /// <summary>
        /// Gets or sets the Identity Server URI.
        /// </summary>
        Uri IdentityServerUri { get; }
    }
}
