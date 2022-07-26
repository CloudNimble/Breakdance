using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;

namespace CloudNimble.Breakdance.AspNetCore.SignalR;

/// <summary>
/// Extensions for HubConnectionBuilder
/// </summary>
public static class HubConnectionBuilderExtensions
{
    /// <summary>
    /// Builds testable HubConnection.
    /// </summary>
    /// <remarks>
    /// It is a normal use case for the someone to add another build method as a part of the builder pattern.
    /// </remarks>
    /// <returns></returns>
    public static TestableNamedHubConnection BuildTestable(this IHubConnectionBuilder hubConnectionBuilder)
    {
        hubConnectionBuilder.Services.AddSingleton<TestableNamedHubConnection>();

        hubConnectionBuilder.WithUrl(SignalRConstants.HubUrl);

        // The service provider is disposed by the HubConnection
        var serviceProvider = hubConnectionBuilder.Services.BuildServiceProvider();

        var connectionFactory = serviceProvider.GetService<IConnectionFactory>() ??
            throw new InvalidOperationException($"Cannot create {nameof(TestableNamedHubConnection)} instance. An {nameof(IConnectionFactory)} was not configured.");

        var endPoint = serviceProvider.GetService<EndPoint>() ??
            throw new InvalidOperationException($"Cannot create {nameof(TestableNamedHubConnection)} instance. An {nameof(EndPoint)} was not configured.");

        return serviceProvider.GetRequiredService<TestableNamedHubConnection>();
    }
}
