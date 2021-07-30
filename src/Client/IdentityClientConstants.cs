namespace Brighid.Identity.Client
{
    public static class IdentityClientConstants
    {
        public const string DefaultIdentityServerUri = "http://identity.brigh.id";
        public const string ProductName = "BrighidIdentityClient";

        public static class GrantTypes
        {
            public const string ClientCredentials = "client_credentials";
            public const string Impersonate = "impersonate";
        }
    }
}
