using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    /// <summary>
    /// Tests for Table-only service startup.
    /// </summary>
    [TestClass]
    public class TableOnlyServiceTests : AzuriteBreakdanceTestBase
    {

        private static AzuriteInstance _azurite;

        protected override AzuriteInstance Azurite => _azurite;

        [ClassInitialize]
        public static async Task ClassInit(TestContext ctx)
        {
            _azurite = await CreateAndStartInstanceAsync(new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Table,
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
        public void TableOnly_ShouldOnlyStartTableService()
        {
            BlobPort.Should().BeNull();
            QueuePort.Should().BeNull();
            TablePort.Should().BeGreaterThan(0);

            BlobEndpoint.Should().BeNull();
            QueueEndpoint.Should().BeNull();
            TableEndpoint.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void TableOnly_ConnectionString_ShouldOnlyContainTable()
        {
            var connectionString = ConnectionString;

            connectionString.Should().Contain("TableEndpoint");
            connectionString.Should().NotContain("BlobEndpoint");
            connectionString.Should().NotContain("QueueEndpoint");
        }

    }

}
