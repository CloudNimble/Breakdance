using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Microsoft.Extensions.Hosting
{

    /// <summary>
    /// Extension methods for <see cref="IHostBuilder"/> to enable seamless migration from
    /// IWebHostBuilder-based testing to IHostBuilder-based testing in Breakdance.
    /// </summary>
    /// <remarks>
    /// Note: ConfigureServices(Action&lt;IServiceCollection&gt;) is already provided by Microsoft.Extensions.Hosting.HostingHostBuilderExtensions.
    /// These extension methods add Configure() and UseStartup&lt;T&gt;() which are web-specific and not available on IHostBuilder by default.
    /// </remarks>
    public static class BreakdanceHostBuilderExtensions
    {

        /// <summary>
        /// Configures the application request pipeline for the web host.
        /// This is a convenience wrapper around ConfigureWebHost for Breakdance test scenarios.
        /// </summary>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <param name="configure">The delegate to configure the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder Configure(this IHostBuilder builder, Action<IApplicationBuilder> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            return builder.ConfigureWebHost(webBuilder => webBuilder.Configure(configure));
        }

        /// <summary>
        /// Specifies the startup type to be used by the web host.
        /// This is a convenience wrapper around ConfigureWebHost for Breakdance test scenarios.
        /// </summary>
        /// <typeparam name="TStartup">The startup type.</typeparam>
        /// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
        /// <returns>The <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder UseStartup<TStartup>(this IHostBuilder builder) where TStartup : class
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return builder.ConfigureWebHost(webBuilder => webBuilder.UseStartup<TStartup>());
        }

    }

}
