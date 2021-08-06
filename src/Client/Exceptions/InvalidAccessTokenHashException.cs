using System;

namespace Brighid.Identity.Client
{
    public class InvalidAccessTokenHashException : Exception
    {
        public InvalidAccessTokenHashException()
            : base("The access token hash in the ID token was invalid.")
        {
        }
    }
}
