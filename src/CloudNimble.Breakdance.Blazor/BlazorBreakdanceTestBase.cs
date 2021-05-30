using Bunit;
using CloudNimble.Breakdance.Assemblies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// An <see cref="Action{IServiceCollection}"/> that lets you register additional services with the DI container.
        /// </summary>
        public Action<IServiceCollection> RegisterServices { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        public T GetService<T>() where T : class => BUnitTestContext?.Services.GetService<T>() ?? TestHost?.Services.GetService<T>();

        /// <summary>
        /// Get an enumeration of services of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
        public IEnumerable<T> GetServices<T>() where T : class  => BUnitTestContext?.Services.GetServices<T>() ?? TestHost?.Services.GetServices<T>();

        /// <summary>
        /// Properly instantiates the <see cref="BUnitTestContext"/> and if <see cref="RegisterServices"/> is not null, properly registers additional services with the context.
        /// </summary>
        public override void TestSetup()
        {
            base.TestSetup();
            BUnitTestContext = new TestContext();
            if (RegisterServices != null)
            {
                RegisterServices.Invoke(BUnitTestContext.Services);
            }
            var configuration = TestHost.Services.GetService<IConfiguration>();
            BUnitTestContext.Services.AddSingleton(configuration);
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
