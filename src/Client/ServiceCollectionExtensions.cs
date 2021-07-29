using System;
using System.Linq;
using System.Net.Http;

using Brighid.Identity.Client;
using Brighid.Identity.Client.Stores;
using Brighid.Identity.Client.Utils;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static void ConfigureBrighidIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<IdentityConfig>(configuration);
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
            var identityConfig = configuration.Get<TConfig>() ?? throw new Exception("Could not retrieve Brighid Identity configuration. ");
            var identityServerUri = identityConfig.IdentityServerUri ?? new Uri(IdentityClientConstants.DefaultIdentityServerUri);
            var identityOptions = Options.Options.Create(identityConfig!);
            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = identityServerUri,
                DefaultRequestVersion = new Version(2, 0),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };

            var identityServerClient = new IdentityServerClient(httpClient);
            var tokenStore = new TokenStore<TConfig>(identityServerClient, identityOptions);
            var userTokenStore = new UserTokenStore(tokenStore, identityServerClient);
            var cacheUtils = new DefaultCacheUtils(tokenStore, userTokenStore);

            services.AddSingleton<ICacheUtils>(cacheUtils);
            services.AddSingleton(messageHandler);
            services.AddTransient<DelegatingHandler>(sp => new ClientCredentialsHandler<TConfig>(tokenStore, userTokenStore, identityOptions));
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
                configureClient(httpClient);
                return (TServiceType)activator(serviceProvider, new[] { httpClient });
            });
        }

#pragma warning disable IDE0051 // Used by Generators
        private static Uri GetIdentityServerApiBaseUri(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var config = provider.GetRequiredService<IOptions<IdentityConfig>>().Value;
            var rawUrl = config.IdentityServerUri.ToString();
            return new Uri($"{rawUrl.TrimEnd('/')}/api");
        }

        private static void ChangeFactoryDescriptorToSingleton<TServiceType>(this IServiceCollection services)
        {
            var oldDescriptor = (from service in services where service.ServiceType == typeof(TServiceType) select service).First();
            var newDescriptor = new ServiceDescriptor(oldDescriptor.ServiceType, oldDescriptor.ImplementationFactory!, ServiceLifetime.Singleton);

            services.Remove(oldDescriptor);
            services.Add(newDescriptor);
        }
#pragma warning restore IDE0051
    }
}
