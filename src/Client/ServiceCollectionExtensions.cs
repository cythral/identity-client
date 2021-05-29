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
        public static void ConfigureBrighidIdentity(this IServiceCollection services, string sectionName)
        {
            var config = (IConfiguration)(from service in services
                                          where service.ServiceType == typeof(IConfiguration)
                                          select service.ImplementationInstance).First();

            var section = config.GetSection(sectionName);
            services.Configure<IdentityConfig>(section);
            services.ConfigureBrighidIdentity<IdentityConfig>(sectionName);
        }

        public static void ConfigureBrighidIdentity<TConfig>(this IServiceCollection services, string sectionName)
            where TConfig : IdentityConfig
        {
            var config = (IConfiguration)(from service in services
                                          where service.ServiceType == typeof(IConfiguration)
                                          select service.ImplementationInstance).First();

            services.TryAddSingleton<ITokenStore, TokenStore>();
            services.TryAddScoped<IdentityServerClient>();
            services.TryAddTransient<DelegatingHandler, ClientCredentialsHandler<TConfig>>();

            var section = config.GetSection(sectionName);
            var identityServerUri = section.GetValue("IdentityServerUri", new Uri(DefaultIdentityServerUri));

            services
            .AddHttpClient<IdentityServerClient>(options => options.BaseAddress = identityServerUri)
            .ConfigureHttpMessageHandlerBuilder(builder =>
            {
                ConfigurePrimaryHandler(builder);
            });
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
    }
}
