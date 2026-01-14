using CloudNimble.Breakdance.DotHttp.Generator;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="DotHttpConfig"/> class.
    /// </summary>
    [TestClass]
    public class DotHttpConfigTests
    {

        #region Constructor Tests

        [TestMethod]
        public void DotHttpConfig_DefaultConstructor_HasExpectedDefaults()
        {
            var config = new DotHttpConfig();

            config.BasePath.Should().BeNull();
            config.CheckBodyForErrors.Should().BeTrue();
            config.CheckContentType.Should().BeTrue();
            config.CheckStatusCode.Should().BeTrue();
            config.Environment.Should().Be("dev");
            config.HttpClientType.Should().BeNull();
            config.LogResponseOnFailure.Should().BeTrue();
            config.Namespace.Should().BeNull();
            config.TestFramework.Should().Be("MSTest");
            config.UseFluentAssertions.Should().BeTrue();
        }

        #endregion

        #region BasePath Property Tests

        [TestMethod]
        public void BasePath_CanBeSet()
        {
            var config = new DotHttpConfig { BasePath = "HttpRequests" };

            config.BasePath.Should().Be("HttpRequests");
        }

        #endregion

        #region CheckBodyForErrors Property Tests

        [TestMethod]
        public void CheckBodyForErrors_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { CheckBodyForErrors = false };

            config.CheckBodyForErrors.Should().BeFalse();
        }

        #endregion

        #region CheckContentType Property Tests

        [TestMethod]
        public void CheckContentType_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { CheckContentType = false };

            config.CheckContentType.Should().BeFalse();
        }

        #endregion

        #region CheckStatusCode Property Tests

        [TestMethod]
        public void CheckStatusCode_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { CheckStatusCode = false };

            config.CheckStatusCode.Should().BeFalse();
        }

        #endregion

        #region Environment Property Tests

        [TestMethod]
        public void Environment_CanBeSet()
        {
            var config = new DotHttpConfig { Environment = "prod" };

            config.Environment.Should().Be("prod");
        }

        #endregion

        #region HttpClientType Property Tests

        [TestMethod]
        public void HttpClientType_CanBeSet()
        {
            var config = new DotHttpConfig { HttpClientType = "MyProject.CustomHttpClient" };

            config.HttpClientType.Should().Be("MyProject.CustomHttpClient");
        }

        #endregion

        #region LogResponseOnFailure Property Tests

        [TestMethod]
        public void LogResponseOnFailure_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { LogResponseOnFailure = false };

            config.LogResponseOnFailure.Should().BeFalse();
        }

        #endregion

        #region Namespace Property Tests

        [TestMethod]
        public void Namespace_CanBeSet()
        {
            var config = new DotHttpConfig { Namespace = "MyProject.Tests" };

            config.Namespace.Should().Be("MyProject.Tests");
        }

        #endregion

        #region TestFramework Property Tests

        [TestMethod]
        public void TestFramework_CanBeSetToXUnit()
        {
            var config = new DotHttpConfig { TestFramework = "XUnit" };

            config.TestFramework.Should().Be("XUnit");
        }

        #endregion

        #region UseFluentAssertions Property Tests

        [TestMethod]
        public void UseFluentAssertions_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { UseFluentAssertions = false };

            config.UseFluentAssertions.Should().BeFalse();
        }

        #endregion

        #region Full Property Tests

        [TestMethod]
        public void AllProperties_CanBeSetTogether()
        {
            var config = new DotHttpConfig
            {
                BasePath = "Requests",
                CheckBodyForErrors = false,
                CheckContentType = false,
                CheckStatusCode = false,
                Environment = "staging",
                HttpClientType = "Custom.HttpClient",
                LogResponseOnFailure = false,
                Namespace = "Test.Namespace",
                TestFramework = "XUnit",
                UseFluentAssertions = false
            };

            config.BasePath.Should().Be("Requests");
            config.CheckBodyForErrors.Should().BeFalse();
            config.CheckContentType.Should().BeFalse();
            config.CheckStatusCode.Should().BeFalse();
            config.Environment.Should().Be("staging");
            config.HttpClientType.Should().Be("Custom.HttpClient");
            config.LogResponseOnFailure.Should().BeFalse();
            config.Namespace.Should().Be("Test.Namespace");
            config.TestFramework.Should().Be("XUnit");
            config.UseFluentAssertions.Should().BeFalse();
        }

        #endregion

    }

}
