using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.Tests.Assemblies.SampleApis;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CloudNimble.Breakdance.Tests.Assemblies
{

    [TestClass]
    public class PublicApiHelpersTests
    {

        [TestMethod]
        public void PublicApiHelpers_GenerateMemberDefinitions_SomeStaticClass()
        {
            var type = typeof(SomeStaticClass);
            var classResult = PublicApiHelpers.GenerateMemberDefinitions(type);

            classResult.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void PublicApiHelpers_GenerateTypeDefinitionsForAssembly_Breakdance()
        {
            var definitions = PublicApiHelpers.GenerateTypeDefinitionsForAssembly("CloudNimble.Breakdance.Assemblies.dll");
            definitions.Should().NotBeNullOrEmpty();
            definitions.Should().HaveCount(15);
        }

        [TestMethod]
        public void PublicApiHelpers_GetPublicApiSurfaceReport_Breakdance()
        {
            var report = PublicApiHelpers.GetPublicApiSurfaceReport("CloudNimble.Breakdance.Assemblies.dll");
            report.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void PublicApiHelpers_GetPublicApiSurfaceReport_FluentAssertions()
        {
            var report = PublicApiHelpers.GetPublicApiSurfaceReport("..\\FluentAssertions.dll");
            report.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void PublicApiHelpers_GetPublicApiSurfaceReport_UnknownAssembly()
        {
            var report = PublicApiHelpers.GetPublicApiSurfaceReport("Azkaban.dll");
            report.Should().BeNullOrWhiteSpace();
        }

        [BreakdanceManifestGenerator]
        public void WritePublicApiManifest(string path)
        {
            var report = PublicApiHelpers.GetPublicApiSurfaceReport("CloudNimble.Breakdance.Assemblies.dll");
            var fullPath = Path.Combine(path, "Baselines//CloudNimble.Breakdance.Assemblies.txt");
            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, report);
        }



    }

}
