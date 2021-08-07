using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.NUnit3;

using Brighid.Identity.Client.Utils;

using Microsoft.IdentityModel.Tokens;

using NSubstitute;

using NUnit.Framework;

using static NSubstitute.Arg;

namespace Brighid.Identity.Client
{
    public class TokenResponseValidatorTests
    {
        [TestFixture]
        public class ValidateTokenResponse
        {
            public class TokenResponseData : AutoDataAttribute
            {
                public TokenResponseData() : base(Create) { }

                public static IFixture Create()
                {
                    var fixture = AutoAttribute.Create();
                    fixture.Inject(new TokenResponse
                    {
                        AccessToken = "eyJhbGciOiJFUzI1NiIsImtpZCI6IjFBMERGNUNBNjYxM0Y2RDJGMEZBODZDMEEyNzMyNjQwODc4OEVEMjgiLCJ0eXAiOiJhdCtqd3QifQ.eyJuYW1lIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwic3ViIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwicm9sZSI6WyJCYXNpYyJdLCJvaV9wcnN0IjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0IiwiY2xpZW50X2lkIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwib2lfdGtuX2lkIjoiYTk0ZDNlNzAtNDJiZS00YjEzLWIxN2YtZWJjY2E2NDM3OGNjIiwiYXVkIjoiaWRlbnRpdHkuYnJpZ2guaWQiLCJzY29wZSI6Im9wZW5pZCIsImV4cCI6MTYyODEzODMyNiwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS5icmlnaC5pZC8iLCJpYXQiOjE2MjgxMzQ3MjZ9.QOcdciV9nviOBQJ3Q5n2c-MMVFX51JlBUCZeWUe3Pq2SpyaNzerS3A2iTwUTqPwmxjjVajO5BbNCVh84thk12w",
                        IdToken = "eyJhbGciOiJFUzI1NiIsImtpZCI6IjFBMERGNUNBNjYxM0Y2RDJGMEZBODZDMEEyNzMyNjQwODc4OEVEMjgiLCJ0eXAiOiJKV1QifQ.eyJuYW1lIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwic3ViIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwicm9sZSI6WyJCYXNpYyJdLCJhenAiOiJjZGZkNzE3NS01NzQ4LTRhYjItOWRiYS0yZTljYWE3MWEwODQiLCJhdF9oYXNoIjoiWWpWdFlRSlNQNmxZYWZnR0JFTkhNZyIsIm9pX3Rrbl9pZCI6ImE5OTNiZWIyLWVhN2MtNDc0NC04ZGJhLWNkMTQ4OGRjZDM5ZiIsImF1ZCI6ImNkZmQ3MTc1LTU3NDgtNGFiMi05ZGJhLTJlOWNhYTcxYTA4NCIsImV4cCI6MTYyODEzNTkyNiwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS5icmlnaC5pZC8iLCJpYXQiOjE2MjgxMzQ3MjZ9.qixPG7xcD90y2YhNRLZp2K_zzCw0kGGZ2rF6en_MqM9XxMskk6tKko-Mhg8MQOolwW3FVeAH9yFXU8EWMbe0Mw"
                    });

                    return fixture;
                }
            }

            [Test, TokenResponseData]
            public async Task ShouldValidateIdTokenSigningKey(
                TokenResponse response,
                [Frozen, Substitute] SecurityKey securityKey,
                [Frozen, Substitute] ISigningKeyResolver signingKeyResolver,
                [Frozen, Substitute] ISecurityTokenValidator securityTokenValidator,
                [Target] TokenResponseValidator validator,
                CancellationToken cancellationToken
            )
            {
                await validator.ValidateTokenResponse(response, cancellationToken);

                await signingKeyResolver.Received().ResolveSigningKey(Is(response.IdToken), Is(cancellationToken));
                securityTokenValidator.Received().ValidateToken(
                    Is(response.IdToken),
                    Is<TokenValidationParameters>(@params =>
                        @params.ValidateIssuerSigningKey &&
                        @params.IssuerSigningKey == securityKey
                    ),
                    out Any<SecurityToken>()
                );
            }

