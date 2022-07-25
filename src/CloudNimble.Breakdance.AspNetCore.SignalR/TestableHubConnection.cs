using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.AspNetCore.SignalR
{

    /// <summary>
    /// A class for emulating a SignalR <see cref="HubConnection"/> with minimal functionality.
    /// </summary>
    public class TestableHubConnection : HubConnection
    {

        /// <summary>
        /// Indicates the state of the <see cref="HubConnection"/> to the server.
        /// </summary>
        public new HubConnectionState State { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestableHubConnection"/> class.
        /// </summary>
        public TestableHubConnection(IConnectionFactory connectionFactory, IHubProtocol hubProtocol, EndPoint endPoint, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
            : base(connectionFactory, hubProtocol, endPoint, serviceProvider, loggerFactory)
        {
            State = HubConnectionState.Disconnected;
        }

        /// <summary>
        /// Starts a connection to the server.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous start.</returns>
        public async override Task StartAsync(CancellationToken cancellationToken = default)
        {
            State = HubConnectionState.Connected;
            await Task.CompletedTask;
        }

        /*
            JHC Note:  I copied some of the base implementations for the HubConnection here as an example.
                       We need to find a good pattern for overriding methods to support tests for both the client
                       to monitor events on the hub and also to invoke signals on the hub.

        // If the registered callback blocks it can cause the client to stop receiving messages. If you need to block, get off the current thread first.
        /// <summary>
        /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
        /// </summary>
        /// <param name="methodName">The name of the hub method to define.</param>
        /// <param name="parameterTypes">The parameters types expected by the hub method.</param>
        /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
        /// <param name="state">A state object that will be passed to the handler.</param>
        /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
        /// <remarks>
        /// This is a low level method for registering a handler. Using an <see cref="HubConnectionExtensions"/> <c>On</c> extension method is recommended.
        /// </remarks>
        public override IDisposable On(string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state)
        {
        }

        /// <summary>
        /// Invokes a hub method on the server using the specified method name and arguments.
        /// </summary>
        /// <param name="methodName">The name of the server method to invoke.</param>
        /// <param name="args"></param>
        public async Task InvokeAsync(string methodName, params object[] args)
        {
            await Task.CompletedTask;
        }
        */

    }

}