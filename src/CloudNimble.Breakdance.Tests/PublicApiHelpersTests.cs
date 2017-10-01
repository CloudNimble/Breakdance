using CloudNimble.Breakdance.Core;
using CloudNimble.Breakdance.Tests.SampleApis;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests
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
            var definitions = PublicApiHelpers.GenerateTypeDefinitionsForAssembly("CloudNimble.Breakdance.dll");
            definitions.Should().NotBeNullOrEmpty();
            definitions.Count.Should().Be(8);
        }

        [TestMethod]
        public void PublicApiHelpers_GetPublicApiSurfaceReport_Breakdance()
        {
            var report = PublicApiHelpers.GetPublicApiSurfaceReport("CloudNimble.Breakdance.dll");
            report.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void PublicApiHelpers_GetPublicApiSurfaceReport_FluentAssertions()
        {
            var report = PublicApiHelpers.GetPublicApiSurfaceReport("FluentAssertions.dll");
            report.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void PublicApiHelpers_GetPublicApiSurfaceReport_UnknownAssembly()
        {
            var report = PublicApiHelpers.GetPublicApiSurfaceReport("Azkaban.dll");
            report.Should().BeNullOrWhiteSpace();
        }


    }

}
