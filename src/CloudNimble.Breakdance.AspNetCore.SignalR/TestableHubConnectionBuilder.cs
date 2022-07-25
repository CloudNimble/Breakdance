using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;

namespace CloudNimble.Breakdance.AspNetCore.SignalR
{

    /// <summary>
    /// Assembles the required services for constructor injection on a <see cref="TestableHubConnection"/>.
    /// </summary>
    public class TestableHubConnectionBuilder : IHubConnectionBuilder
    {
        private bool _hubConnectionBuilt;

        /// <summary>
        /// A local <see cref="IServiceCollection"/> instance for assembling dependencies.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestableHubConnectionBuilder"/> class.
        /// </summary>
        public TestableHubConnectionBuilder()
        {
            Services = new ServiceCollection();
            Services.AddSingleton<TestableHubConnection>();
            Services.AddLogging();
            this.AddJsonProtocol();
        }

        /// <summary>
        /// Creates a new <see cref="TestableHubConnection"/> with the required dependencies.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public HubConnection Build()
        {
            // Build can only be used once
            if (_hubConnectionBuilt)
            {
                throw new InvalidOperationException("TestableHubConnectionBuilder allows creation only of a single instance of TestableHubConnection.");
            }

            _hubConnectionBuilt = true;

            // The service provider is disposed by the HubConnection
            var serviceProvider = Services.BuildServiceProvider();

            var connectionFactory = serviceProvider.GetService<IConnectionFactory>() ??
                throw new InvalidOperationException($"Cannot create {nameof(TestableHubConnectionBuilder)} instance. An {nameof(IConnectionFactory)} was not configured.");

            var endPoint = serviceProvider.GetService<EndPoint>() ??
                throw new InvalidOperationException($"Cannot create {nameof(TestableHubConnectionBuilder)} instance. An {nameof(EndPoint)} was not configured.");

            return serviceProvider.GetRequiredService<TestableHubConnection>();
        }

    }

}
