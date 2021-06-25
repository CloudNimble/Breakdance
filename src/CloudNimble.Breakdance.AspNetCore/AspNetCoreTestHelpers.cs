using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
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
        public static TestServer GetTestableHttpServer()
            => GetTestableHttpServer(null, null, null);

        /// <summary>
        /// Gets a new <see cref="TestServer"/> with the provided service registration.
        /// </summary>
        /// <param name="registration">Delegate for customizing the <see cref="IServiceCollection"/> of services available to the <see cref="TestServer"/>.</param>
        /// <returns></returns>
        public static TestServer GetTestableHttpServer(Action<IServiceCollection> registration)
            => GetTestableHttpServer(registration, null, null);

        /// <summary>
        /// Gets a new <see cref="TestServer"/> with the provided service registration and application builder.
        /// </summary>
        /// <param name="registration">Delegate for customizing the <see cref="IServiceCollection"/> of services available to the <see cref="TestServer"/>.</param>
        /// <param name="builder">Delegate for customizing the <see cref="IApplicationBuilder"></see> used to configure the <see cref="TestServer"/>.</param>
        /// <returns></returns>
        public static TestServer GetTestableHttpServer(Action<IServiceCollection> registration, Action<IApplicationBuilder> builder)
            => GetTestableHttpServer(registration, builder, null);

        /// <summary>
        /// Gets a new <see cref="TestServer"/> with the provided service registration, application builder and configuration builder.
        /// </summary>
        /// <param name="registration">Delegate for customizing the <see cref="IServiceCollection"/> of services available to the <see cref="TestServer"/>.</param>
        /// <param name="builder">Delegate for customizing the <see cref="IApplicationBuilder"></see> used to configure the <see cref="TestServer"/>.</param>
        /// <param name="configuration">Delegate for providing an <see cref="IConfigurationBuilder"/> used to generate an <see cref="IConfiguration"/> for the <see cref="TestServer"/>.</param>
        /// <returns></returns>
        public static TestServer GetTestableHttpServer(Action<IServiceCollection> registration, Action<IApplicationBuilder> builder, Action<IConfigurationBuilder> configuration)
        {
            var testBase = new AspNetCoreBreakdanceTestBase
            {
                RegisterServices = registration,
                ConfigureHost = builder,
                BuildConfiguration = configuration
            };
            testBase.EnsureTestServer();
            return testBase.TestServer;
        }

    }
}
