using Humanizer;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
        public Dictionary<string, List<InvocationHandle>> RegisteredHandlers { get; set; } = new();

#nullable enable
        /// <summary>
        /// Dictionary of the method names that have been invoked and their arguments.
        /// </summary>
        public Dictionary<string, object?[]> InvokedMethods { get; set; } = new();
#nullable disable

        #endregion

        #region Constructors

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

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public async override Task StartAsync(CancellationToken cancellationToken = default)
        {
            _state.GetType()
                .GetProperty("OverallState", BindingFlags.Public | BindingFlags.Instance)
                .SetValue(_state, HubConnectionState.Connected, null);

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public override IDisposable On(string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state)
        {
            if (RegisteredHandlers.ContainsKey(methodName))
            {
                RegisteredHandlers[methodName].Add(new(parameterTypes, handler, state));
            }
            else
            {
                RegisteredHandlers.Add(methodName, new() { new(parameterTypes, handler, state) });
            }
            return default;
        }

#nullable enable
        /// <inheritdoc />
        public async override Task SendCoreAsync(string methodName, object?[] args, CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Method added to test activating a handler that has been registered with a specific set of arguments
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public async Task InvokeHandlerFromHubAsync(string methodName, params object?[] args)
        {
            if (!RegisteredHandlers.ContainsKey(methodName))
            {
                throw new InvalidOperationException($"The {nameof(methodName)} '{methodName}' has not been registered");
            }

            var invocationHandlers = RegisteredHandlers[methodName];

            var validInvocationHandlers = invocationHandlers.Where(h => h.ParameterTypes.Length == args?.Length);

            if (validInvocationHandlers.Count() is 0)
            {
                throw new InvalidOperationException(
                    $"Tried to invoke {nameof(methodName)} '{methodName}' from the hub with the wrong number of arguments. " +
                    $"Expected {invocationHandlers.Select(h => h.ParameterTypes.Length).Humanize("or")} but got {(args is not null ? args.Length : 0)}"
                    );
            }
            else
            {
                foreach(var invocationHandler in validInvocationHandlers)
                {
                    await invocationHandler.Handler.Invoke(args, invocationHandler.State);
                }
            }
        }

#nullable disable

        #endregion

    }

}