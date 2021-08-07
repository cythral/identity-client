using System;

namespace Brighid.Identity.Client
{
    public class SigningKeyNotFoundException : Exception
    {
        public SigningKeyNotFoundException(string keyId)
            : base($"The signing key for kid: {keyId} was not found.")
        {
            KeyId = keyId;
        }

        public string KeyId { get; }
    }
}
