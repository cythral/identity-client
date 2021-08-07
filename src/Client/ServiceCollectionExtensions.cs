using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

using Brighid.Identity.Client;
using Brighid.Identity.Client.Stores;
using Brighid.Identity.Client.Utils;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        [Obsolete("Use ConfigureBrighidIdentity<TConfig> instead, and supply a TConfig with an encrypted client secret.", false)]
        public static void ConfigureBrighidIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureBrighidIdentity<IdentityConfig>(configuration);
        }

        public static void ConfigureBrighidIdentity<TConfig>(this IServiceCollection services, IConfiguration configuration)
            where TConfig : IdentityConfig
        {
            services.ConfigureBrighidIdentity<TConfig>(configuration, new HttpClientHandler());
        }

        public static void ConfigureBrighidIdentity<TConfig>(this IServiceCollection services, IConfiguration configuration, HttpMessageHandler messageHandler)
            where TConfig : IdentityConfig
        {
            var identityConfig = configuration.Get<TConfig>() ?? throw new Exception("Could not retrieve Brighid Identity configuration.");
            var identityServerUri = identityConfig.IdentityServerUri ?? new Uri(IdentityClientConstants.DefaultIdentityServerUri);
            var identityOptions = Options.Options.Create(identityConfig!);
            var cacheOptions = new MemoryCacheOptions();
            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = identityServerUri,
                DefaultRequestVersion = new Version(2, 0),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };

            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(IdentityClientConstants.ProductName, ThisAssembly.AssemblyInformationalVersion));

            var internalServices = new ServiceCollection();
            internalServices.AddSingleton(httpClient);
            internalServices.AddSingleton(identityOptions);
            internalServices.AddSingleton<IdentityConfig>(identityConfig);
            internalServices.AddSingleton<IdentityServerClient>();
            internalServices.AddSingleton<ITokenStore, TokenStore<TConfig>>();
            internalServices.AddSingleton<IUserTokenStore, UserTokenStore>();
            internalServices.AddSingleton<ICacheUtils, DefaultCacheUtils>();
            internalServices.AddSingleton<IMetadataProvider, DefaultMetadataProvider>();
            internalServices.AddSingleton<ITokenResponseValidator, TokenResponseValidator>();
            internalServices.AddSingleton<ISigningKeyResolver, SigningKeyResolver>();
            internalServices.AddSingleton<ITokenCryptoService, TokenCryptoService>();
            internalServices.AddSingleton<ISecurityTokenValidator, JwtSecurityTokenHandler>();
            internalServices.AddSingleton<IMemoryCache>(new MemoryCache(cacheOptions));
            internalServices.AddSingleton<DelegatingHandler, ClientCredentialsHandler<TConfig>>();

            var internalServiceProvider = internalServices.BuildServiceProvider();
            services.AddSingleton(internalServiceProvider.GetRequiredService<IMetadataProvider>());
            services.AddSingleton(internalServiceProvider.GetRequiredService<ICacheUtils>());
            services.AddSingleton(internalServiceProvider.GetRequiredService<DelegatingHandler>());
            services.AddSingleton(identityOptions);
            services.AddSingleton(messageHandler);
        }

        public static void UseBrighidIdentity<TServiceType, TImplementation>(this IServiceCollection services, Uri? baseAddress = null)
            where TServiceType : class
            where TImplementation : class, TServiceType
        {
            services.UseBrighidIdentity<TServiceType, TImplementation>(options => options.BaseAddress = baseAddress);
        }

        public static void UseBrighidIdentityWithHttp2<TServiceType, TImplementation>(this IServiceCollection services, Uri? baseAddress = null)
            where TServiceType : class
            where TImplementation : class, TServiceType
        {
            services.UseBrighidIdentity<TServiceType, TImplementation>(options =>
            {
                options.BaseAddress = baseAddress;
                options.DefaultRequestVersion = new Version(2, 0);
                options.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
            });
        }

        public static void UseBrighidIdentity<TServiceType, TImplementation>(this IServiceCollection services, Action<HttpClient> configureClient)
            where TServiceType : class
            where TImplementation : class, TServiceType
        {

            var activator = ActivatorUtilities.CreateFactory(typeof(TImplementation), new Type[] { typeof(HttpClient), });

            services.AddSingleton(serviceProvider =>
            {
                var primaryHandler = serviceProvider.GetRequiredService<HttpMessageHandler>();
                var delegatingHandlers = serviceProvider.GetServices<DelegatingHandler>().ToArray();

                for (var i = 0; i < delegatingHandlers.Length; i++)
                {
                    delegatingHandlers[i].InnerHandler = i != delegatingHandlers.Length - 1
                        ? delegatingHandlers[i + 1]
                        : primaryHandler;
                }

                var httpClient = new HttpClient(delegatingHandlers[0], false);
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(IdentityClientConstants.ProductName, ThisAssembly.AssemblyInformationalVersion));
                configureClient(httpClient);

                return (TServiceType)activator(serviceProvider, new[] { httpClient });
            });
        }

#pragma warning disable IDE0051 // Used by Generators
        private static Uri GetIdentityServerApiBaseUri(IServiceCollection services)
        {
            var metadataProvider = (from service in services where service.ServiceType == typeof(IMetadataProvider) select (IMetadataProvider)service.ImplementationInstance!).First();
            var rawUrl = metadataProvider.IdentityServerUri.ToString();
            return new Uri($"{rawUrl.TrimEnd('/')}/api");
        }
#pragma warning restore IDE0051
    }
}
