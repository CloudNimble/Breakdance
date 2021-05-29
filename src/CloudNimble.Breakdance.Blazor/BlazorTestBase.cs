using Bunit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace CloudNimble.Breakdance.Blazor
{

    /// <summary>
    /// A base class for building BUnit unit tests for Blazor apps that automatically handles basic registration stuff for you.
    /// </summary>
    public class BlazorTestBase
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
        public T GetService<T>() => BUnitTestContext.Services.GetService<T>();

        /// <summary>
        /// Get an enumeration of services of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
        public IEnumerable<T> GetServices<T>() => BUnitTestContext.Services.GetServices<T>();

        /// <summary>
        /// Properly instantiates the <see cref="BUnitTestContext"/> and if <see cref="RegisterServices"/> is not null, properly registers additional services with the context.
        /// </summary>
        public void TestSetup()
        {
            BUnitTestContext = new TestContext();
            if (RegisterServices != null)
            {
                RegisterServices.Invoke(BUnitTestContext.Services);
            }
        }

        /// <summary>
        /// Disposes of the <see cref="BUnitTestContext"/>.
        /// </summary>
        public void TestTearDown()
        {
            BUnitTestContext?.Dispose();
        }

        #endregion

    }

}
