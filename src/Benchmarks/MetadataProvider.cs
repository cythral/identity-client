using System;

using Brighid.Identity.Client.Utils;

namespace Brighid.Identity.Client
{
    public class MetadataProvider : IMetadataProvider
    {
        public Uri IdentityServerUri => new("https://identity.brigh.id/");
    }
}
