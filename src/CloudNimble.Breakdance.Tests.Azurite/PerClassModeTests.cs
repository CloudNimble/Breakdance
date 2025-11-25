using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    /// <summary>
    /// Tests for PerClass mode.
    /// Each test class gets its own Azurite instance.
    /// </summary>
    [TestClass]
    public class PerClassModeTests : AzuriteTestBase
    {

        protected override EmulatorMode Mode => EmulatorMode.PerClass;

        private static int? _classBlobPort;

        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            // Note: ClassInitialize must be static, so we can't call instance methods
            // The instance will be created when the first test runs
            await Task.CompletedTask;
        }

        [TestInitialize]
        public async Task Initialize()
        {
            await TestSetupAsync();

            // Capture the port on first test
            if (!_classBlobPort.HasValue)
            {
                _classBlobPort = BlobPort;
            }
        }

        [TestMethod]
        public void Azurite_ShouldBeRunning()
        {
            // Assert
            Azurite.Should().NotBeNull();
            Azurite.IsRunning.Should().BeTrue();
        }

        [TestMethod]
        public void PerClassInstance_ShouldBeConsistentAcrossTests()
        {
            // Assert - Should use the same port as captured in Initialize
            BlobPort.Should().Be(_classBlobPort.Value,
                "PerClass mode should reuse the same instance for all tests in this class");
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await TestTearDownAsync();
        }

        [ClassCleanup]
        public static async Task ClassClean()
        {
            // PerClass mode will cleanup in ClassTearDownAsync
            await Task.CompletedTask;
        }

    }

}
