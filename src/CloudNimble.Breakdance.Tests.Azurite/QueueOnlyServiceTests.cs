using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{
    /// <summary>
    /// Tests for Queue-only service startup.
    /// </summary>
    [TestClass]
    public class QueueOnlyServiceTests : AzuriteTestBase
    {
        private static AzuriteInstance _azurite;

        protected override AzuriteInstance Azurite => _azurite;

        [ClassInitialize]
        public static async Task ClassInit(TestContext ctx)
        {
            _azurite = await CreateAndStartInstanceAsync(new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Queue,
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
        public void QueueOnly_ShouldOnlyStartQueueService()
        {
            // Assert
            BlobPort.Should().BeNull();
            QueuePort.Should().BeGreaterThan(0);
            TablePort.Should().BeNull();

            BlobEndpoint.Should().BeNull();
            QueueEndpoint.Should().NotBeNullOrEmpty();
            TableEndpoint.Should().BeNull();
        }

    }

}
