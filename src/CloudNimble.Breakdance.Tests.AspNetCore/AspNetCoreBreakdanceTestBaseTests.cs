using Breakdance.Tests.AspNetCore.Fakes;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.Breakdance.Tests.AspNetCore.Fakes;
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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.AspNetCore
{

    /// <summary>
    /// Tests the functionality of <see cref="AspNetCoreBreakdanceTestBase"/> when using inheritance.
    /// </summary>
    [TestClass]
    public class AspNetCoreBreakdanceTestBaseTests : AspNetCoreBreakdanceTestBase
    {
        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            RegisterServices = services =>
            {
                services.AddScoped<FakeService>();

                services.AddMvcCore(setup => setup.EnableEndpointRouting = false)
                    .AddApplicationPart(typeof(HomeController).Assembly);

            };

            ConfigureHost = builder =>
            {
                builder.UseMvcWithDefaultRoute();
                builder.UseResponseCaching();

                // adds a middleware that will return a custom response from a designated request path
                builder.Use(async (context, next) =>
                {
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
            //TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(28);
            GetService<IConfiguration>().Should().NotBeNull();
            GetService<FakeService>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the <see cref="TestServer"/> can generate an <see cref="HttpClient"/> and return an HTTP response.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BlazorBreakdanceTestBase_TestServer_CanCreateClient()
        {
            // make a GET request to ensure the pipeline completes (the path does not exist, so the response should be 404)
            var response = await TestServer.CreateClient().GetAsync("/");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Hello world!");
        }

        /// <summary>
        /// Tests that the <see cref="TestServer"/> can generate an <see cref="HttpRequest"/> directly.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BlazorBreakdanceTestBase_TestServer_CanCreateRequest()
        {
            var response = await TestServer.CreateRequest("/").GetAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Hello world!");
        }

        /// <summary>
        /// Tests that the <see cref="TestServer"/> can generate an HTTP post and receive a response.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BlazorBreakdanceTestBase_TestServer_CanPostData()
        {
            var client = TestServer.CreateClient();

            var phrase = "Hello from net5!";
            var postData = new Dictionary<string, string>
            {
                { "saySomething", phrase }
            };

            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "home/testpost")
            {
                Content = new FormUrlEncodedContent(postData)
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadAsStringAsync();
            result.Should().Contain(phrase);
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
}
