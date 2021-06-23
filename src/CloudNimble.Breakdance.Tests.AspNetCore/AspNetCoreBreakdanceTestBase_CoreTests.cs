using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.AspNetCore
{

    /// <summary>
    /// Tests the functionality of <see cref="AspNetCoreBreakdanceTestBase"/>
    /// </summary>
    [TestClass]
    public class AspNetCoreBreakdanceTestBase_CoreTests : AspNetCoreBreakdanceTestBase
    {
        /// <summary>
        /// Tests wether or not a <see cref="TestServer"/> is created on setup.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_Setup_CreatesTestServer_NoSetup()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestServer.Should().BeNull();
            RegisterServices.Should().BeNull();
            ConfigureHost.Should().BeNull();
            GetService<IConfiguration>().Should().BeNull();

            TestSetup();

            TestServer.Should().NotBeNull();
            RegisterServices.Should().BeNull();
            ConfigureHost.Should().BeNull();
            TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(27);
            GetService<IConfiguration>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that DI works property when configuring services for <see cref="TestServer"/>.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_Setup_CreatesTestServer_WithRegisteredServices()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestServer.Should().BeNull();
            RegisterServices.Should().BeNull();
            ConfigureHost.Should().BeNull();

            RegisterServices = services => {
                services.AddScoped<DummyService>();
            };
            TestSetup();

            TestServer.Should().NotBeNull();
            RegisterServices.Should().NotBeNull();
            ConfigureHost.Should().BeNull();
            TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(28);
            GetService<IConfiguration>().Should().NotBeNull();
            GetService<DummyService>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that both service configuration and host building delegates function properly.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_Setup_CreatesTestServer_WithRegisteredServicesAndBuilder()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestServer.Should().BeNull();
            RegisterServices.Should().BeNull();
            ConfigureHost.Should().BeNull();

            RegisterServices = services => {
                services.AddScoped<DummyService>();
            };

            ConfigureHost = builder =>
            {
                builder.ServerFeatures.Set<IServerAddressesFeature>(new ServerAddressesFeature());
            };

            TestSetup();

            TestServer.Should().NotBeNull();
            RegisterServices.Should().NotBeNull();
            ConfigureHost.Should().NotBeNull();

            TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(28);
            GetService<IConfiguration>().Should().NotBeNull();
            GetService<DummyService>().Should().NotBeNull();
            TestServer.Features.Get<IServerAddressesFeature>().Should().NotBeNull();
        }

    }
}
