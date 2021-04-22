using System;

using Brighid.Identity.Client;

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static void ConfigureBrighidIdentity(this IServiceCollection serviceCollection, Action<IdentityOptionsBuilder> configure)
        {
            var builder = new IdentityOptionsBuilder(serviceCollection);
            configure(builder);
            builder.Build();
        }

        public static void UseBrighidIdentity<TServiceType, TImplementation>(this IServiceCollection services)
            where TServiceType : class
            where TImplementation : class, TServiceType
        {
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<ClientCredentials>>().Value;

            services
            .AddHttpClient<TServiceType, TImplementation>(typeof(TImplementation).FullName, options => options.BaseAddress = options.BaseAddress)
            .AddHttpMessageHandler<ClientCredentialsHandler>();
        }
    }
}
