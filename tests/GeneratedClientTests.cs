using System.Net.Http;
using System.Net.Http.Headers;

using FluentAssertions;

using NUnit.Framework;

namespace Brighid.Identity.Client
{
#pragma warning disable IDE0055, IDE0017
    [Category("Integration")]
    public class GeneratedClientTests
    {
        [TestFixture]
        public class SetToken
        {
            [Test, Auto]
            public void ShouldAddADefaultAuthorizationHeader(
                string token
            )
            {
                var httpClient = new HttpClient();
                var applicationsClient = new ApplicationsClient(httpClient);
                applicationsClient.Token = token;
                httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
                httpClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be(token);
            }

            [Test, Auto]
            public void ShouldRemoveTheDefaultAuthorizationHeaderIfNullGiven(
                string token
            )
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var applicationsClient = new ApplicationsClient(httpClient);
                applicationsClient.Token = null;
                httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
            }
        }
    }
}
