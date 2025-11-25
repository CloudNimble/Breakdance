using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    /// <summary>
    /// Tests for selective service startup (Blob, Queue, Table).
    /// </summary>
    [TestClass]
    public class BlobOnlyServiceTests : AzuriteTestBase
    {

        protected override AzuriteServiceType Services => AzuriteServiceType.Blob;
        protected override EmulatorMode Mode => EmulatorMode.PerClass;

        [TestInitialize]
        public async Task Initialize()
        {
            await TestSetupAsync();
        }

        [TestMethod]
        public void BlobOnly_ShouldOnlyStartBlobService()
        {
            // Assert
            BlobPort.Should().BeGreaterThan(0);
            QueuePort.Should().Be(0);
            TablePort.Should().Be(0);

            BlobEndpoint.Should().NotBeNullOrEmpty();
            QueueEndpoint.Should().BeNull();
            TableEndpoint.Should().BeNull();
        }

        [TestMethod]
        public void BlobOnly_ConnectionString_ShouldOnlyContainBlob()
        {
            // Arrange & Act
            var connectionString = ConnectionString;

            // Assert
            connectionString.Should().Contain("BlobEndpoint");
            connectionString.Should().NotContain("QueueEndpoint");
            connectionString.Should().NotContain("TableEndpoint");
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await TestTearDownAsync();
        }

    }

    [TestClass]
    public class QueueOnlyServiceTests : AzuriteTestBase
    {

        protected override AzuriteServiceType Services => AzuriteServiceType.Queue;
        protected override EmulatorMode Mode => EmulatorMode.PerClass;

        [TestInitialize]
        public async Task Initialize()
        {
            await TestSetupAsync();
        }

        [TestMethod]
        public void QueueOnly_ShouldOnlyStartQueueService()
        {
            // Assert
            BlobPort.Should().Be(0);
            QueuePort.Should().BeGreaterThan(0);
            TablePort.Should().Be(0);

            BlobEndpoint.Should().BeNull();
            QueueEndpoint.Should().NotBeNullOrEmpty();
            TableEndpoint.Should().BeNull();
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await TestTearDownAsync();
        }

    }

    [TestClass]
    public class TableOnlyServiceTests : AzuriteTestBase
    {

        protected override AzuriteServiceType Services => AzuriteServiceType.Table;
        protected override EmulatorMode Mode => EmulatorMode.PerClass;

        [TestInitialize]
        public async Task Initialize()
        {
            await TestSetupAsync();
        }

        [TestMethod]
        public void TableOnly_ShouldOnlyStartTableService()
        {
            // Assert
            BlobPort.Should().Be(0);
            QueuePort.Should().Be(0);
            TablePort.Should().BeGreaterThan(0);

            BlobEndpoint.Should().BeNull();
            QueueEndpoint.Should().BeNull();
            TableEndpoint.Should().NotBeNullOrEmpty();
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await TestTearDownAsync();
        }

    }

}
