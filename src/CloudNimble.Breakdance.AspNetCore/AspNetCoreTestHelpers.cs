using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http;
using CloudNimble.Breakdance.Assemblies;
using Microsoft.Extensions.Hosting;

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
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.EnsureTestServer();
            return testBase.TestServer;
        }

        /// <summary>
        /// Gets a new <see cref="TestServer"/> with the provided service registration.
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static TestServer GetTestableHttpServer(Action<IServiceCollection> registration)
        {
            var testBase = new AspNetCoreBreakdanceTestBase();
            testBase.RegisterServices = registration;
            testBase.EnsureTestServer();
            return testBase.TestServer;
        }

    }
}
