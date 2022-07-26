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

        /// <summary>
        /// Tests that the <see cref="TestableHubConnection"/> can invoke a handler that had been registered for a methodName.
        /// </summary>
        [TestMethod]
        public async Task CanInvokeHandler()
        {
            var hubConnection = GetService<HubConnection>();

            await hubConnection.StartAsync();

            var result = "not set";

            hubConnection.On("HubMethod1", () => { result = "1"; });
            hubConnection.On("HubMethod2", (bool b) => { result = "2"; });
            hubConnection.On("HubMethod3", (string s, bool b) => { result = "3"; });

            var testabelHubConnection = (TestableHubConnection)hubConnection;

            await testabelHubConnection.InvokeHandlerFromHubAsync("HubMethod1");

            result.Should().Be("1");

            await testabelHubConnection.InvokeHandlerFromHubAsync("HubMethod2", true);

            result.Should().Be("2");

            await testabelHubConnection.InvokeHandlerFromHubAsync("HubMethod3", "test", true);

            result.Should().Be("3");
        }

        /// <summary>
        /// Tests that the <see cref="TestableHubConnection"/> throws exception
        /// if we try to invoke a method which has no handler from the hub.
        /// </summary>
        [TestMethod]
        public async Task HandlerNotRegistered_ThrowsException()
        {
            var hubConnection = GetService<HubConnection>();

            await hubConnection.StartAsync();

            var testabelHubConnection = (TestableHubConnection)hubConnection;

            var action = async () => await testabelHubConnection.InvokeHandlerFromHubAsync("HubMethod");

            await action.Should().ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(
                    "The methodName 'HubMethod' has not been registered",
                    null);
        }

        /// <summary>
        /// Tests that the <see cref="TestableHubConnection"/> throws exception
        /// if we try to invoke a method with wrong number of arguments from the hub.
        /// </summary>
        [TestMethod]
        public async Task WrongArguments_ThrowsException()
        {
            var hubConnection = GetService<HubConnection>();

            await hubConnection.StartAsync();

            hubConnection.On("HubMethod", () => { });

            var testabelHubConnection = (TestableHubConnection)hubConnection;

            var action = async () => await testabelHubConnection.InvokeHandlerFromHubAsync("HubMethod", true);

            await action.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage(
                    "Tried to invoke methodName 'HubMethod' from the hub with the wrong number of arguments. Expected 0 but got 1",
                    null);
        }
    }

}
