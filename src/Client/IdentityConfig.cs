using System;

namespace Brighid.Identity.Client
{
    public class IdentityConfig
    {
        public virtual Uri IdentityServerUri { get; set; } = new Uri("https://identity.brigh.id/");
        public virtual string? Audience { get; set; } = null;
        public virtual string ClientId { get; set; } = string.Empty;
        public virtual string ClientSecret { get; set; } = string.Empty;
        public virtual int MaxRefreshAttempts { get; set; } = 3;
    }
}
