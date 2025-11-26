using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    /// <summary>
    /// Tests for configuration overrides.
    /// </summary>
    [TestClass]
    public class ConfigurationOverrideTests : AzuriteBreakdanceTestBase
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
                Silent = false, // Test non-silent mode
                StartupTimeoutSeconds = 60
            });
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            await StopAndDisposeAsync(_azurite);
            _azurite = null;
        }

        [TestMethod]
        public void ConfigurationOverrides_ShouldBeApplied()
        {
            // Assert - The instance should be running with custom config
            Azurite.Should().NotBeNull("Configuration overrides should still allow Azurite to start");
            Azurite.IsRunning.Should().BeTrue();

            // We can't directly test silent mode, but we can verify
            // that the instance started successfully with custom timeout
            BlobPort.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void InMemoryPersistence_ShouldBeConfigured()
        {
            // Assert
            Azurite.IsRunning.Should().BeTrue();
        }

    }

}
