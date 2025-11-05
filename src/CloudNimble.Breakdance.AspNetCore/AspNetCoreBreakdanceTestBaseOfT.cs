using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace CloudNimble.Breakdance.AspNetCore
{

    /// <summary>
    /// A base class for building unit tests for AspNetCore APIs that automatically maintains a <see cref="TestServer"/> with configuration and a Dependency Injection containers for you.
    /// </summary>
    /// <typeparam name="TStartup"></typeparam>
    public class AspNetCoreBreakdanceTestBase<TStartup> : AspNetCoreBreakdanceTestBase where TStartup : class
    {

        /// <summary>
        /// Creates a new <see cref="AspNetCoreBreakdanceTestBase"/> instance.
        /// </summary>
        /// <remarks>The call to .Configure() with no content is required to get a minimal, empty <see cref="IWebHost"/>.</remarks>
        public AspNetCoreBreakdanceTestBase() : base()
        {
            TestHostBuilder.UseStartup<TStartup>();
        }

    }

}
