using System.IO;
using System.Linq;
using System.Reflection;
using CloudNimble.Breakdance.DotHttp;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// End-to-end tests for <see cref="DotHttpFileParser"/> using real .http files.
    /// </summary>
    [TestClass]
    public class DotHttpFileParserFileTests
    {

        #region Helper Methods

        private static string GetTestFilePath(string fileName)
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(assemblyLocation, "HttpFiles", fileName);
        }

        private static string ReadTestFile(string fileName)
        {
            var filePath = GetTestFilePath(fileName);
            return File.ReadAllText(filePath);
        }

        #endregion

        #region Simple Requests Tests

        [TestMethod]
        public void Parse_SimpleRequestsFile_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("simple-requests.http");

            // Act
            var result = parser.Parse(content, "simple-requests.http");

            // Assert
            result.Should().NotBeNull();
            result.Diagnostics.Should().BeEmpty();
            result.Requests.Should().HaveCount(5);
        }

        [TestMethod]
        public void Parse_SimpleRequestsFile_ParsesVariables()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("simple-requests.http");

            // Act
            var result = parser.Parse(content, "simple-requests.http");

            // Assert
            result.Variables.Should().ContainKey("baseUrl");
            result.Variables["baseUrl"].Should().Be("https://api.example.com");
            result.Variables.Should().ContainKey("apiVersion");
            result.Variables["apiVersion"].Should().Be("v1");
        }

        [TestMethod]
        public void Parse_SimpleRequestsFile_ParsesRequestNames()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("simple-requests.http");

            // Act
            var result = parser.Parse(content, "simple-requests.http");

            // Assert
            result.Requests.Select(r => r.Name).Should().BeEquivalentTo(
                new[] { "GetUsers", "GetUser", "CreateUser", "UpdateUser", "DeleteUser" });
        }

        [TestMethod]
        public void Parse_SimpleRequestsFile_ParsesHttpMethods()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("simple-requests.http");

            // Act
            var result = parser.Parse(content, "simple-requests.http");

            // Assert
            result.Requests.Select(r => r.Method).Should().BeEquivalentTo(
                new[] { "GET", "GET", "POST", "PUT", "DELETE" });
        }

        [TestMethod]
        public void Parse_SimpleRequestsFile_ParsesHeaders()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("simple-requests.http");

            // Act
            var result = parser.Parse(content, "simple-requests.http");

            // Assert
            var getUsers = result.Requests.First(r => r.Name == "GetUsers");
            getUsers.Headers.Should().ContainKey("Accept");
            getUsers.Headers["Accept"].Should().Be("application/json");

            var createUser = result.Requests.First(r => r.Name == "CreateUser");
            createUser.Headers.Should().ContainKey("Content-Type");
            createUser.Headers["Content-Type"].Should().Be("application/json");
        }

        [TestMethod]
        public void Parse_SimpleRequestsFile_ParsesBodies()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("simple-requests.http");

            // Act
            var result = parser.Parse(content, "simple-requests.http");

            // Assert
            var createUser = result.Requests.First(r => r.Name == "CreateUser");
            createUser.Body.Should().NotBeNullOrEmpty();
            createUser.Body.Should().Contain("John Doe");

            var updateUser = result.Requests.First(r => r.Name == "UpdateUser");
            updateUser.Body.Should().Contain("John Updated");
        }

        [TestMethod]
        public void Parse_SimpleRequestsFile_NoChainedRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("simple-requests.http");

            // Act
            var result = parser.Parse(content, "simple-requests.http");

            // Assert
            result.HasChainedRequests.Should().BeFalse();
            result.Requests.All(r => !r.HasResponseReferences).Should().BeTrue();
        }

        #endregion

        #region Chained Requests Tests

        [TestMethod]
        public void Parse_ChainedRequestsFile_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("chained-requests.http");

            // Act
            var result = parser.Parse(content, "chained-requests.http");

            // Assert
            result.Should().NotBeNull();
            result.Diagnostics.Should().BeEmpty();
            result.Requests.Should().HaveCount(5);
        }

        [TestMethod]
        public void Parse_ChainedRequestsFile_HasChainedRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("chained-requests.http");

            // Act
            var result = parser.Parse(content, "chained-requests.http");

            // Assert
            result.HasChainedRequests.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_ChainedRequestsFile_LoginHasNoDependencies()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("chained-requests.http");

            // Act
            var result = parser.Parse(content, "chained-requests.http");

            // Assert
            var login = result.Requests.First(r => r.Name == "login");
            login.HasResponseReferences.Should().BeFalse();
            login.DependsOn.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_ChainedRequestsFile_GetProfileDependsOnLogin()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("chained-requests.http");

            // Act
            var result = parser.Parse(content, "chained-requests.http");

            // Assert
            var getProfile = result.Requests.First(r => r.Name == "getProfile");
            getProfile.HasResponseReferences.Should().BeTrue();
            getProfile.DependsOn.Should().Contain("login");
        }

        [TestMethod]
        public void Parse_ChainedRequestsFile_GetResourceDependsOnMultiple()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("chained-requests.http");

            // Act
            var result = parser.Parse(content, "chained-requests.http");

            // Assert
            var getResource = result.Requests.First(r => r.Name == "getResource");
            getResource.HasResponseReferences.Should().BeTrue();
            getResource.DependsOn.Should().Contain("login");
            getResource.DependsOn.Should().Contain("createResource");
        }

        [TestMethod]
        public void Parse_ChainedRequestsFile_BodyReferences()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("chained-requests.http");

            // Act
            var result = parser.Parse(content, "chained-requests.http");

            // Assert
            var updateProfile = result.Requests.First(r => r.Name == "updateProfile");
            updateProfile.Body.Should().Contain("{{login.response.body.$.timestamp}}");
        }

        [TestMethod]
        public void Parse_ChainedRequestsFile_HeaderReferences()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("chained-requests.http");

            // Act
            var result = parser.Parse(content, "chained-requests.http");

            // Assert
            var getProfile = result.Requests.First(r => r.Name == "getProfile");
            getProfile.Headers.Should().ContainKey("X-Request-Id");
            getProfile.Headers["X-Request-Id"].Should().Be("{{login.response.headers.X-Request-Id}}");
        }

        #endregion

        #region Edge Cases Tests

        [TestMethod]
        public void Parse_EdgeCasesFile_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("edge-cases.http");

            // Act
            var result = parser.Parse(content, "edge-cases.http");

            // Assert
            result.Should().NotBeNull();
            result.Requests.Should().HaveCountGreaterThan(5);
        }

        [TestMethod]
        public void Parse_EdgeCasesFile_ParsesHttp2Version()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("edge-cases.http");

            // Act
            var result = parser.Parse(content, "edge-cases.http");

            // Assert
            var http2Request = result.Requests.FirstOrDefault(r => r.Url.Contains("http2"));
            http2Request.Should().NotBeNull();
            http2Request.HttpVersion.Should().Be("HTTP/2");
        }

        [TestMethod]
        public void Parse_EdgeCasesFile_ParsesAllHttpMethods()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("edge-cases.http");

            // Act
            var result = parser.Parse(content, "edge-cases.http");

            // Assert
            result.Requests.Select(r => r.Method).Distinct().Should().Contain("OPTIONS");
            result.Requests.Select(r => r.Method).Distinct().Should().Contain("HEAD");
            result.Requests.Select(r => r.Method).Distinct().Should().Contain("TRACE");
            result.Requests.Select(r => r.Method).Distinct().Should().Contain("CONNECT");
        }

        [TestMethod]
        public void Parse_EdgeCasesFile_ParsesMultiLineHeader()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("edge-cases.http");

            // Act
            var result = parser.Parse(content, "edge-cases.http");

            // Assert
            var multilineRequest = result.Requests.FirstOrDefault(r => r.Name == "multilineHeader");
            multilineRequest.Should().NotBeNull();
            var authHeader = multilineRequest.Headers["Authorization"];
            authHeader.Should().Contain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
            authHeader.Should().Contain("SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c");
        }

        [TestMethod]
        public void Parse_EdgeCasesFile_ParsesEmptyHeaderValue()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("edge-cases.http");

            // Act
            var result = parser.Parse(content, "edge-cases.http");

            // Assert
            var emptyHeaderRequest = result.Requests.FirstOrDefault(r => r.Name == "emptyHeaderValue");
            emptyHeaderRequest.Should().NotBeNull();
            emptyHeaderRequest.Headers.Should().ContainKey("X-Empty-Header");
            emptyHeaderRequest.Headers["X-Empty-Header"].Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_EdgeCasesFile_ParsesHeaderWithColon()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("edge-cases.http");

            // Act
            var result = parser.Parse(content, "edge-cases.http");

            // Assert
            var colonRequest = result.Requests.FirstOrDefault(r => r.Name == "headerWithColon");
            colonRequest.Should().NotBeNull();
            colonRequest.Headers["X-Forwarded-Host"].Should().Be("api.example.com:8080");
            colonRequest.Headers["X-Real-URL"].Should().Be("https://internal.example.com:443/path");
        }

        [TestMethod]
        public void Parse_EdgeCasesFile_ParsesFileBodyReference()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("edge-cases.http");

            // Act
            var result = parser.Parse(content, "edge-cases.http");

            // Assert
            var fileBodyRequest = result.Requests.FirstOrDefault(r => r.Name == "fileBody");
            fileBodyRequest.Should().NotBeNull();
            fileBodyRequest.BodyFilePath.Should().Be("./data/payload.json");
            fileBodyRequest.IsFileBody.Should().BeTrue();
            fileBodyRequest.Body.Should().BeNull();
        }

        [TestMethod]
        public void Parse_EdgeCasesFile_ParsesMixedComments()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("edge-cases.http");

            // Act
            var result = parser.Parse(content, "edge-cases.http");

            // Assert
            var mixedCommentsRequest = result.Requests.FirstOrDefault(r => r.Name == "mixedComments");
            mixedCommentsRequest.Should().NotBeNull();
            mixedCommentsRequest.Comments.Should().HaveCount(2);
        }

        #endregion

    }

}
