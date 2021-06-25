using CloudNimble.Breakdance.Assemblies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

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
        /// 
        /// </summary>
        public new IWebHostBuilder TestHostBuilder { get; internal set; }

        /// <summary>
        /// An <see cref="Action{IServiceCollection}"/> that lets you register additional services with the <see cref="TestServer"/>.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public Action<IServiceCollection> RegisterServices { get; set; }

        /// <summary>
        /// An <see cref="Action{IApplicationBuilder}"/> that lets you modify the application configuration for the <see cref="TestServer"/>.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public Action<IApplicationBuilder> ConfigureHost { get; set; }

        /// <summary>
        /// An <see cref="Action{IConfigurationBuilder}"/> that lets you specify customize the <see cref="IConfiguration"/> for the <see cref="TestServer"/>.
        /// </summary>
        public Action<IConfigurationBuilder> BuildConfiguration { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="AspNetCoreBreakdanceTestBase"/> instance.
        /// </summary>
        public AspNetCoreBreakdanceTestBase()
        {
            // replace the TestHostBuilder with one that will generate an IWebHost
            TestHostBuilder = new WebHostBuilder()
                                .Configure(appBuilder =>
                                {
                                    if (ConfigureHost != null)
                                    {
                                        ConfigureHost.Invoke(appBuilder);
                                    }
                                })
                                .ConfigureAppConfiguration(configuration =>
                                {
                                    if (BuildConfiguration != null)
                                    {
                                        BuildConfiguration.Invoke(configuration);
                                    }
                                })
                                .ConfigureServices(services =>
                                {
                                    if (RegisterServices != null)
                                    {
                                        RegisterServices.Invoke(services);
                                    }
                                });

            /*
            TestHostBuilder
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
                            if (ConfigureHost != null)
                            {
                                ConfigureHost.Invoke(app);
                            }
                        });
                });
            */
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
                // the constructor automatically calls the IWebHost.StartAsync() method
                TestServer = new TestServer(TestHostBuilder);
            }
        }

        #endregion

    }
}
