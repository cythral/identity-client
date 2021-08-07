using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using Microsoft.Extensions.Caching.Memory;

namespace Brighid.Identity.Client.Benchmarks
{
    public class TokenResponseValidatorBenchmarks
    {
        private const string IdToken = "eyJhbGciOiJFUzI1NiIsImtpZCI6IjFBMERGNUNBNjYxM0Y2RDJGMEZBODZDMEEyNzMyNjQwODc4OEVEMjgiLCJ0eXAiOiJKV1QifQ.eyJuYW1lIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwic3ViIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwicm9sZSI6WyJCYXNpYyJdLCJhenAiOiJjZGZkNzE3NS01NzQ4LTRhYjItOWRiYS0yZTljYWE3MWEwODQiLCJhdF9oYXNoIjoiOWhHVklzWWNwY0thRFV6N1l1bUZnZyIsIm9pX3Rrbl9pZCI6IjJiZTcyMTIyLWRhMzAtNGIzNy1iNjgyLTU4NTQ5MTJiOGE4ZiIsImF1ZCI6ImNkZmQ3MTc1LTU3NDgtNGFiMi05ZGJhLTJlOWNhYTcxYTA4NCIsImV4cCI6MTYyODIyNjI4MCwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS5icmlnaC5pZC8iLCJpYXQiOjE2MjgyMjUwODB9.dPniwnFeZHG8vIF1z8y6mF5WBdq5XyzgfigAWJATugsd9oQE4-NxPHJTw7E-UJm40VQrZrTdwvzC7mfZfyuvjw";
        private const string AccessToken = "eyJhbGciOiJFUzI1NiIsImtpZCI6IjFBMERGNUNBNjYxM0Y2RDJGMEZBODZDMEEyNzMyNjQwODc4OEVEMjgiLCJ0eXAiOiJhdCtqd3QifQ.eyJuYW1lIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwic3ViIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwicm9sZSI6WyJCYXNpYyJdLCJvaV9wcnN0IjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0IiwiY2xpZW50X2lkIjoiY2RmZDcxNzUtNTc0OC00YWIyLTlkYmEtMmU5Y2FhNzFhMDg0Iiwib2lfdGtuX2lkIjoiYjFlZDRlYjktYTczNi00ZGFmLWFmYjAtZGU4OGIyNzMwYmU4IiwiYXVkIjoiaWRlbnRpdHkuYnJpZ2guaWQiLCJzY29wZSI6Im9wZW5pZCIsImV4cCI6MTYyODIyODY4MCwiaXNzIjoiaHR0cHM6Ly9pZGVudGl0eS5icmlnaC5pZC8iLCJpYXQiOjE2MjgyMjUwODB9.Edt8EEAqKRkld5R4yJ5CE1XtM3dleoLa-sat4ypZ3NRNziAcBMZFOJFp0kTpbZ8791JPS9MBc5mL6BGHJkiH-Q";
        private readonly HttpClient httpClient;
        private readonly SigningKeyResolver signingKeyResolver;
        private readonly TokenCryptoService signingKeyService;
        private readonly MemoryCache memoryCache;
        private readonly JwtSecurityTokenHandler securityTokenHandler;
        private readonly MetadataProvider metadataProvider;
        private readonly TokenResponseValidator tokenValidator;

        public TokenResponseValidatorBenchmarks()
        {
            httpClient = new HttpClient { BaseAddress = new Uri("https://identity.brigh.id/"), DefaultRequestVersion = new Version(2, 0) };
            signingKeyService = new TokenCryptoService(httpClient);
            memoryCache = new MemoryCache(new MemoryCacheOptions());
            signingKeyResolver = new SigningKeyResolver(memoryCache, signingKeyService);
            securityTokenHandler = new JwtSecurityTokenHandler();
            metadataProvider = new MetadataProvider();
            tokenValidator = new TokenResponseValidator(metadataProvider, signingKeyResolver, signingKeyService, securityTokenHandler);
        }

        [Benchmark(OperationsPerInvoke = 2)]
        public async Task ValidateTokenResponseWithoutCache()
        {
            memoryCache.Compact(1);
            await tokenValidator.ValidateTokenResponse(new TokenResponse { IdToken = IdToken, AccessToken = AccessToken });
        }

        [Benchmark]
        public async Task ValidateTokenResponseWithCache()
        {
            await tokenValidator.ValidateTokenResponse(new TokenResponse { IdToken = IdToken, AccessToken = AccessToken });
        }
    }
}
