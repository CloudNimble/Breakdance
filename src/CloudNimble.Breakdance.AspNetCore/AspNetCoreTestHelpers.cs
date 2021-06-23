using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.AspNetCore
{

    /// <summary>
    /// Helper methods for creating testable resources for AspNetCore.
    /// </summary>
    public static class AspNetCoreTestHelpers
    {
        /// <summary>
        /// Gets a new <see cref="TestServer" /> with default services.
        /// </summary>
        public static async Task<TestServer> GetTestableHttpServer()
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            await testBase.EnsureTestServer().ConfigureAwait(false);
            return testBase.TestServer;
        }

        /// <summary>
        /// Gets a new <see cref="TestServer"/> with the provided service registration.
        /// </summary>
        /// <param name="registration">Delegate for customizing the <see cref="IServiceCollection"/> of services available to the host.</param>
        /// <returns></returns>
        public static async Task<TestServer> GetTestableHttpServer(Action<IServiceCollection> registration)
        {
            var testBase = new AspNetCoreBreakdanceTestBase
            {
                RegisterServices = registration
            };
            await testBase.EnsureTestServer().ConfigureAwait(false);
            return testBase.TestServer;
        }

        /// <summary>
        /// Gets a new <see cref="TestServer"/> with the provided service registration and application builder.
        /// </summary>
        /// <param name="registration">Delegate for customizing the <see cref="IServiceCollection"/> of services available to the host.</param>
        /// <param name="builder">Delegate for customizing the <see cref="IApplicationBuilder"></see> used to configure the host.</param>
        /// <returns></returns>
        public static async Task<TestServer> GetTestableHttpServer(Action<IServiceCollection> registration, Action<IApplicationBuilder> builder)
        {
            var testBase = new AspNetCoreBreakdanceTestBase
            {
                RegisterServices = registration,
                ConfigureHost = builder
            };
            await testBase.EnsureTestServer().ConfigureAwait(false);
            return testBase.TestServer;
        }

    }
}
