using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.Breakdance.Tests.AspNetCore.Fakes;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;

namespace CloudNimble.Breakdance.Tests.AspNetCore
{

    /// <summary>
    /// Tests the functionality of <see cref="AspNetCoreBreakdanceTestBase"/> when the object is used directly instead of being used as a base class.
    /// </summary>
    [TestClass]
    public class AspNetCoreBreakdanceTestBase_CoreTests
    {

        /// <summary>
        /// Tests that a <see cref="TestServer"/> is created when EnsureTestServer() is called.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_EnsureTestServer_CreatesDefaultConfig()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.TestServer.Should().BeNull();
            testBase.GetService<IConfiguration>().Should().BeNull();

            testBase.EnsureTestServer();

            testBase.GetService<IConfiguration>().Should().NotBeNull();
            testBase.TestServer.Features.As<IEnumerable>().Should().BeEmpty();
        }

        /// <summary>
        /// Tests that DI works property when configuring services for <see cref="TestServer"/>.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_TestServer_CanRegisterServices()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.TestServer.Should().BeNull();
            testBase.GetService<IConfiguration>().Should().BeNull();
            testBase.GetService<FakeService>().Should().BeNull();

            testBase.TestHostBuilder.ConfigureServices(services => {
                services.AddScoped<FakeService>();
            });

            testBase.EnsureTestServer();

            testBase.TestServer.Should().NotBeNull();
            testBase.GetService<IConfiguration>().Should().NotBeNull();
            testBase.GetService<FakeService>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the <see cref="TestServer"/> has the expected services.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_AddMinimalMvc_HasExpectedServices()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.TestServer.Should().BeNull();

            testBase.AddMinimalMvc(options => options.EnableEndpointRouting = false);
            testBase.EnsureTestServer();

            testBase.TestServer.Should().NotBeNull();
            testBase.GetService<IConfiguration>().Should().NotBeNull();

            // JHC TODO: add asserts here using testBase.GetService<T> to ensure that the expected services were registered on the host
        }

        /// <summary>
        /// Tests that the <see cref="TestServer"/> has the expected services.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_AddApis_HasExpectedServices()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.TestServer.Should().BeNull();

            testBase.AddApis(options => options.EnableEndpointRouting = false);
            testBase.EnsureTestServer();

            testBase.TestServer.Should().NotBeNull();
            testBase.GetService<IConfiguration>().Should().NotBeNull();

            // JHC TODO: add asserts here using testBase.GetService<T> to ensure that the expected services were registered on the host
        }

        /// <summary>
        /// Tests that the <see cref="TestServer"/> has the expected services.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_AddViews_HasExpectedServices()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.TestServer.Should().BeNull();

            testBase.AddViews(options => options.EnableEndpointRouting = false);
            testBase.EnsureTestServer();

            testBase.TestServer.Should().NotBeNull();
            testBase.GetService<IConfiguration>().Should().NotBeNull();

            // JHC TODO: add asserts here using testBase.GetService<T> to ensure that the expected services were registered on the host
        }

        /// <summary>
        /// Tests that the <see cref="TestServer"/> has the expected services.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_AddRazorPages_HasExpectedServices()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.TestServer.Should().BeNull();

            testBase.AddRazorPages();
            testBase.EnsureTestServer();

            testBase.TestServer.Should().NotBeNull();
            testBase.GetService<IConfiguration>().Should().NotBeNull();

            // JHC TODO: add asserts here using testBase.GetService<T> to ensure that the expected services were registered on the host
        }

    }

}
