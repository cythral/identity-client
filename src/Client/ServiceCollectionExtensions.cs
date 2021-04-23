using System;

using Brighid.Identity.Client;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static void ConfigureBrighidIdentity(this IServiceCollection services, string sectionName)
        {
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var section = config.GetSection(sectionName);
            services.Configure<IdentityConfig>(section);
            services.TryAddSingleton<TokenCache>();
            services.TryAddScoped<IdentityServerClient>();
            services.TryAddTransient<ClientCredentialsHandler>();

            var identityServerUri = section.GetValue("IdentityServerUri", new Uri("http://identity.brigh.id"));

            services
            .AddHttpClient<IdentityServerClient>(options => options.BaseAddress = identityServerUri);
        }

        public static void UseBrighidIdentity<TServiceType, TImplementation>(this IServiceCollection services, Uri? baseAddress = null)
            where TServiceType : class
            where TImplementation : class, TServiceType
        {
            services
            .AddHttpClient<TServiceType, TImplementation>(typeof(TImplementation).FullName, options => options.BaseAddress = baseAddress)
            .AddHttpMessageHandler<ClientCredentialsHandler>();
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
