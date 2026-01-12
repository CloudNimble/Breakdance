using System.IO;
using System.Linq;
using System.Reflection;
using CloudNimble.Breakdance.DotHttp;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for <see cref="DotHttpFileParser"/> using real .http files from GitHub repositories.
    /// These files were sourced from:
    /// - dotnet/eShop
    /// - dotnet/aspire
    /// - YuukanOO/seelf (chained requests with JSONPath)
    /// - vip32/aspnetcore-keycloak (OAuth flow with response variables)
    /// </summary>
    [TestClass]
    public class DotHttpFileParserRealWorldTests
    {

        #region Helper Methods

        private static string GetTestFilePath(string fileName)
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(assemblyLocation, "HttpFiles", "RealWorld", fileName);
        }

        private static string ReadTestFile(string fileName)
        {
            var filePath = GetTestFilePath(fileName);
            return File.ReadAllText(filePath);
        }

        #endregion

        #region dotnet/eShop - Catalog.API Tests

        [TestMethod]
        public void Parse_EShopCatalog_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("dotnet-eshop-catalog.http");

            // Act
            var result = parser.Parse(content, "dotnet-eshop-catalog.http");

            // Assert
            result.Should().NotBeNull();
            result.Diagnostics.Should().BeEmpty();
            result.Requests.Should().HaveCount(6);
        }

        [TestMethod]
        public void Parse_EShopCatalog_ParsesVariablesWithDots()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("dotnet-eshop-catalog.http");

            // Act
            var result = parser.Parse(content, "dotnet-eshop-catalog.http");

            // Assert
            result.Variables.Should().ContainKey("Catalog.API_HostAddress");
            result.Variables["Catalog.API_HostAddress"].Should().Be("http://localhost:5222");
            result.Variables.Should().ContainKey("ApiVersion");
            result.Variables["ApiVersion"].Should().Be("1.0");
        }

        [TestMethod]
        public void Parse_EShopCatalog_ParsesGetRequestsWithQueryParameters()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("dotnet-eshop-catalog.http");

            // Act
            var result = parser.Parse(content, "dotnet-eshop-catalog.http");

            // Assert
            var requestWithParams = result.Requests.ElementAt(1);
            requestWithParams.Url.Should().Contain("api-version={{ApiVersion}}");
        }

        [TestMethod]
        public void Parse_EShopCatalog_ParsesPutRequestWithJsonBody()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("dotnet-eshop-catalog.http");

            // Act
            var result = parser.Parse(content, "dotnet-eshop-catalog.http");

            // Assert
            var putRequest = result.Requests.First(r => r.Method == "PUT");
            putRequest.Body.Should().Contain("\"id\": 999");
            putRequest.Body.Should().Contain("\"name\": \"Item1\"");
            putRequest.Headers.Should().ContainKey("content-type");
            putRequest.Headers["content-type"].Should().Be("application/json");
        }

        [TestMethod]
        public void Parse_EShopCatalog_ParsesInlineComments()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("dotnet-eshop-catalog.http");

            // Act
            var result = parser.Parse(content, "dotnet-eshop-catalog.http");

            // Assert
            // Requests 4 and 5 have inline comments
            var request4 = result.Requests.ElementAt(3);
            request4.Comments.Should().NotBeEmpty();
            request4.Comments.Should().Contain(c => c.Contains("400 ProblemDetails"));
        }

        #endregion

        #region Seelf - Chained Requests with JSONPath Tests

        [TestMethod]
        public void Parse_SeelfChained_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert
            result.Should().NotBeNull();
            result.Diagnostics.Should().BeEmpty();
            result.Requests.Should().HaveCountGreaterThan(20);
        }

        [TestMethod]
        public void Parse_SeelfChained_ParsesCommentedOutVariable()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert - The active variable should be parsed
            result.Variables.Should().ContainKey("url");
            result.Variables["url"].Should().Be("http://localhost:8080/api/v1");
            // The commented out variable should NOT be in variables
            result.Variables.Should().NotContainKey("#@url");
        }

        [TestMethod]
        public void Parse_SeelfChained_ParsesNamedRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert
            var namedRequests = result.Requests.Where(r => !string.IsNullOrEmpty(r.Name)).ToList();
            namedRequests.Should().NotBeEmpty();
            namedRequests.Select(r => r.Name).Should().Contain("createTarget");
            namedRequests.Select(r => r.Name).Should().Contain("createRegistry");
            namedRequests.Select(r => r.Name).Should().Contain("createApp");
            namedRequests.Select(r => r.Name).Should().Contain("queueDeployment");
        }

        [TestMethod]
        public void Parse_SeelfChained_HasChainedRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert
            result.HasChainedRequests.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_SeelfChained_ParsesJsonPathReferences()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert - Find request that uses createTarget response
            var requestWithJsonPath = result.Requests.FirstOrDefault(r =>
                r.Url.Contains("{{createTarget.response.body.$.id}}"));
            requestWithJsonPath.Should().NotBeNull();
            requestWithJsonPath.DependsOn.Should().Contain("createTarget");
        }

        [TestMethod]
        public void Parse_SeelfChained_ParsesMultipleResponseReferences()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert - queueDeployment response is used in subsequent requests
            var requestsUsingQueueDeployment = result.Requests.Where(r =>
                r.Url.Contains("queueDeployment.response")).ToList();
            requestsUsingQueueDeployment.Should().NotBeEmpty();
        }

        [TestMethod]
        public void Parse_SeelfChained_ParsesAllHttpMethods()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert
            var methods = result.Requests.Select(r => r.Method).Distinct().ToList();
            methods.Should().Contain("GET");
            methods.Should().Contain("POST");
            methods.Should().Contain("PUT");
            methods.Should().Contain("PATCH");
            methods.Should().Contain("DELETE");
        }

        [TestMethod]
        public void Parse_SeelfChained_ParsesMultipartFormData()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert
            var multipartRequest = result.Requests.FirstOrDefault(r =>
                r.Headers.TryGetValue("Content-Type", out var ct) &&
                ct.Contains("multipart/form-data"));
            multipartRequest.Should().NotBeNull();
            multipartRequest.Body.Should().Contain("WebKitFormBoundary");
            multipartRequest.Body.Should().Contain("Content-Disposition: form-data");
        }

        [TestMethod]
        public void Parse_SeelfChained_ParsesFileBodyReferenceInMultipart()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert - The multipart body contains a file reference
            var multipartRequest = result.Requests.FirstOrDefault(r =>
                r.Headers.TryGetValue("Content-Type", out var ct) &&
                ct.Contains("multipart/form-data"));
            multipartRequest.Should().NotBeNull();
            multipartRequest.Body.Should().Contain("< ./examples/go-api/go-api.tar.gz");
        }

        [TestMethod]
        public void Parse_SeelfChained_ParsesNestedJsonInBody()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert - createApp request has deeply nested JSON
            var createAppRequest = result.Requests.FirstOrDefault(r => r.Name == "createApp");
            createAppRequest.Should().NotBeNull();
            createAppRequest.Body.Should().Contain("\"version_control\":");
            createAppRequest.Body.Should().Contain("\"production\":");
            createAppRequest.Body.Should().Contain("\"staging\":");
            createAppRequest.Body.Should().Contain("\"vars\":");
        }

        [TestMethod]
        public void Parse_SeelfChained_ParsesResponseReferenceInJsonBody()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert - createApp body references createTarget response
            var createAppRequest = result.Requests.FirstOrDefault(r => r.Name == "createApp");
            createAppRequest.Should().NotBeNull();
            createAppRequest.Body.Should().Contain("{{createTarget.response.body.$.id}}");
            createAppRequest.DependsOn.Should().Contain("createTarget");
        }

        #endregion

        #region Keycloak OAuth Flow Tests

        [TestMethod]
        public void Parse_KeycloakOAuth_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("keycloak-oauth-flow.http");

            // Act
            var result = parser.Parse(content, "keycloak-oauth-flow.http");

            // Assert
            result.Should().NotBeNull();
            result.Diagnostics.Should().BeEmpty();
            result.Requests.Should().HaveCount(6);
        }

        [TestMethod]
        public void Parse_KeycloakOAuth_ParsesHttpVersion()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("keycloak-oauth-flow.http");

            // Act
            var result = parser.Parse(content, "keycloak-oauth-flow.http");

            // Assert - All requests have HTTP/1.1
            result.Requests.All(r => r.HttpVersion == "HTTP/1.1").Should().BeTrue();
        }

        [TestMethod]
        public void Parse_KeycloakOAuth_ParsesVariableAssignmentFromResponse()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("keycloak-oauth-flow.http");

            // Act
            var result = parser.Parse(content, "keycloak-oauth-flow.http");

            // Assert - Response variable assignments that appear after request body are currently
            // included in the body. This test verifies the sign_in request body contains these
            // variable assignment patterns, which can be used for response chaining.
            var signInRequest = result.Requests.FirstOrDefault(r => r.Name == "sign_in");
            signInRequest.Should().NotBeNull();
            signInRequest.Body.Should().Contain("@access_token = {{sign_in.response.body.$.access_token}}");
            signInRequest.Body.Should().Contain("@refresh_token = {{sign_in.response.body.$.refresh_token}}");

            // The HasResponseReferences flag should be true since body contains response references
            signInRequest.HasResponseReferences.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_KeycloakOAuth_ParsesNamedSignInRequest()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("keycloak-oauth-flow.http");

            // Act
            var result = parser.Parse(content, "keycloak-oauth-flow.http");

            // Assert
            var signInRequest = result.Requests.FirstOrDefault(r => r.Name == "sign_in");
            signInRequest.Should().NotBeNull();
            signInRequest.Method.Should().Be("POST");
        }

        [TestMethod]
        public void Parse_KeycloakOAuth_ParsesFormUrlEncodedBody()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("keycloak-oauth-flow.http");

            // Act
            var result = parser.Parse(content, "keycloak-oauth-flow.http");

            // Assert
            var signInRequest = result.Requests.FirstOrDefault(r => r.Name == "sign_in");
            signInRequest.Should().NotBeNull();
            signInRequest.Headers["Content-Type"].Should().Be("application/x-www-form-urlencoded");
            signInRequest.Body.Should().Contain("grant_type=password");
            signInRequest.Body.Should().Contain("client_id=aspnetcore-keycloak");
            signInRequest.Body.Should().Contain("scope=openid");
        }

        [TestMethod]
        public void Parse_KeycloakOAuth_ParsesAuthorizationBearerHeader()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("keycloak-oauth-flow.http");

            // Act
            var result = parser.Parse(content, "keycloak-oauth-flow.http");

            // Assert - Requests after sign_in use the access_token variable
            var userInfoRequest = result.Requests.FirstOrDefault(r => r.Url.Contains("/userinfo"));
            userInfoRequest.Should().NotBeNull();
            userInfoRequest.Headers.Should().ContainKey("Authorization");
            userInfoRequest.Headers["Authorization"].Should().Be("Bearer {{access_token}}");
        }

        [TestMethod]
        public void Parse_KeycloakOAuth_ParsesRefreshTokenRequest()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("keycloak-oauth-flow.http");

            // Act
            var result = parser.Parse(content, "keycloak-oauth-flow.http");

            // Assert
            var refreshRequest = result.Requests.FirstOrDefault(r =>
                r.Body != null && r.Body.Contains("grant_type=refresh_token"));
            refreshRequest.Should().NotBeNull();
            refreshRequest.Body.Should().Contain("refresh_token={{refresh_token}}");
        }

        [TestMethod]
        public void Parse_KeycloakOAuth_ParsesCommentBlockSeparators()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("keycloak-oauth-flow.http");

            // Act
            var result = parser.Parse(content, "keycloak-oauth-flow.http");

            // Assert - The file uses ### blocks with descriptions between variables and requests.
            // The parser correctly identifies all 6 requests at lines 5, 11, 27, 33, 44, 50.
            // The first request (openid-configuration) is parsed from the comment block section.
            var openIdRequest = result.Requests.First();
            openIdRequest.Method.Should().Be("GET");
            openIdRequest.Url.Should().Contain("openid-configuration");
            openIdRequest.LineNumber.Should().Be(5);
        }

        #endregion

        #region dotnet/aspire Tests

        [TestMethod]
        public void Parse_AspireNats_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("dotnet-aspire-nats.http");

            // Act
            var result = parser.Parse(content, "dotnet-aspire-nats.http");

            // Assert
            result.Should().NotBeNull();
            result.Diagnostics.Should().BeEmpty();
            result.Requests.Should().HaveCount(6);
        }

        [TestMethod]
        public void Parse_AspireMySql_ParsesDeleteRequest()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadTestFile("dotnet-aspire-mysql.http");

            // Act
            var result = parser.Parse(content, "dotnet-aspire-mysql.http");

            // Assert
            var deleteRequest = result.Requests.First(r => r.Method == "DELETE");
            deleteRequest.Url.Should().Be("{{HostAddress}}/catalog/4");
        }

        #endregion

        #region Cross-File Verification Tests

        [TestMethod]
        public void Parse_AllRealWorldFiles_NoDiagnostics()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var files = new[]
            {
                "dotnet-eshop-catalog.http",
                "dotnet-aspire-nats.http",
                "dotnet-aspire-mysql.http",
                "seelf-api-chained.http",
                "keycloak-oauth-flow.http"
            };

            // Act & Assert
            foreach (var file in files)
            {
                var content = ReadTestFile(file);
                var result = parser.Parse(content, file);

                result.Diagnostics.Should().BeEmpty($"File {file} should have no diagnostics");
                result.Requests.Should().NotBeEmpty($"File {file} should have at least one request");
            }
        }

        [TestMethod]
        public void Parse_AllRealWorldFiles_AllRequestsHaveMethodAndUrl()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var files = new[]
            {
                "dotnet-eshop-catalog.http",
                "dotnet-aspire-nats.http",
                "dotnet-aspire-mysql.http",
                "seelf-api-chained.http",
                "keycloak-oauth-flow.http"
            };

            // Act & Assert
            foreach (var file in files)
            {
                var content = ReadTestFile(file);
                var result = parser.Parse(content, file);

                foreach (var request in result.Requests)
                {
                    request.Method.Should().NotBeNullOrEmpty($"Request in {file} should have a method");
                    request.Url.Should().NotBeNullOrEmpty($"Request in {file} should have a URL");
                }
            }
        }

        [TestMethod]
        public void Parse_ChainedFiles_CorrectlyIdentifyDependencies()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var chainedFiles = new[]
            {
                "seelf-api-chained.http",
                "keycloak-oauth-flow.http"
            };

            // Act & Assert
            foreach (var file in chainedFiles)
            {
                var content = ReadTestFile(file);
                var result = parser.Parse(content, file);

                result.HasChainedRequests.Should().BeTrue($"File {file} should have chained requests");

                // At least one request should have dependencies
                result.Requests.Any(r => r.DependsOn.Count > 0 || r.HasResponseReferences)
                    .Should().BeTrue($"File {file} should have requests with dependencies");
            }
        }

        #endregion

    }

}
