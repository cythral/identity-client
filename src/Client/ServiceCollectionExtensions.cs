using System;
using System.Linq;
using System.Net.Http;

using Brighid.Identity.Client;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        private const string DefaultIdentityServerUri = "http://identity.brigh.id";
        public static void ConfigureBrighidIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<IdentityConfig>(configuration);
            services.ConfigureBrighidIdentity<IdentityConfig>(configuration);
        }

        public static void ConfigureBrighidIdentity<TConfig>(this IServiceCollection services, IConfiguration configuration)
            where TConfig : IdentityConfig
        {
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            services.TryAddSingleton<ITokenStore, TokenStore<TConfig>>();
            services.TryAddSingleton<IUserTokenStore, UserTokenStore>();
            services.TryAddTransient<DelegatingHandler, ClientCredentialsHandler<TConfig>>();

            var identityServerUri = configuration.GetValue("IdentityServerUri", new Uri(DefaultIdentityServerUri));

            services
            .AddHttpClient<IdentityServerClient>(options => options.BaseAddress = identityServerUri)
            .ConfigureHttpMessageHandlerBuilder(builder =>
            {
                ConfigurePrimaryHandler(builder);
            });

            services.ChangeFactoryDescriptorToSingleton<IdentityServerClient>();
        }

        public static void UseBrighidIdentity<TServiceType, TImplementation>(this IServiceCollection services, Uri? baseAddress = null)
            where TServiceType : class
            where TImplementation : class, TServiceType
        {
            services
            .AddHttpClient<TServiceType, TImplementation>(typeof(TImplementation).FullName, options => options.BaseAddress = baseAddress)
            .ConfigureHttpMessageHandlerBuilder(builder =>
            {
                ConfigurePrimaryHandler(builder);
                ConfigureAdditionalHandlers(builder);
            });

            services.ChangeFactoryDescriptorToSingleton<TServiceType>();
        }

        private static void ConfigurePrimaryHandler(Http.HttpMessageHandlerBuilder builder)
        {
            var primaryHandler = builder.Services.GetService<HttpMessageHandler>();
            if (primaryHandler != null)
            {
                builder.PrimaryHandler = primaryHandler;
            }
        }

        private static void ConfigureAdditionalHandlers(Http.HttpMessageHandlerBuilder builder)
        {
            var delegatingHandlers = builder.Services.GetServices<DelegatingHandler>();
            foreach (var handler in delegatingHandlers)
            {
                builder.AdditionalHandlers.Add(handler);
            }
        }

#pragma warning disable IDE0051
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
            Console.WriteLine(oldDescriptor);
            var newDescriptor = new ServiceDescriptor(oldDescriptor.ServiceType, oldDescriptor.ImplementationFactory!, ServiceLifetime.Singleton);

            services.Remove(oldDescriptor);
            services.Add(newDescriptor);
        }
    }
}
