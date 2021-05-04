using System;

namespace Brighid.Identity.Client
{
    public class TokenRefreshException : Exception
    {
        public TokenRefreshException() : base("Failed to refresh token.")
        {
        }
    }
}
