using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    /// <summary>
    /// Tests for SharedPerAssembly mode (default mode).
    /// All tests in this class should share the same Azurite instance.
    /// </summary>
    [TestClass]
    public class SharedPerAssemblyModeTests : AzuriteTestBase
    {

        private static int? _sharedBlobPort;

        [TestInitialize]
        public async Task Initialize()
        {
            await TestSetupAsync();
        }

        [TestMethod]
        public void Azurite_ShouldBeRunning()
        {
            // Assert
            Azurite.Should().NotBeNull("Ensure pattern should have started Azurite");
            Azurite.IsRunning.Should().BeTrue();
        }

        [TestMethod]
        public void AllServices_ShouldBeStarted()
        {
            // Assert - Default is all services
            BlobPort.Should().BeGreaterThan(0);
            QueuePort.Should().BeGreaterThan(0);
            TablePort.Should().BeGreaterThan(0);

            BlobEndpoint.Should().NotBeNullOrEmpty();
            QueueEndpoint.Should().NotBeNullOrEmpty();
            TableEndpoint.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void ConnectionString_ShouldBeValid()
        {
            // Arrange & Act
            var connectionString = ConnectionString;

            // Assert
            connectionString.Should().NotBeNullOrEmpty();
            connectionString.Should().Contain("DefaultEndpointsProtocol=http");
            connectionString.Should().Contain("AccountName=devstoreaccount1");
            connectionString.Should().Contain("BlobEndpoint");
            connectionString.Should().Contain("QueueEndpoint");
            connectionString.Should().Contain("TableEndpoint");
        }

        [TestMethod]
        public void SharedInstance_FirstTest_ShouldCapturePort()
        {
            // Arrange & Act - Capture the port for later tests
            _sharedBlobPort = BlobPort;

            // Assert
            _sharedBlobPort.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void SharedInstance_SecondTest_ShouldUseSamePort()
        {
            // This test may run before FirstTest in parallel mode,
            // so we need to handle both cases
            if (_sharedBlobPort.HasValue)
            {
                // Assert - If we captured a port, it should match
                BlobPort.Should().Be(_sharedBlobPort.Value,
                    "SharedPerAssembly mode should reuse the same instance across tests");
            }
            else
            {
                // First time seeing this - just verify it's running
                BlobPort.Should().BeGreaterThan(0);
            }
        }

        [TestMethod]
        public void Mode_ShouldBeSharedPerAssembly()
        {
            // Assert
            Mode.Should().Be(EmulatorMode.SharedPerAssembly);
        }

    }

}
