using CloudNimble.Breakdance.AspNetCore.SignalR;
using CloudNimble.Breakdance.Assemblies;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CloudNimble.Breakdance.Tests.AspNetCore.SignalR
{

    /// <summary>
    /// A base class for configuring SignalR clients using the <see cref="TestableHubConnection"/>.
    /// </summary>
    public class ClientTestBase : BreakdanceTestBase
    {

        /// <summary>
        /// Constructs a new instance with common configuration for all tests.
        /// </summary>
        public ClientTestBase()
        {
            // configure services for the TestServer
            TestHostBuilder.ConfigureServices((builder, services) => 
            {
                // create a testable hub connection
                services.AddSingleton(serviceProviders => 
                {
                    return new TestableHubConnectionBuilder()
                        //JHC Note: we need to enable this line, but it is creating a build error right now
                        //          and I'm not sure why.  The extension should exist.
                        //.WithUrl(new Uri("http://localhost/Hub"))
                        .WithAutomaticReconnect()
                        .Build();
                });

            });

        }

    }

}