using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{
    /// <summary>
    /// Tests for memory limit configuration.
    /// </summary>
    [TestClass]
    public class MemoryLimitConfigurationTests : AzuriteTestBase
    {
        private static AzuriteInstance _azurite;

        protected override AzuriteInstance Azurite => _azurite;

        [ClassInitialize]
        public static async Task ClassInit(TestContext ctx)
        {
            _azurite = await CreateAndStartInstanceAsync(new AzuriteConfiguration
            {
                Services = AzuriteServiceType.All,
                InMemoryPersistence = true,
                Silent = true,
                ExtentMemoryLimitMB = 512
            });
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            await StopAndDisposeAsync(_azurite);
            _azurite = null;
        }

        [TestMethod]
        public void MemoryLimit_ShouldAllowStartup()
        {
            // Assert - Verify the instance starts with memory limit
            Azurite.Should().NotBeNull();
            Azurite.IsRunning.Should().BeTrue();
            BlobPort.Should().BeGreaterThan(0);
        }

    }

}
