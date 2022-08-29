using Bunit;
using CloudNimble.Breakdance.Assemblies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace CloudNimble.Breakdance.Blazor
{

    /// <summary>
    /// A base class for building BUnit unit tests for Blazor apps that automatically handles basic registration stuff for you.
    /// </summary>
    public class BlazorBreakdanceTestBase : BreakdanceTestBase
    {

        #region Properties

        /// <summary>
        /// The bUnit <see cref="TestContext"/> for the currently-executing test.
        /// </summary>
        public TestContext BUnitTestContext { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        public override T GetService<T>() where T : class => 
            BUnitTestContext?.Services.GetService<T>() ?? base.GetService<T>();

        /// <summary>
        /// Get an enumeration of services of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
        public override IEnumerable<T> GetServices<T>() where T : class => 
            BUnitTestContext?.Services.GetServices<T>() ?? base.GetServices<T>();

        /// <summary>
        /// Properly instantiates the <see cref="BUnitTestContext"/> and registers the <see cref="BreakdanceTestBase.TestHost">TestHost's</see> 
        /// <see cref="IHost.Services"/> as a "fallback" <see cref="IServiceProvider"/>.
        /// </summary>
        public override void TestSetup()
        {
            TestSetup(JSRuntimeMode.Loose);
        }

        /// <summary>
        /// Properly instantiates the <see cref="BUnitTestContext"/> and registers the <see cref="BreakdanceTestBase.TestHost">TestHost's</see> 
        /// <see cref="IHost.Services"/> as a "fallback" <see cref="IServiceProvider"/> and allows you to set the bUnit JSInterop mode.
        /// </summary>
        public void TestSetup(JSRuntimeMode jSRuntimeMode)
        {
            BUnitTestContext = new TestContext();
            BUnitTestContext.JSInterop.Mode = jSRuntimeMode;
            // RWM: Make the BUnit JSRuntime available to the TestHost services (in case a service that requires it is materialized from the base container.
            TestHostBuilder.ConfigureServices(services => services.AddSingleton((sp) => BUnitTestContext.JSInterop.JSRuntime));
            base.TestSetup();
            BUnitTestContext.Services.AddFallbackServiceProvider(TestHost.Services);
        }

        /// <summary>
        /// Disposes of the <see cref="BUnitTestContext"/>.
        /// </summary>
        public override void TestTearDown()
        {
            base.TestTearDown();
            BUnitTestContext?.Dispose();
        }

        #endregion

    }

}
