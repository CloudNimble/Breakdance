using CloudNimble.Breakdance.AspNetCore.SignalR;
using CloudNimble.Breakdance.Assemblies;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

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
                services.AddSingleton<HubConnection>(new HubConnectionBuilder().BuildTestable());
            });
        }

    }

}