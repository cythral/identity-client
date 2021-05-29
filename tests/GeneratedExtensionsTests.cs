using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Brighid.Identity.Client
{
#pragma warning disable IDE0055
    [Category("Integration")]
    public class GeneratedExtensionsTests
    {
        [TestFixture]
        public class UseBrighidIdentityApplications
        {
            [Test, Auto]
            public void ShouldAddAnApplicationServiceWithCorrectBaseUrl(
                Uri url,
                string clientId,
                string clientSecret
            )
            {
                var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Identity:IdentityServerUri"] = url.ToString(),
                    ["Identity:ClientId"] = clientId,
                    ["Identity:ClientSecret"] = clientSecret,
                })
                .Build();
 
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(configuration);
                services.ConfigureBrighidIdentity(configuration.GetSection("Identity"));

                services.UseBrighidIdentityApplications();

                var provider = services.BuildServiceProvider();
                var applicationsClient = provider.GetRequiredService<IApplicationsClient>();

                applicationsClient.Should().NotBeNull();
                ((ApplicationsClient)applicationsClient).BaseUrl.Should().Be(url.ToString().TrimEnd('/') + "/api");
            }
        }
    }
}
