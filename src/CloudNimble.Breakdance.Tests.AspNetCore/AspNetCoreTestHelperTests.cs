using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.AspNetCore
{

    /// <summary>
    /// Tests the static methods provided in <see cref="AspNetCoreTestHelpers"/>.
    /// </summary>
    [TestClass]
    public class AspNetCoreTestHelperTests
    {
        /// <summary>
        /// Tests that the static method properly creates a <see cref="TestServer"/> with expected defaults in its <see cref="IServiceCollection"/>.
        /// </summary>
        [TestMethod]
        public async Task CanGetTestableHttpServer_WithoutConfiguration()
        {
            var server = await AspNetCoreTestHelpers.GetTestableHttpServer();
            server.Should().NotBeNull();
            server.Services.GetAllServiceDescriptors().Should().HaveCount(43);
        }

        /// <summary>
        /// Tests that the static method properly creates a <see cref="TestServer"/> that can generate an <see cref="HttpClient"/>.
        /// </summary>
        [TestMethod]
        public async Task CanGetTestableHttpClient_FromServer()
        {
            var server = await AspNetCoreTestHelpers.GetTestableHttpServer();
            var client = server.CreateClient();
            client.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the static method can properly pass configuration delegates to the <see cref="IHostBuilder"/>.
        /// </summary>
        [TestMethod]
        public async Task CanGetTestableHttpServer_WithFullConfiguration()
        {
            var server = await AspNetCoreTestHelpers.GetTestableHttpServer(
                services => {
                    services.AddScoped<DummyService>();
                },
                builder =>
                {
                    builder.ServerFeatures.Set(new EmptyFeature());
                });

            server.Services.GetAllServiceDescriptors().Should().HaveCount(44);
            server.Features.Should().Contain(c => c.Key.Name == nameof(EmptyFeature));
        }

        /// <summary>
        /// Tests that the static method correctly configures and starts up the <see cref="TestServer"/>.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CanGetTestableHttpServer_WithFunctioningClient()
        {
            var server = await AspNetCoreTestHelpers.GetTestableHttpServer(
                services => {
                    services.AddScoped<DummyService>();
                },
                builder =>
                {
                    builder.ServerFeatures.Set(new EmptyFeature());
                });

            server.Services.GetAllServiceDescriptors().Should().HaveCount(44);
            server.Features.Should().Contain(c => c.Key.Name == nameof(EmptyFeature));

            var client = server.CreateClient();
            var response = await client.GetAsync("/");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

    }
}
