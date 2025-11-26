using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{
    /// <summary>
    /// Tests for Blob-only service startup.
    /// </summary>
    [TestClass]
    public class BlobOnlyServiceTests : AzuriteBreakdanceTestBase
    {
        private static AzuriteInstance _azurite;

        protected override AzuriteInstance Azurite => _azurite;

        [ClassInitialize]
        public static async Task ClassInit(TestContext ctx)
        {
            _azurite = await CreateAndStartInstanceAsync(new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                InMemoryPersistence = true,
                Silent = true
            });
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            await StopAndDisposeAsync(_azurite);
            _azurite = null;
        }

        [TestMethod]
        public void BlobOnly_ShouldOnlyStartBlobService()
        {
            // Assert
            BlobPort.Should().BeGreaterThan(0);
            QueuePort.Should().BeNull();
            TablePort.Should().BeNull();

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

    }

}
