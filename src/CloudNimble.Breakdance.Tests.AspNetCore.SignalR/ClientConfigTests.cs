using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.AspNetCore.SignalR
{

    /// <summary>
    /// Tests the configuration for clients using the <see cref="TestableHubConnection"/>.
    /// </summary>
    [TestClass]
    public class ClientConfigTests : ClientTestBase
    {

        /// <summary>
        /// Sets up services needed for tests.
        /// </summary>
        [TestInitialize]
        public void Setup() => TestSetup();

        /// <summary>
        /// Tests that the <see cref="IServiceProvider"/> contains a valid <see cref="TestableHubConnection"/>.
        /// </summary>
        [TestMethod]
        public async Task TestHost_CanConfigure_TestableHubConnection()
        {
            var hubConnection = GetService<HubConnection>();
            hubConnection.Should().NotBeNull();
            hubConnection.State.Should().Be(HubConnectionState.Disconnected);
            await hubConnection.StartAsync();
            hubConnection.State.Should().Be(HubConnectionState.Connected);
        }

    }

}
