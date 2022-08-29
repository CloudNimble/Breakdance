using Bunit.TestDoubles;
using CloudNimble.Breakdance.Blazor;
using CloudNimble.Breakdance.Tests.Blazor.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.Blazor
{

    /// <summary>
    /// Tests the functionality of <see cref="BlazorBreakdanceTestBase"/>.
    /// </summary>
    [TestClass]
    public class BlazorBreakdanceTestBase_CoreTests : BlazorBreakdanceTestBase
    {
        /// <summary>
        /// Tests whether or not a <see cref="Bunit.TestContext"/> is created on setup.
        /// </summary>
        [TestMethod]
        public void BlazorBreakdanceTestBase_Setup_CreatesTestContext_NoServices()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestHost.Should().BeNull();
            BUnitTestContext.Should().BeNull();
            RegisterServices.Should().BeNull();
            GetService<IConfiguration>().Should().BeNull();

            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
            RegisterServices.Should().BeNull();
            TestHost.Services.GetAllServiceDescriptors().Should().HaveCount(34);

            BUnitTestContext.Services.Should().HaveCount(13);
            GetService<NavigationManager>().Should().NotBeNull().And.BeOfType(typeof(FakeNavigationManager));
            BUnitTestContext.Services.GetService<IConfiguration>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that DI works property when configuring the <see cref="Bunit.TestContext"/>.
        /// </summary>
        [TestMethod]
        public void BlazorBreakdanceTestBase_TestSetup_CreatesTestContext_ConflictingServices()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestHost.Should().BeNull();
            BUnitTestContext.Should().BeNull();
            RegisterServices.Should().BeNull();

            RegisterServices = services => {
                services.AddScoped<TestableNavigationManager>();
            };
            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
            RegisterServices.Should().NotBeNull();
            BUnitTestContext.Services.Should().HaveCount(14);
            GetService<NavigationManager>().Should().NotBeNull().And.BeOfType(typeof(FakeNavigationManager));
            GetServices<NavigationManager>().Should().HaveCount(1);
            GetService<TestableNavigationManager>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that DI works property when configuring the <see cref="Bunit.TestContext"/>.
        /// </summary>
        [TestMethod]
        public void BlazorBreakdanceTestBase_TestSetup_CreatesTestContext_Services()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestHost.Should().BeNull();
            BUnitTestContext.Should().BeNull();
            RegisterServices.Should().BeNull();

            RegisterServices = services => {
                services.AddScoped<NavigationManager>(sp => new TestableNavigationManager());
            };
            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
            RegisterServices.Should().NotBeNull();
            BUnitTestContext.Services.Should().HaveCount(14);
            GetService<NavigationManager>().Should().NotBeNull().And.BeOfType(typeof(TestableNavigationManager));
            GetServices<NavigationManager>().Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that services registered specifically on the <see cref="IHost"/> will not be registered on the <see cref="Bunit.TestContext"/>.
        /// </summary>
        [TestMethod]
        public void BlazorBreakdanceTestBase_GetServices_ReturnsServiceOnlyInTestContext()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestHost.Should().BeNull();
            BUnitTestContext.Should().BeNull();
            RegisterServices.Should().BeNull();

            TestHostBuilder.ConfigureServices((context, services) => services.AddSingleton<DummyService>());
            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
            RegisterServices.Should().BeNull();
            BUnitTestContext.Services.Should().HaveCount(13);
            BUnitTestContext.Services.GetService<DummyService>().Should().BeNull();
            TestHost.Services.GetAllServiceDescriptors().Should().HaveCount(35);
            TestHost.Services.GetService<DummyService>().Should().NotBeNull();
            GetService<DummyService>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that services registered specifically on the <see cref="IHost"/> will not be registered on the <see cref="Bunit.TestContext"/>.
        /// </summary>
        [TestMethod]
        public void BlazorBreakdanceTestBase_DI_InjectsIJSRuntime_FromTestHost()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestHost.Should().BeNull();
            BUnitTestContext.Should().BeNull();
            RegisterServices.Should().BeNull();

            TestHostBuilder.ConfigureServices((context, services) => services.AddSingleton<TestJavaScriptService>());
            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
            RegisterServices.Should().BeNull();
            BUnitTestContext.Services.Should().HaveCount(13);
            BUnitTestContext.Services.GetService<TestJavaScriptService>().Should().BeNull();
            TestHost.Services.GetAllServiceDescriptors().Should().HaveCount(35);
            TestHost.Services.GetService<TestJavaScriptService>().Should().NotBeNull();
            var service = GetService<TestJavaScriptService>();
            service.Should().NotBeNull();
            service.JSRuntime.Should().NotBeNull();
        }

    }

    public class DummyService
    {

    }

}
