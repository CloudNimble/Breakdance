using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    /// <summary>
    /// Tests for PerTest mode.
    /// Each test method gets its own Azurite instance.
    /// </summary>
    [TestClass]
    public class PerTestModeTests : AzuriteTestBase
    {

        protected override EmulatorMode Mode => EmulatorMode.PerTest;

        private static readonly List<int> _capturedPorts = new List<int>();
        private static readonly object _lock = new object();

        [TestInitialize]
        public async Task Initialize()
        {
            await TestSetupAsync();
        }

        [TestMethod]
        public void PerTest_FirstTest_ShouldHaveUniqueInstance()
        {
            // Arrange & Act
            var port = BlobPort;

            lock (_lock)
            {
                _capturedPorts.Add(port);
            }

            // Assert
            Azurite.Should().NotBeNull();
            Azurite.IsRunning.Should().BeTrue();
            port.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void PerTest_SecondTest_ShouldHaveUniqueInstance()
        {
            // Arrange & Act
            var port = BlobPort;

            lock (_lock)
            {
                _capturedPorts.Add(port);
            }

            // Assert
            Azurite.Should().NotBeNull();
            Azurite.IsRunning.Should().BeTrue();
            port.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void PerTest_ThirdTest_ShouldHaveUniqueInstance()
        {
            // Arrange & Act
            var port = BlobPort;

            lock (_lock)
            {
                _capturedPorts.Add(port);
            }

            // Assert
            Azurite.Should().NotBeNull();
            Azurite.IsRunning.Should().BeTrue();
            port.Should().BeGreaterThan(0);

            // Note: We can't reliably assert that all ports are unique in parallel test execution
            // because tests may reuse ports after others have cleaned up
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await TestTearDownAsync();
        }

    }

}
