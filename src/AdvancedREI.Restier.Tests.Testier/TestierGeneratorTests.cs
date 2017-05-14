using AdvancedREI.Restier.Testier;
using AdvancedREI.Restier.Tests.Testier.Controllers;
using AdvancedREI.Restier.Tests.Testier.Model;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace AdvancedREI.Restier.Tests.Testier
{
    [TestClass]
    public class TestierGeneratorTests
    {

        private TestContext testContextInstance;

        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [TestMethod]
        public async Task GenerateConventionMatrix()
        {
            var modelBuilder = new TestModelBuilder();
            var model = await modelBuilder.GetModelAsync(null, default(CancellationToken));
            var result = model.GenerateConventionMatrix();
            TestContext.WriteLine(result);
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().NotContain("--");
        }

        [TestMethod]
        public async Task GenerateConventionMatrix_Readable()
        {
            var modelBuilder = new TestModelBuilder();
            var model = await modelBuilder.GetModelAsync(null, default(CancellationToken));
            var result = model.GenerateConventionMatrix(true);
            TestContext.WriteLine(result);
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Contain("--");
        }

        [TestMethod]
        public async Task CompareReportToApi()
        {
            var modelBuilder = new TestModelBuilder();
            var model = await modelBuilder.GetModelAsync(null, default(CancellationToken));
            var api = new SportsApi(null);
            var result = api.GenerateVisibilityReport(model);

            TestContext.WriteLine(result);
            result.Should().NotBeNullOrWhiteSpace();
        }

    }

}