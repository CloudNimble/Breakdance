using Breakdance.Tests.AspNetCore.Fakes;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.Breakdance.Tests.AspNetCore.Fakes;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
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
        /// Tests that the static method properly creates a <see cref="TestServer"/>.
        /// </summary>
        [TestMethod]
        public void GetTestableHttpServer_WithoutConfiguration_ThrowsException()
        {
            Action configureService = () => { var server = AspNetCoreTestHelpers.GetTestableHttpServer(); };
            configureService.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Tests that the static method can properly pass action delegates to the <see cref="IWebHostBuilder"/>.
        /// </summary>
        [TestMethod]
        public void GetTestableHttpServer_WithFullConfiguration_CreatesServer()
        {
            var server = AspNetCoreTestHelpers.GetTestableHttpServer(
                services => 
                {
                    services.AddScoped<FakeService>();
                },
                builder =>
                {
                    builder.ServerFeatures.Set(new FakeFeature());
                },
                configuration =>
                {
                    configuration.Build();
                });

            //server.Services.GetAllServiceDescriptors().Should().HaveCount(44);
            server.Features.Should().Contain(c => c.Key.Name == nameof(FakeFeature));
        }

        /// <summary>
        /// Tests that the static method correctly configures the <see cref="TestServer"/> to respond to an HTTP Get.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetTestableHttpServer_CanGet()
        {
            var server = AspNetCoreTestHelpers.GetTestableHttpServer(
                services => 
                {
                    services.AddMvcCore(setup => setup.EnableEndpointRouting = false)
                        .AddApplicationPart(typeof(HomeController).Assembly);
                },
                builder =>
                {
                    builder.UseMvcWithDefaultRoute();
                });

            var client = server.CreateClient();
            var response = await server.CreateRequest("/").GetAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Hello world!");
        }

    }

}
