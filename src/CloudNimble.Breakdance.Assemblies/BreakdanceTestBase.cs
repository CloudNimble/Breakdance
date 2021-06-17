using Microsoft.Extensions.Hosting;
using System;

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

        #endregion

        #region Private Methods

        /// <summary>
        /// Makes sure that we always have a working Host.
        /// </summary>
        internal void EnsureTestHost()
        {
            if (TestHost == null)
            {
                TestHost = TestHostBuilder.Build();
            }
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
