using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brighid.Identity.Client
{

#pragma warning disable CA1812

    internal class IdentityServicesConfigurer : IIdentityServicesConfigurer
    {
        private readonly IServiceCollection services;

        public IdentityServicesConfigurer(IServiceCollection services)
        {
            this.services = services;
        }

        public void ConfigureServices(ConfigurationContext context)
        {
            var section = Configuration.GetSection(context.ConfigSectionName);
            services.Configure<ClientCredentials>(section);
            services.TryAddSingleton<TokenCache>();
            services.TryAddScoped<IdentityServerClient>();
            services.TryAddScoped<ClientCredentialsHandler>();

            services
            .AddHttpClient<IdentityServerClient>(options => options.BaseAddress = context.IdentityServerUri);
        }

        private IConfiguration Configuration =>
            services.BuildServiceProvider().GetRequiredService<IConfiguration>();
    }
}
