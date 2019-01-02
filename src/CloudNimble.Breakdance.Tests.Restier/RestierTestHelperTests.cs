using CloudNimble.Breakdance.Restier;
using CloudNimble.Breakdance.Tests.Restier.Controllers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Restier
{
    [TestClass]
    public class RestierTestHelperTests
    {

        private TestContext testContextInstance;
        private const string relativePath = "..//..//";

        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [TestMethod]
        public async Task RestierTestHelpers_CheckVerboseErrors_NotFound()
        {
            var responseMessage = await RestierTestHelpers.ExecuteTestRequest<SportsApi>(HttpMethod.Get, resource: "/DoesntExist").ConfigureAwait(false);
            responseMessage.Should().NotBeNull();
            responseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var response = await responseMessage.Content.ReadAsStringAsync();
            response.Should().NotBe("{\"Message\":\"An error has occurred.\"}");
            response.Should().Be("{\"Message\":\"No HTTP resource was found that matches the request URI 'http://localhost/api/test/DoesntExist'.\",\"MessageDetail\":\"No route data was found for this request.\"}");
            TestContext.WriteLine(response);
        }

        [TestMethod]
        public async Task RestierTestHelpers_GenerateConventionDefinitions()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<SportsApi>();
            var result = model.GenerateConventionDefinitions();
            //TestContext.WriteLine(result);
            result.Should().NotBeEmpty();
            result.Count.Should().Be(33);
        }

        //[TestMethod]
        //public async Task GenerateConventionMatrix_Readable()
        //{
        //    var model = await TestierHelpers.GetTestableModelAsync<SportsApi>();
        //    var result = model.GenerateConventionList(true);
        //    TestContext.WriteLine(result);
        //    result.Should().NotBeNullOrWhiteSpace();
        //    result.Should().Contain("--");
        //}

        [TestMethod]
        public async Task RestierTestHelpers_CompareReportToApi()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<SportsApi>();
            var result = await api.GenerateVisibilityMatrix();

            TestContext.WriteLine(result);
            result.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public async Task RestierTestHelpers_WriteApiToFileSystem()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<SportsApi>();
            await api.WriteCurrentVisibilityMatrix(relativePath);

            File.Exists($"{relativePath}{api.GetType().Name}-ApiSurface.txt").Should().BeTrue();
        }

        [TestMethod]
        public async Task RestierTestHelpers_CompareCurrentApiReportToPriorRun()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<SportsApi>();
            var fileName = $"{relativePath}{api.GetType().Name}-ApiSurface.txt";

            File.Exists(fileName).Should().BeTrue();
            var oldReport = File.ReadAllText(fileName);
            var newReport = await api.GenerateVisibilityMatrix();
            oldReport.Should().BeEquivalentTo(newReport);
        }

        [TestMethod]
        public async Task RestierTestHelpers_WriteApiMetadataToFileSystem()
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<SportsApi>(relativePath);

            File.Exists($"{relativePath}{typeof(SportsApi).Name}-ApiMetadata.txt").Should().BeTrue();
        }

        [TestMethod]
        public async Task RestierTestHelpers_CompareCurrentApiMetadataToPriorRun()
        {
            var fileName = $"{relativePath}{typeof(SportsApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadata<SportsApi>();
            oldReport.Should().BeEquivalentTo(newReport);
        }

    }

}