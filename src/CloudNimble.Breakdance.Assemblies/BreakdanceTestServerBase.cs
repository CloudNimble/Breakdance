using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// A base class for unit tests that maintains an <see cref="IHost"/> with configuration and a Dependency Injection container.
    /// </summary>
    public abstract class BreakdanceTestServerBase : IDisposable
    {
        private bool disposedValue;

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public TestServer TestServer { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public IWebHostBuilder TestWebHostBuilder { get; internal set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="BreakdanceTestServerBase"/> instance. 
        /// </summary>
        public BreakdanceTestServerBase()
        {
            EnsureTestServer();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// With MSTest, use [AssemblyInitialize].
        /// With NUnit, use [OneTimeSetup].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public virtual void AssemblySetup()
        {
            EnsureTestServer();
        }

        /// <summary>
        /// Disposes of the TestServer
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
        /// 
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// With MSTest, use [TestInitialize].
        /// With NUnit, use [Setup].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public virtual void TestSetup()
        {
            EnsureTestServer();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// 
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
        internal void EnsureTestServer()
        {
            if (TestServer == null)
            {
                using var host = new HostBuilder()
                    .ConfigureWebHost(builder =>
                    {
                        builder.UseTestServer()
                           .ConfigureServices(services =>
                           {
                               // JHC TODO:
                           })
                           .Configure(app =>
                           {
                               // JHC TODO:
                           });
                    })
                    .Build();
                
                host.Start();

                TestServer = host.GetTestServer();
            }
        }

        /// <summary>
        /// Removes references to all <see cref="BreakdanceTestServerBase"/> resources.
        /// </summary>
        /// <param name="disposing">Whether or not we are actively disposing of resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    TestServer?.Dispose();
                }

                TestWebHostBuilder = null;
                TestServer = null;
                disposedValue = true;
            }
        }

        #endregion

    }

}
