using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace CloudNimble.Breakdance.AspNetCore.SignalR
{

    /// <summary>
    /// A class for emulating a SignalR <see cref="HubConnection"/> with minimal functionality.
    /// </summary>
    public class TestableHubConnection : HubConnection
    {
        #region Private Members

        private object _state;

        #endregion

        #region Public Properties

        /// <summary>
        /// Dictionary of the method names that have been registered and their parameter types.
        /// </summary>
        public Dictionary<string, Type[]> RegisteredHandlers { get; set; } = new();

#nullable enable
        /// <summary>
        /// Dictionary of the method names that have been invoked and their arguments.
        /// </summary>
        public Dictionary<string, object?[]> InvokedMethods { get; set; } = new();
#nullable disable

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TestableHubConnection"/> class.
        /// </summary>
        public TestableHubConnection(IConnectionFactory connectionFactory,
                             IHubProtocol protocol,
                             EndPoint endPoint,
                             IServiceProvider serviceProvider,
                             ILoggerFactory loggerFactory) : base(connectionFactory, protocol, endPoint, serviceProvider, loggerFactory)
        {
            _state = typeof(HubConnection)
                .GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(this);

            _state.GetType()
                .GetProperty("OverallState", BindingFlags.Public | BindingFlags.Instance)
                .SetValue(_state, HubConnectionState.Disconnected, null);
        }

        /// <inheritdoc />
        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _state.GetType()
                .GetProperty("OverallState", BindingFlags.Public | BindingFlags.Instance)
                .SetValue(_state, HubConnectionState.Connected, null);

            await Task.CompletedTask;
        }


        /// <inheritdoc />
        public override IDisposable On(string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state)
        {
            RegisteredHandlers.Add(methodName, parameterTypes);
            return default;
        }

#nullable enable
        /// <inheritdoc />
        public override async Task SendCoreAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
        {
            InvokedMethods.Add(methodName, args);
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task<object?> InvokeCoreAsync(string methodName, Type returnType, object?[] args, CancellationToken cancellationToken = default)
        {
            InvokedMethods.Add(methodName, args);
            await Task.CompletedTask;
            if (returnType == typeof(object))
            {
                return null;
            }
            else if (returnType.IsValueType)
            {
                return Activator.CreateInstance(returnType);
            }
            return null;
        }
#nullable disable
    }
}