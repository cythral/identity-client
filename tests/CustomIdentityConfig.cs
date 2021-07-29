namespace Brighid.Identity.Client
{
    public class CustomIdentityConfig : IdentityConfig
    {
        public override string ClientId { get; set; } = string.Empty;

        public override string ClientSecret { get; set; } = string.Empty;
    }
}
