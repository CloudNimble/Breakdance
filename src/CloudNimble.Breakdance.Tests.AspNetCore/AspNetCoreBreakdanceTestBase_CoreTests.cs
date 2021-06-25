using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.Breakdance.Tests.AspNetCore.Fakes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.AspNetCore
{

    /// <summary>
    /// Tests the functionality of <see cref="AspNetCoreBreakdanceTestBase"/> directly.
    /// </summary>
    [TestClass]
    public class AspNetCoreBreakdanceTestBase_CoreTests
    {

        /// <summary>
        /// Tests wether or not a <see cref="TestServer"/> is created on setup.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_Setup_CreatesTestServer_NoSetup()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();

            testBase.TestServer.Should().BeNull();
            testBase.RegisterServices.Should().BeNull();
            testBase.ConfigureHost.Should().BeNull();
            testBase.GetService<IConfiguration>().Should().BeNull();

            testBase.TestSetup();

            testBase.TestServer.Should().NotBeNull();
            testBase.RegisterServices.Should().BeNull();
            testBase.ConfigureHost.Should().BeNull();
            //testBase.TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(27);
            testBase.GetService<IConfiguration>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that DI works property when configuring services for <see cref="TestServer"/>.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_Setup_CreatesTestServer_WithRegisteredServices()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();

            testBase.TestServer.Should().BeNull();
            testBase.RegisterServices.Should().BeNull();
            testBase.ConfigureHost.Should().BeNull();

            testBase.RegisterServices = services => {
                services.AddScoped<FakeService>();
            };
            testBase.TestSetup();

            testBase.TestServer.Should().NotBeNull();
            testBase.RegisterServices.Should().NotBeNull();
            testBase.ConfigureHost.Should().BeNull();
            //testBase.TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(28);
            testBase.GetService<IConfiguration>().Should().NotBeNull();
            testBase.GetService<FakeService>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that both service configuration and host building delegates function properly.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_Setup_CreatesTestServer_WithAppBuilder()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();

            testBase.TestServer.Should().BeNull();
            testBase.RegisterServices.Should().BeNull();
            testBase.ConfigureHost.Should().BeNull();

            testBase.ConfigureHost = builder =>
            {
                builder.ServerFeatures.Set<IServerAddressesFeature>(new ServerAddressesFeature());
            };

            testBase.TestSetup();

            testBase.TestServer.Should().NotBeNull();
            testBase.RegisterServices.Should().BeNull();
            testBase.ConfigureHost.Should().NotBeNull();

            //testBase.TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(28);
            testBase.GetService<IConfiguration>().Should().NotBeNull();
            testBase.TestServer.Features.Get<IServerAddressesFeature>().Should().NotBeNull();
        }

    }

}
