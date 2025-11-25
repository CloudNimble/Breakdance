using CloudNimble.Breakdance.Assemblies;
using Flurl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

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
        /// Replaces the <see cref="TestHostBuilder"/> from the <see cref="BreakdanceTestBase"/> with an <see cref="IHostBuilder"/> implementation configured for web hosting.
        /// </summary>
        public new IHostBuilder TestHostBuilder { get; internal set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="AspNetCoreBreakdanceTestBase"/> instance.
        /// </summary>
        /// <remarks>Uses the modern <see cref="IHostBuilder"/> pattern for web hosting instead of the deprecated WebHostBuilder.</remarks>
        public AspNetCoreBreakdanceTestBase()
        {
            // Use the modern HostBuilder pattern instead of deprecated WebHost.CreateDefaultBuilder()
            TestHostBuilder = new HostBuilder();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds minimal MVC services to the <see cref="TestHostBuilder"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="app"></param>
        /// <remarks>
        /// Calls AddMvcCore() on the <see cref="IServiceCollection"/> which does the following, according to the Microsoft docs:
        ///    will register the minimum set of services necessary to route requests and invoke
        ///     controllers. It is not expected that any application will satisfy its requirements
        ///     with just a call to Microsoft.Extensions.DependencyInjection.MvcCoreServiceCollectionExtensions.AddMvcCore(Microsoft.Extensions.DependencyInjection.IServiceCollection).
        ///     Additional configuration using the Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder
        ///     will be required.
        /// </remarks>
        public void AddMinimalMvc(Action<MvcOptions> options = default, Action<IApplicationBuilder> app = default)
        {
            TestHostBuilder.ConfigureServices(services => {
                services.AddMvcCore(options ?? (mvcOptions => { }));
            })
            .Configure(app ?? ((IApplicationBuilder appBuilder) => {
                appBuilder.UseRouting();
                appBuilder.UseEndpoints(endpoints => endpoints.MapControllers());
            }));
        }

        /// <summary>
        /// Adds Controller services to the <see cref="TestHostBuilder"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="app"></param>
        /// <remarks>
        /// Calls AddControllers() on the <see cref="IServiceCollection"/> which does the following, according to the Microsoft docs:
        ///     combines the effects of Microsoft.Extensions.DependencyInjection.MvcCoreServiceCollectionExtensions.AddMvcCore(Microsoft.Extensions.DependencyInjection.IServiceCollection),
        ///     Microsoft.Extensions.DependencyInjection.MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.MvcCoreMvcCoreBuilderExtensions.AddAuthorization(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.MvcCorsMvcCoreBuilderExtensions.AddCors(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     and Microsoft.Extensions.DependencyInjection.MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder).
        /// </remarks>
        public void AddApis(Action<MvcOptions> options = default, Action<IApplicationBuilder> app = default)
        {
            TestHostBuilder.ConfigureServices(services => {
                services.AddControllers(options ?? (mvcoptions => { }));
            })
            .Configure(app ?? ((IApplicationBuilder appBuilder) => {
                appBuilder.UseRouting();
                appBuilder.UseEndpoints(endpoints => endpoints.MapControllers());
            }));
        }

        /// <summary>
        /// Adds support for Controllers and Razor views to the <see cref="TestHostBuilder"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="app"></param>
        /// <remarks>
        /// Calls AddControllersWithViews() on the <see cref="IServiceCollection"/> which does the following, according to the Microsoft docs:
        ///     combines the effects of Microsoft.Extensions.DependencyInjection.MvcCoreServiceCollectionExtensions.AddMvcCore(Microsoft.Extensions.DependencyInjection.IServiceCollection),
        ///     Microsoft.Extensions.DependencyInjection.MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.MvcCoreMvcCoreBuilderExtensions.AddAuthorization(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.MvcCorsMvcCoreBuilderExtensions.AddCors(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.TagHelperServicesExtensions.AddCacheTagHelper(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.MvcViewFeaturesMvcCoreBuilderExtensions.AddViews(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     and Microsoft.Extensions.DependencyInjection.MvcRazorMvcCoreBuilderExtensions.AddRazorViewEngine(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder).
        /// </remarks>
        public void AddViews(Action<MvcOptions> options = default, Action<IApplicationBuilder> app = default)
        {
            TestHostBuilder.ConfigureServices(services => {
                services.AddControllersWithViews(options ?? (mvcOptions => { }));
            })
            .Configure(app ?? ((IApplicationBuilder appBuilder) => {
                appBuilder.UseRouting();
                appBuilder.UseEndpoints(endpoints => endpoints.MapControllers());
            }));
        }

        /// <summary>
        /// Adds support for Controllers and Razor views to the <see cref="TestHostBuilder"/>.
        /// </summary>
        /// <remarks>
        /// Calls AddRazorPages() on the <see cref="IServiceCollection"/> which does the following, according to the Microsoft docs:
        ///     combines the effects of Microsoft.Extensions.DependencyInjection.MvcCoreServiceCollectionExtensions.AddMvcCore(Microsoft.Extensions.DependencyInjection.IServiceCollection),
        ///     Microsoft.Extensions.DependencyInjection.MvcCoreMvcCoreBuilderExtensions.AddAuthorization(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     Microsoft.Extensions.DependencyInjection.TagHelperServicesExtensions.AddCacheTagHelper(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder),
        ///     and Microsoft.Extensions.DependencyInjection.MvcRazorPagesMvcCoreBuilderExtensions.AddRazorPages(Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder).
        /// </remarks>
        public void AddRazorPages(Action<RazorPagesOptions> options = default, Action<IApplicationBuilder> app = default)
        {
            TestHostBuilder.ConfigureServices(services => {
                services.AddRazorPages(options ?? (razorOptions => { }));
            })
            .Configure(app ?? ((IApplicationBuilder appBuilder) => {
                appBuilder.UseRouting();
                appBuilder.UseEndpoints(endpoints => endpoints.MapControllers());
            }));
        }

        /// <summary>
        /// Retrieves an <see cref="HttpClient"/> instance from the <see cref="TestServer"/> and properly configures the <see cref="HttpClient.BaseAddress"/>.
        /// </summary>
        /// <param name="routePrefix">
        /// The string to append to the <see cref="HttpClient.BaseAddress"/> for all requests. Defaults to <see cref="WebApiConstants.RoutePrefix"/>.
        /// </param>
        /// <returns>A properly configured <see cref="HttpClient"/>instance from the <see cref="TestServer"/>.</returns>
        public HttpClient GetHttpClient(string routePrefix = WebApiConstants.RoutePrefix)
        {
            var client = TestServer.CreateClient();
            client.BaseAddress = new Uri(Url.Combine(WebApiConstants.Localhost, routePrefix));
            return client;
        }

        /// <summary>
        /// Retrieves an <see cref="HttpClient"/> instance from the <see cref="TestServer"/> and properly configures the <see cref="HttpClient.BaseAddress"/>.
        /// </summary>
        /// <param name="authHeader"></param>
        /// <param name="routePrefix">
        /// The string to append to the <see cref="HttpClient.BaseAddress"/> for all requests. Defaults to <see cref="WebApiConstants.RoutePrefix"/>.
        /// </param>
        /// <returns>A properly configured <see cref="HttpClient"/>instance from the <see cref="TestServer"/>.</returns>
        public HttpClient GetHttpClient(AuthenticationHeaderValue authHeader, string routePrefix = WebApiConstants.RoutePrefix)
        {
            if (authHeader is null)
            {
                throw new ArgumentNullException(nameof(authHeader));
            }

            var client = GetHttpClient(routePrefix);
            client.DefaultRequestHeaders.Authorization = authHeader;
            return client;
        }

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        public override T GetService<T>() where T : class => TestServer?.Services.GetService<T>() ?? base.GetService<T>();

        /// <summary>
        /// Get an enumeration of services of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>An enumeration of services of type <typeparamref name="T"/>.</returns>
        public override IEnumerable<T> GetServices<T>() where T : class => TestServer?.Services.GetServices<T>() ?? base.GetServices<T>();

#if NET8_0_OR_GREATER

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="key">The key of the service to get.</param>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        public override T GetKeyedService<T>(string key) where T : class => TestServer?.Services.GetKeyedService<T>(key) ?? base.GetKeyedService<T>(key);

        /// <summary>
        /// Get services of type <typeparamref name="T"/> from the System.IServiceProvider.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <param name="key">The </param>
        /// <returns>An <see cref="IEnumerable{T}"/> of type <typeparamref name="T"/>.</returns>
        public override IEnumerable<T> GetKeyedServices<T>(string key)  where T : class => TestServer?.Services.GetKeyedServices<T>(key) ?? base.GetKeyedServices<T>(key);

#endif

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
            EnsureTestServerAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Method used by test assemblies to setup the environment asynchronously.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [AssemblyInitialize].
        /// With NUnit, use [OneTimeSetUp].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public async override Task AssemblySetupAsync()
        {
            base.AssemblySetup();
            await EnsureTestServerAsync();
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
            EnsureTestServerAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Method used by test classes to setup the environment asynchronously.
        /// </summary>
        /// <remarks>
        /// With MSTest, use [TestInitialize].
        /// With NUnit, use [SetUp].
        /// With xUnit, good luck: https://xunit.net/docs/shared-context
        /// </remarks>
        public async override Task TestSetupAsync()
        {
            base.TestSetup();
            await EnsureTestServerAsync();
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

        #region Internal Methods

        /// <summary>
        /// Ensures that the <see cref="TestServer"/> has been constructed.
        /// </summary>
        /// <remarks>Builds the host using <see cref="IHostBuilder"/>, starts it, and retrieves the <see cref="TestServer"/> from the host services.</remarks>
        [Obsolete("Please use EnsureTestServerAsync instead.", false)]
        internal void EnsureTestServer()
        {
            EnsureTestServerAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ensures that the <see cref="TestServer"/> has been constructed asynchronously.
        /// </summary>
        /// <remarks>Builds the host using <see cref="IHostBuilder"/>, starts it, and retrieves the <see cref="TestServer"/> from the host services.</remarks>
        internal async Task EnsureTestServerAsync()
        {
            if (TestServer is null)
            {
                try
                {
                    // Configure the host to use TestServer
                    TestHostBuilder.ConfigureWebHost(webBuilder =>
                    {
                        webBuilder.UseTestServer();
                    });

                    // Build and start the host
                    var host = TestHostBuilder.Build();
                    await host.StartAsync();

                    // Get the TestServer from the host services
                    TestServer = host.GetTestServer();
                }
                catch (InvalidOperationException iox)
                {
                    throw new InvalidOperationException("You must specify a configuration before calling EnsureTestServer. Please use one of the helper methods such as AddMinimalMvc() or provide your own configuration directly on the TestHostBuilder.", iox);
                }
            }
        }

        #endregion

    }

}
