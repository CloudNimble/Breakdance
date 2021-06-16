using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
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
        public void AspNetCoreBreakdanceTestBase_Setup_CreatesTestServer_NoServices()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestServer.Should().BeNull();
            RegisterServices.Should().BeNull();
            GetService<IConfiguration>().Should().BeNull();

            TestSetup();

            TestServer.Should().NotBeNull();
            RegisterServices.Should().BeNull();
            TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(27);
            GetService<IConfiguration>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that DI works property when configuring the <see cref="TestServer"/>.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_Setup_CreatesTestServer_WithRegisteredServices()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestServer.Should().BeNull();
            RegisterServices.Should().BeNull();

            RegisterServices = services => {
                services.AddScoped<DummyService>();
            };
            TestSetup();

            TestServer.Should().NotBeNull();
            RegisterServices.Should().NotBeNull();
            TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(28);
            GetService<IConfiguration>().Should().NotBeNull();
            GetService<DummyService>().Should().NotBeNull();
        }

    }

    public class DummyService
    {
    }
}
