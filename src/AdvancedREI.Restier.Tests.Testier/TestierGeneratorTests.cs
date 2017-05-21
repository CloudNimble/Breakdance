using AdvancedREI.Restier.Testier;
using AdvancedREI.Restier.Tests.Testier.Controllers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
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
        public async Task GenerateConventionDefinitions()
        {
            var model = await TestierHelpers.GetTestableModelAsync<SportsApi>();
            var result = model.GenerateConventionDefinitions();
            //TestContext.WriteLine(result);
            result.Should().NotBeEmpty();
            result.Count.Should().Be(33);
        }

        [TestMethod]
        public async Task GenerateConventionMatrix_Readable()
        {
            //var model = await TestierHelpers.GetTestableModelAsync<SportsApi>();
            //var result = model.GenerateConventionList(true);
            //TestContext.WriteLine(result);
            //result.Should().NotBeNullOrWhiteSpace();
            //result.Should().Contain("--");
        }

        [TestMethod]
        public async Task CompareReportToApi()
        {
            var api = await TestierHelpers.GetTestableApiInstance<SportsApi>();
            var result = await api.GenerateVisibilityMatrix();

            TestContext.WriteLine(result);
            result.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task WriteApiToFileSystem()
        {
            var relativePath = "..//..//";
            var api = await TestierHelpers.GetTestableApiInstance<SportsApi>();
            await api.WriteCurrentVisibilityMatrix(relativePath);

            File.Exists($"{relativePath}{api.GetType().Name}-ApiSurface.txt").Should().BeTrue();
        }

        [TestMethod]
        public async Task CompareCurrentApiReportToPriorRun()
        {
            var relativePath = "..//..//";
            var api = await TestierHelpers.GetTestableApiInstance<SportsApi>();
            var fileName = $"{relativePath}{api.GetType().Name}-ApiSurface.txt";

            File.Exists(fileName).Should().BeTrue();
            var oldReport = File.ReadAllText(fileName);
            var newReport = await api.GenerateVisibilityMatrix();
            oldReport.Should().BeEquivalentTo(newReport);
        }

    }

}