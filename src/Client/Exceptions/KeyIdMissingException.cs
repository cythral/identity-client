using System;

namespace Brighid.Identity.Client
{
    public class KeyIdMissingException : Exception
    {
        public KeyIdMissingException()
            : base("The kid claim was missing in the given token.")
        {
        }
    }
}
