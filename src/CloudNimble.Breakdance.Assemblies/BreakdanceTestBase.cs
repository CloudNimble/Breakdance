using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using CloudNimble.EasyAF.Core;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// A base class for unit tests that maintains an <see cref="IHost"/> with configuration and a Dependency Injection container.
    /// </summary>
    public abstract class BreakdanceTestBase : IDisposable
    {
        private bool disposedValue;

        #region Properties

        /// <summary>
        /// The <see cref="IHost"/> instance containing the test host.
        /// </summary>
        public IHost TestHost { get; internal set; }

        /// <summary>
        /// The <see cref="IHostBuilder"/> instance used to configure the test host.
        /// </summary>
        public IHostBuilder TestHostBuilder { get; internal set; }

        /// <summary>
        /// Provides a default <see cref="IServiceScope"/> implementation to contain scoped services.
        /// </summary>
        public IServiceScope DefaultScope { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="BreakdanceTestBase"/> instance. 
        /// </summary>
        public BreakdanceTestBase()
        {
            TestHostBuilder = Host.CreateDefaultBuilder();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Method used by test assemblies to setup the environment.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [AssemblyInitialize].
        /// With NUnit, use [OneTimeSetup].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public virtual void AssemblySetup()
        {
            EnsureTestHost();
        }

        /// <summary>
        /// Method used by test assemblies to clean up the environment.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [AssemblyCleanup].
        /// With NUnit, use [OneTimeTearDown].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public virtual void AssemblyTearDown()
        {
            Dispose();
        }

        /// <summary>
        /// Method used by test classes to setup the environment.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [ClassInitialize].
        /// With NUnit, use [OneTimeSetup].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public virtual void ClassSetup()
        {
            EnsureTestHost();
        }

        /// <summary>
        /// Method used by test classes to clean up the environment.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [ClassCleanup].
        /// With NUnit, use [OneTimeTearDown].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public virtual void ClassTearDown()
        {
            Dispose();
        }

        /// <summary>
        /// Clean up disposable objects in the environment.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Method used by test classes to setup the environment.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [TestInitialize].
        /// With NUnit, use [Setup].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public virtual void TestSetup()
        {
            EnsureTestHost();
        }

        /// <summary>
        /// Method used by test classes to clean up the environment.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [TestCleanup].
        /// With NUnit, use [TearDown].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public virtual void TestTearDown()
        {
        }

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        public virtual T GetService<T>() where T : class => TestHost?.Services.GetService<T>();

        /// <summary>
        /// Get an enumeration of services of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
        public virtual IEnumerable<T> GetServices<T>() where T : class => TestHost?.Services.GetServices<T>();

        /// <summary>
        /// Get the requested service from the specified <see cref="IServiceScope"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetScopedService<T>(IServiceScope scope) where T : class
        {
            Ensure.ArgumentNotNull(scope, nameof(scope));
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Get the requested service from the default <see cref="IServiceScope"/> provided by Breakdance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetScopedService<T>() where T : class
        {
            DefaultScope ??= TestHost.Services.CreateScope();
            return GetScopedService<T>(DefaultScope);
        }

        /// <summary>
        /// Get the requested service from the specified <see cref="IServiceScope"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetScopedServices<T>(IServiceScope scope) where T : class
        {
            Ensure.ArgumentNotNull(scope, nameof(scope));
            return scope.ServiceProvider.GetServices<T>();
        }

        /// <summary>
        /// Get the requested service from the default <see cref="IServiceScope"/> provided by Breakdance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetScopedServices<T>() where T : class
        {
            DefaultScope ??= TestHost.Services.CreateScope();
            return GetScopedServices<T>(DefaultScope);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Makes sure that we always have a working Host.
        /// </summary>
        internal void EnsureTestHost()
        {
            TestHost ??= TestHostBuilder.Build();
        }

        /// <summary>
        /// Removes references to all <see cref="BreakdanceTestBase"/> resources.
        /// </summary>
        /// <param name="disposing">Whether or not we are actively disposing of resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    TestHost?.Dispose();
                }

                TestHostBuilder = null;
                TestHost = null;
                disposedValue = true;
            }
        }

        #endregion

    }

}
