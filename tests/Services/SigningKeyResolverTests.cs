using System.Threading;
using System.Threading.Tasks;

using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

using NSubstitute;

using NUnit.Framework;

using static NSubstitute.Arg;

namespace Brighid.Identity.Client
{
    public class SigningKeyResolverTests
    {
        [TestFixture]
        public class ResolveSigningKey
        {
            [Test, Auto]
            public async Task ShouldFetchSigningKeyForToken(
                string keyId,
                string token,
                [Frozen, Substitute] SecurityKey securityKey,
                [Frozen, Substitute] ITokenCryptoService service,
                [Target] SigningKeyResolver resolver,
                CancellationToken cancellationToken
            )
            {
                service.GetKeyIdForToken(Any<string>()).Returns(keyId);
                service.FetchSigningKey(Any<string>(), Any<CancellationToken>()).Returns(securityKey);

                var result = await resolver.ResolveSigningKey(token, cancellationToken);

                result.Should().Be(securityKey);
                service.Received().GetKeyIdForToken(Is(token));
                await service.Received().FetchSigningKey(Is(keyId), Is(cancellationToken));
            }

            [Test, Auto]
            public async Task ShouldCacheSecurityKeys(
                string keyId,
                string token,
                [Frozen, Substitute] SecurityKey securityKey,
                [Frozen, Substitute] ITokenCryptoService service,
                [Target] SigningKeyResolver resolver,
                CancellationToken cancellationToken
            )
            {
                service.GetKeyIdForToken(Any<string>()).Returns(keyId);
                service.FetchSigningKey(Any<string>(), Any<CancellationToken>()).Returns(securityKey);

                await resolver.ResolveSigningKey(token, cancellationToken);
                await Task.Delay(1000);
                await resolver.ResolveSigningKey(token, cancellationToken);
                var result = await resolver.ResolveSigningKey(token, cancellationToken);

                result.Should().Be(securityKey);
                service.Received().GetKeyIdForToken(Is(token));
                await service.Received(1).FetchSigningKey(Is(keyId), Is(cancellationToken));
            }
        }
    }
}
