using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;

namespace CloudNimble.Breakdance.AspNetCore.SignalR;

/// <summary>
/// Extensions for HubConnectionBuilder
/// </summary>
public static class Breakdance_SignalR_HubConnectionBuilderExtensions
{

    /// <summary>
    /// Builds testable HubConnection.
    /// </summary>
    /// <remarks>
    /// It is a normal use case for the someone to add another build method as a part of the builder pattern.
    /// </remarks>
    /// <returns></returns>
    public static TestableHubConnection BuildTestable(this IHubConnectionBuilder hubConnectionBuilder)
    {
        hubConnectionBuilder.Services.AddSingleton<TestableHubConnection>();

        hubConnectionBuilder.WithUrl("https://localhost/TestHub");

        // The service provider is disposed by the HubConnection
        var serviceProvider = hubConnectionBuilder.Services.BuildServiceProvider();

        var connectionFactory = serviceProvider.GetService<IConnectionFactory>() ??
            throw new InvalidOperationException($"Cannot create {nameof(TestableHubConnection)} instance. An {nameof(IConnectionFactory)} was not configured.");

        var endPoint = serviceProvider.GetService<EndPoint>() ??
            throw new InvalidOperationException($"Cannot create {nameof(TestableHubConnection)} instance. An {nameof(EndPoint)} was not configured.");

        return serviceProvider.GetRequiredService<TestableHubConnection>();
    }

}
