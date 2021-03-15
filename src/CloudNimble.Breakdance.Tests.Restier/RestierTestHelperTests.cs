using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.Restier;
using CloudNimble.Breakdance.Tests.Restier.Controllers;
using CloudNimble.Breakdance.Tests.Restier.Model;
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
        private const string relativePath = "..//..//..//";

        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }


        [TestMethod]
        public async Task RestierTestHelpers_CheckVerboseErrors_NotFound()
        {
            var responseMessage = await RestierTestHelpers.ExecuteTestRequest<SportsApi, SportsDbContext>(HttpMethod.Get, resource: "/DoesntExist").ConfigureAwait(false);
            responseMessage.Should().NotBeNull();
            var response = await responseMessage.Content.ReadAsStringAsync();
            TestContext.WriteLine(response);
            responseMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);
            response.Should().Be("{\"Message\":\"No HTTP resource was found that matches the request URI 'http://localhost/api/tests/DoesntExist'.\",\"MessageDetail\":\"No route data was found for this request.\"}");
            TestContext.WriteLine(response);
        }

        [TestMethod]
        public async Task RestierTestHelpers_EntitySet_Found()
        {
            var responseMessage = await RestierTestHelpers.ExecuteTestRequest<SportsApi, SportsDbContext>(HttpMethod.Get, resource: "/Sports").ConfigureAwait(false);
            responseMessage.Should().NotBeNull();
            var response = await responseMessage.Content.ReadAsStringAsync();
            TestContext.WriteLine(response);
            responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
            TestContext.WriteLine(response);
        }

        [TestMethod]
        public async Task RestierTestHelpers_GenerateConventionDefinitions()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<SportsApi, SportsDbContext>();
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
            var api = await RestierTestHelpers.GetTestableApiInstance<SportsApi, SportsDbContext>();
            var result = await api.GenerateVisibilityMatrix();

            TestContext.WriteLine(result);
            result.Should().NotBeNullOrWhiteSpace();
        }

        [BreakdanceManifestGenerator]
        public async Task RestierTestHelpers_WriteApiToFileSystem(string projectPath)
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<SportsApi, SportsDbContext>();
            await api.WriteCurrentVisibilityMatrix(Path.Combine(projectPath, "Baselines"));
        }

        [TestMethod]
        public async Task RestierTestHelpers_CompareCurrentApiReportToPriorRun()
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<SportsApi, SportsDbContext>();
            var fileName = $"{relativePath}{api.GetType().Name}-ApiSurface.txt";

            File.Exists(fileName).Should().BeTrue();
            var oldReport = File.ReadAllText(fileName);
            var newReport = await api.GenerateVisibilityMatrix();
            oldReport.Should().BeEquivalentTo(newReport);
        }

        [BreakdanceManifestGenerator]
        public async Task RestierTestHelpers_WriteApiMetadataToFileSystem(string projectPath)
        {
            await RestierTestHelpers.WriteCurrentApiMetadata<SportsApi, SportsDbContext>(Path.Combine(projectPath, "Baselines"));
        }

        [TestMethod]
        public async Task RestierTestHelpers_CompareCurrentApiMetadataToPriorRun()
        {
            var fileName = $"{relativePath}{typeof(SportsApi).Name}-ApiMetadata.txt";
            File.Exists(fileName).Should().BeTrue();

            var oldReport = File.ReadAllText(fileName);
            var newReport = await RestierTestHelpers.GetApiMetadata<SportsApi, SportsDbContext>();
            oldReport.Should().BeEquivalentTo(newReport.ToString());
        }

    }

}