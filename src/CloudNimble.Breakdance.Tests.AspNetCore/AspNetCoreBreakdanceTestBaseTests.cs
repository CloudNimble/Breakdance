using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Breakdance.Tests.AspNetCore
{

    /// <summary>
    /// Tests the functionality of <see cref="AspNetCoreBreakdanceTestBase"/> the way most end-users will use them.
    /// </summary>
    [TestClass]
    public class AspNetCoreBreakdanceTestBaseTests : AspNetCoreBreakdanceTestBase
    {
        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            RegisterServices = services => {
                services.AddScoped<DummyService>();
            };

            ConfigureHost = builder =>
            {
                // adds a middleware that will return a custom response from a designated request path
                builder.Use(async (context, next) =>
                {
                    // trigger the rest of the pipeline first so that our test code is the last thing to run
                    await next.Invoke();

                    if (context.Request.Path.Value.StartsWith("/dummy"))
                    {
                        context.Response.Clear();
                        await context.Response.WriteAsync("Hello from the dummy middleware!");
                    }

                });
            };

            TestSetup();
        }

        [TestCleanup]
        public void TearDown() => TestTearDown();

        #endregion

        /// <summary>
        /// Tests that DI works property when configuring the <see cref="TestServer"/>.
        /// </summary>
        [TestMethod]
        public void BlazorBreakdanceTestBase_Setup_CreatesTestServer_WithExpectedServices()
        {
            TestServer.Should().NotBeNull();
            RegisterServices.Should().NotBeNull();

            TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(28);
            GetService<IConfiguration>().Should().NotBeNull();
            GetService<DummyService>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the <see cref="TestServer"/> can return an HTTP response.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BlazorBreakdanceTestBase_TestServer_ReturnsResponse()
        {
            // make a GET request to ensure the pipeline completes (the path does not exist, so the response should be 404)
            var response = await TestServer.CreateClient().GetAsync("/");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Tests that the <see cref="IApplicationBuilder"/> can be configured by adding a dummy delegate to the pipeline and invoking it with a custom query path.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BlazorBreakdanceTestBase_Setup_CanConfigureMiddlewares()
        {
            var response = await TestServer.CreateClient().GetAsync("/dummy");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Hello from the dummy middleware!");
        }

    }

    public class DummyService
    {
    }
}
