using CloudNimble.Breakdance.AspNetCore.SignalR;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.AspNetCore.SignalR
{

    /// <summary>
    /// Tests the configuration for clients using the <see cref="TestableHubConnection"/>.
    /// </summary>
    [TestClass]
    public class TestableHubConnectionTests : ClientTestBase
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
        public async Task CanConfigure()
        {
            var hubConnection = GetService<HubConnection>();
            hubConnection.Should().NotBeNull();
            hubConnection.State.Should().Be(HubConnectionState.Disconnected);
            await hubConnection.StartAsync();
            hubConnection.State.Should().Be(HubConnectionState.Connected);
        }

        /// <summary>
        /// Tests that the <see cref="TestableHubConnection"/> can register a handler.
        /// </summary>
        [TestMethod]
        public async Task CanRegisterHandler()
        {
            var hubConnection = GetService<HubConnection>();

            await hubConnection.StartAsync();

            hubConnection.On("HubMethod", () => { });

            var testabelHubConnection = (TestableHubConnection)hubConnection;

            testabelHubConnection.RegisteredHandlers.Keys.Should().Contain("HubMethod");
        }

        /// <summary>
        /// Tests that the <see cref="TestableHubConnection"/> can send.
        /// </summary>
        [TestMethod]
        public async Task CanSend()
        {
            var hubConnection = GetService<HubConnection>();

            await hubConnection.StartAsync();

            await hubConnection.SendAsync("InvokableMethod");

            var testabelHubConnection = (TestableHubConnection)hubConnection;

            testabelHubConnection.InvokedMethods.Keys.Should().Contain("InvokableMethod");
        }

        /// <summary>
        /// Tests that the <see cref="TestableHubConnection"/> can invoke.
        /// </summary>
        [TestMethod]
        public async Task CanInvoke()
        {
            var hubConnection = GetService<HubConnection>();

            await hubConnection.StartAsync();

            await hubConnection.InvokeAsync("InvokableMethod1");
            await hubConnection.InvokeAsync<int>("InvokableMethod2");
            await hubConnection.InvokeAsync<SimpleTestPOCO>("InvokableMethod3");

            var testabelHubConnection = (TestableHubConnection)hubConnection;

            testabelHubConnection.InvokedMethods.Keys.Should().Contain("InvokableMethod1");
            testabelHubConnection.InvokedMethods.Keys.Should().Contain("InvokableMethod2");
            testabelHubConnection.InvokedMethods.Keys.Should().Contain("InvokableMethod3");
        }
        private class SimpleTestPOCO
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }
    }

}