            [Test, TokenResponseData]
            public async Task ShouldValidateIdTokenIssuer(
                Uri issuer,
                TokenResponse response,
                [Frozen, Substitute] IMetadataProvider metadataProvider,
                [Frozen, Substitute] ISecurityTokenValidator securityTokenValidator,
                [Target] TokenResponseValidator validator,
                CancellationToken cancellationToken
            )
            {
                metadataProvider.IdentityServerUri.Returns(issuer);

                await validator.ValidateTokenResponse(response, cancellationToken);

                securityTokenValidator.Received().ValidateToken(
                    Is(response.IdToken),
                    Is<TokenValidationParameters>(@params =>
                        @params.ValidateIssuer &&
                        @params.ValidIssuer == issuer.ToString()
                    ),
                    out Any<SecurityToken>()
                );
            }

            [Test, TokenResponseData]
            public async Task ShouldValidateIdTokenLifetime(
                TokenResponse response,
                [Frozen, Substitute] ISecurityTokenValidator securityTokenValidator,
                [Target] TokenResponseValidator validator,
                CancellationToken cancellationToken
            )
            {
                await validator.ValidateTokenResponse(response, cancellationToken);

                securityTokenValidator.Received().ValidateToken(
                    Is(response.IdToken),
                    Is<TokenValidationParameters>(@params =>
                        @params.ValidateLifetime
                    ),
                    out Any<SecurityToken>()
                );
            }

            [Test, TokenResponseData]
            public async Task ShouldRequireSignedIdTokens(
                TokenResponse response,
                [Frozen, Substitute] ISecurityTokenValidator securityTokenValidator,
                [Target] TokenResponseValidator validator,
                CancellationToken cancellationToken
            )
            {
                await validator.ValidateTokenResponse(response, cancellationToken);

                securityTokenValidator.Received().ValidateToken(
                    Is(response.IdToken),
                    Is<TokenValidationParameters>(@params =>
                        @params.RequireSignedTokens
                    ),
                    out Any<SecurityToken>()
                );
            }

            [Test, TokenResponseData]
            public async Task ShouldAllow5MinuteClowSkew(
                TokenResponse response,
                [Frozen, Substitute] ISecurityTokenValidator securityTokenValidator,
                [Target] TokenResponseValidator validator,
                CancellationToken cancellationToken
            )
            {
                await validator.ValidateTokenResponse(response, cancellationToken);

                securityTokenValidator.Received().ValidateToken(
                    Is(response.IdToken),
                    Is<TokenValidationParameters>(@params =>
                        @params.ClockSkew == TimeSpan.FromMinutes(5)
                    ),
                    out Any<SecurityToken>()
                );
            }

            [Test, TokenResponseData]
            public async Task ShouldAllowOnlyEs256Algorithm(
                TokenResponse response,
                [Frozen, Substitute] ISecurityTokenValidator securityTokenValidator,
                [Target] TokenResponseValidator validator,
                CancellationToken cancellationToken
            )
            {
                await validator.ValidateTokenResponse(response, cancellationToken);

                securityTokenValidator.Received().ValidateToken(
                    Is(response.IdToken),
                    Is<TokenValidationParameters>(@params =>
                        @params.ValidAlgorithms.SequenceEqual(new[] { "ES256" })
                    ),
                    out Any<SecurityToken>()
                );
            }

            [Test, TokenResponseData]
            public async Task ShouldValidateAccessTokenHash(
                string hash,
                TokenResponse response,
                [Frozen, Substitute] ITokenCryptoService tokenCryptoService,
                [Target] TokenResponseValidator validator,
                CancellationToken cancellationToken
            )
            {
                tokenCryptoService.GetAccessTokenHash(Any<ClaimsPrincipal>()).Returns(hash);

                await validator.ValidateTokenResponse(response, cancellationToken);

                tokenCryptoService.Received().ValidateAccessTokenHash(Is("SHA256"), Is(hash), Is(response.AccessToken));
            }
        }
    }
}
