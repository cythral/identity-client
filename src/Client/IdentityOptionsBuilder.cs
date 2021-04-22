using System;

using Microsoft.Extensions.DependencyInjection;

namespace Brighid.Identity.Client
{
    public class IdentityOptionsBuilder
    {
        private readonly IServiceCollection services;
        private readonly ConfigurationContext context;
        private IIdentityServicesConfigurer? configurer;

        public IdentityOptionsBuilder(IServiceCollection services)
        {
            this.services = services;
            context = new ConfigurationContext();
        }

        public IdentityOptionsBuilder WithIdentityServerUri(Uri identityServerUri)
        {
            context.IdentityServerUri = identityServerUri;
            return this;
        }

        public IdentityOptionsBuilder WithBaseAddress(string baseAddress)
        {
            context.BaseAddress = baseAddress;
            return this;
        }

        public IdentityOptionsBuilder WithCredentials(string sectionName)
        {
            context.ConfigSectionName = sectionName;
            configurer = new IdentityServicesConfigurer(services);
            return this;
        }

        internal void Build()
        {
            if (configurer == null)
            {
                throw new Exception("Configuration for Brighid Identity not setup.");
            }

            configurer.ConfigureServices(context);
        }
    }
}
