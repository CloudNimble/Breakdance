using Bunit.TestDoubles;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.Blazor;
using CloudNimble.Breakdance.Tests.Blazor.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CloudNimble.Breakdance.Tests.Blazor
{

    /// <summary>
    /// Tests the functionality of <see cref="BlazorBreakdanceTestBase"/>.
    /// </summary>
    [TestClass]
    public class BlazorBreakdanceTestBase_CoreTests : BlazorBreakdanceTestBase
    {

        private const string projectPath = "..//..//..//";

        /// <summary>
        /// Tests whether or not a <see cref="Bunit.TestContext"/> is created on setup.
        /// </summary>
        [TestMethod]
        public void BlazorBreakdanceTestBase_Setup_CreatesTestContext_NoServices()
        {
            //RWM: We're not *quite* setting this up properly, because we want to test the state both before and after calling TestSetup();
            TestHost.Should().BeNull();
            BUnitTestContext.Should().BeNull();
            GetService<IConfiguration>().Should().BeNull();

            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
#if NET8_0_OR_GREATER
            TestHost.Services.GetAllServiceDescriptors().Should().HaveCount(43);
#endif
            BUnitTestContext.Services.Should().HaveCount(26);
            GetService<NavigationManager>().Should().NotBeNull().And.BeOfType(typeof(BunitNavigationManager));
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

            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
            BUnitTestContext.Services.Should().HaveCount(26);
            GetService<NavigationManager>().Should().NotBeNull().And.BeOfType(typeof(BunitNavigationManager));
            GetServices<NavigationManager>().Should().HaveCount(1);
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

            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
            BUnitTestContext.Services.Should().HaveCount(26);
            GetServices<NavigationManager>().Should().HaveCount(1);
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

            TestHostBuilder.ConfigureServices((context, services) => services.AddSingleton<DummyService>());
            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
            BUnitTestContext.Services.Should().HaveCount(26);
            BUnitTestContext.Services.GetService<DummyService>().Should().NotBeNull();
#if NET8_0_OR_GREATER
            TestHost.Services.GetAllServiceDescriptors().Should().HaveCount(44);
#endif
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

            TestHostBuilder.ConfigureServices((context, services) => services.AddSingleton<TestJavaScriptService>());
            TestSetup();

            TestHost.Should().NotBeNull();
            BUnitTestContext.Should().NotBeNull();
            BUnitTestContext.Services.Should().HaveCount(26);
            BUnitTestContext.Services.GetService<TestJavaScriptService>().Should().NotBeNull();
#if NET8_0_OR_GREATER
            TestHost.Services.GetAllServiceDescriptors().Should().HaveCount(44);
#endif
            TestHost.Services.GetService<TestJavaScriptService>().Should().NotBeNull();
            var service = GetService<TestJavaScriptService>();
            service.Should().NotBeNull();
            service.JSRuntime.Should().NotBeNull();
        }

        //[DataRow(projectPath)]
        //[TestMethod]
        [BreakdanceManifestGenerator]
        public void WriteHostBuilderOutputLog(string projectPath)
        {
            TestHostBuilder.ConfigureServices((context, services) => services.AddSingleton<TestJavaScriptService>());
            TestSetup();

            var result = TestHost.Services.GetContainerContentsLog();
#if NET8_0_OR_GREATER
            var fullPath = Path.Combine(projectPath, "Baselines//BlazorTestHost_NET8.txt");
#endif
            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, result);
        }

    }

    public class DummyService
    {

    }

}
