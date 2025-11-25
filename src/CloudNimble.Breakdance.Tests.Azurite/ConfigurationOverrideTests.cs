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
    public class ConfigurationOverrideTests : AzuriteTestBase
    {

        protected override bool SilentMode => false;
        protected override int StartupTimeoutSeconds => 60;
        protected override bool UseInMemoryPersistence => true;
        protected override EmulatorMode Mode => EmulatorMode.PerClass;

        [TestInitialize]
        public async Task Initialize()
        {
            await TestSetupAsync();
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
            UseInMemoryPersistence.Should().BeTrue();
            Azurite.IsRunning.Should().BeTrue();
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await TestTearDownAsync();
        }

    }

    /// <summary>
    /// Tests for memory limit configuration.
    /// </summary>
    [TestClass]
    public class MemoryLimitConfigurationTests : AzuriteTestBase
    {

        protected override int? ExtentMemoryLimitMB => 512;
        protected override EmulatorMode Mode => EmulatorMode.PerClass;

        [TestInitialize]
        public async Task Initialize()
        {
            await TestSetupAsync();
        }

        [TestMethod]
        public void MemoryLimit_ShouldAllowStartup()
        {
            // Assert - Verify the instance starts with memory limit
            Azurite.Should().NotBeNull();
            Azurite.IsRunning.Should().BeTrue();
            BlobPort.Should().BeGreaterThan(0);
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await TestTearDownAsync();
        }

    }

}
