using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    /// <summary>
    /// Tests for Manual mode where test author controls lifecycle.
    /// </summary>
    [TestClass]
    public class ManualModeTests : AzuriteTestBase
    {

        protected override EmulatorMode Mode => EmulatorMode.Manual;

        [TestMethod]
        public async Task Manual_StartAndStop_ShouldWork()
        {
            // Arrange - In manual mode, Azurite should NOT start automatically
            Azurite.Should().BeNull("Manual mode should not auto-start");

            // Act - Manually start
            await EnsureAzuriteAsync();

            // Assert
            Azurite.Should().NotBeNull();
            Azurite.IsRunning.Should().BeTrue();
            BlobPort.Should().BeGreaterThan(0);

            // Act - Manually stop
            await StopAzuriteAsync();

            // Assert
            Azurite.Should().BeNull("StopAzuriteAsync should null out the instance");
        }

        //[TestMethod]
        //public async Task Manual_MultipleStartStop_ShouldWork()
        //{
        //    // Arrange
        //    Azurite.Should().BeNull();

        //    // Act - Start, stop, start again
        //    await StartAzuriteAsync();
        //    var firstPort = BlobPort;
        //    await StopAzuriteAsync();

        //    await StartAzuriteAsync();
        //    var secondPort = BlobPort;

        //    // Assert
        //    firstPort.Should().BeGreaterThan(0);
        //    secondPort.Should().BeGreaterThan(0);
        //    Azurite.Should().NotBeNull();
        //    Azurite.IsRunning.Should().BeTrue();

        //    // Cleanup
        //    await StopAzuriteAsync();
        //}

    }

}
