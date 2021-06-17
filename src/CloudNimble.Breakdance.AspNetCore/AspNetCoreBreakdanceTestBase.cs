using CloudNimble.Breakdance.Assemblies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.TestHost.HostBuilderTestServerExtensions;

namespace CloudNimble.Breakdance.AspNetCore
{

    /// <summary>
    /// A base class for building unit tests for AspNetCore APIs that automatically maintains a <see cref="TestServer"/> with configuration and a Dependency Injection containers for you.
    /// </summary>
    public class AspNetCoreBreakdanceTestBase : BreakdanceTestBase
    {
        #region Properties

        /// <summary>
        /// The <see cref="TestServer"/> for handling requests.
        /// </summary>
        public TestServer TestServer { get; internal set; }

        /// <summary>
        /// A preconfigured <see cref="HttpClient"/> for communicating with the <see cref="TestServer"/>.
        /// </summary>
        public HttpClient TestClient { get; internal set; }

        /// <summary>
        /// An <see cref="Action{IServiceCollection}"/> that lets you register additional services with the <see cref="TestServer"/>.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public Action<IServiceCollection> RegisterServices { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="AspNetCoreBreakdanceTestBase"/> instance.
        /// </summary>
        public AspNetCoreBreakdanceTestBase()
        {
            // configure the TestHostBuilder in the base class to create a WebHost
            TestHostBuilder = new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.UseTestServer()
                           .ConfigureServices(services =>
                           {
                               if (RegisterServices != null)
                               {
                                   RegisterServices.Invoke(services);
                               }
                           })
                           .Configure(app =>
                            {
                                // JHC TODO: should we provide an Action<IApplicationBuilder> propertyas well and invoke it here??
                            });
                });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        public T GetService<T>() where T : class => TestServer?.Services.GetService<T>();

        /// <summary>
        /// Get an enumeration of services of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
        public IEnumerable<T> GetServices<T>() where T : class => TestServer?.Services.GetServices<T>();

        /// <summary>
        /// Method used by test assemblies to setup the environment.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [AssemblyInitialize].
        /// With NUnit, use [OneTimeSetup].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public override void AssemblySetup()
        {
            base.AssemblySetup();
            EnsureTestServer();
        }

        /// <summary>
        /// Method used by test classes to setup the environment.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [TestInitialize].
        /// With NUnit, use [Setup].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public override void TestSetup()
        {
            base.TestSetup();
            EnsureTestServer();
        }

        /// <summary>
        /// Method used by test classes to clean up the environment.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [TestCleanup].
        /// With NUnit, use [TearDown].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public override void TestTearDown()
        {
            base.TestTearDown();
            TestServer?.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Properly instantiates the <see cref="TestServer"/> and if <see cref="RegisterServices"/> is not null, properly registers additional services with the context.
        /// </summary>
        internal void EnsureTestServer()
        {
            if (TestServer == null)
            {
                TestHost.Start();
                TestServer = TestHost.GetTestServer();
                TestClient = TestServer.CreateClient();
            }
        }

        #endregion

    }
}
