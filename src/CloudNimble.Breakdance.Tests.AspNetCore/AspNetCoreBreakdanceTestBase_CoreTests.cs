using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.Breakdance.Tests.AspNetCore.Fakes;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Net;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.AspNetCore
{

    /// <summary>
    /// Tests the functionality of <see cref="AspNetCoreBreakdanceTestBase"/> when the object is used directly instead of being used as a base class.
    /// </summary>
    [TestClass]
    public class AspNetCoreBreakdanceTestBase_CoreTests
    {

        /// <summary>
        /// Tests that a an exception is thrown if the <see cref="TestServer"/> does not have the minimal required configuration.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_EnsureTestServer_ThrowsExceptionWhenUnconfigured()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.TestServer.Should().BeNull();
            testBase.GetService<IConfiguration>().Should().BeNull();

            Action act = () => { testBase.EnsureTestServer(); };
            act.Should().Throw<InvalidOperationException>();
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
        /// Tests that the <see cref="TestServer"/> has the expected set of services when calling the helper.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_AddMinimalMvc_HasExpectedDefaults()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.TestServer.Should().BeNull();

            testBase.AddMinimalMvc();
            testBase.EnsureTestServer();

            testBase.TestServer.Should().NotBeNull();
            testBase.GetService<IConfiguration>().Should().NotBeNull();

            // JHC TODO: add asserts here using testBase.GetService<T> to ensure that the expected services were registered on the host
        }

        /// <summary>
        /// Tests that the helper method on the <see cref="TestServer"/> uses the provided <see cref="IApplicationBuilder"/>.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AspNetCoreBreakdanceTestBase_AddMinimalMvc_CanInvokeMiddleware()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.AddMinimalMvc(app: builder =>
            {
                builder.UseWelcomePage("/welcome");
            });

            testBase.EnsureTestServer();

            var response = await testBase.TestServer.CreateRequest("/welcome").GetAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Your ASP.NET Core application has been successfully started.");
        }

        /// <summary>
        /// Tests that the <see cref="TestServer"/> has the expected set of services when calling the helper.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_AddApis_HasExpectedDefaults()
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
        /// Tests that the <see cref="TestServer"/> has the expected set of services when calling the helper.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_AddViews_HasExpectedDefaults()
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
        /// Tests that the <see cref="TestServer"/> has the expected set of services when calling the helper.
        /// </summary>
        [TestMethod]
        public void AspNetCoreBreakdanceTestBase_AddRazorPages_HasExpectedDefaults()
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
