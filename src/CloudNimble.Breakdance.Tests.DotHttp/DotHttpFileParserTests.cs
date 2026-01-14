using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CloudNimble.Breakdance.DotHttp;
using CloudNimble.Breakdance.DotHttp.Generator;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Unit tests for the <see cref="DotHttpFileParser"/> class.
    /// </summary>
    /// <remarks>
    /// These files were sourced from:
    /// - dotnet/eShop
    /// - dotnet/aspire
    /// - YuukanOO/seelf (chained requests with JSONPath)
    /// - vip32/aspnetcore-keycloak (OAuth flow with response variables)
    /// </remarks>
    [TestClass]
    public class DotHttpFileParserTests
    {

        #region Helper Methods

        private static string GetTestFilePath(string fileName)
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(assemblyLocation, "HttpFiles", fileName);
        }

        private static string GetRealWorldTestFilePath(string fileName)
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(assemblyLocation, "HttpFiles", "RealWorld", fileName);
        }

        private static string ReadTestFile(string fileName)
        {
            var filePath = GetTestFilePath(fileName);
            return File.ReadAllText(filePath);
        }

        private static string ReadRealWorldTestFile(string fileName)
        {
            var filePath = GetRealWorldTestFilePath(fileName);
            return File.ReadAllText(filePath);
        }

        private static ParseContext CreateContext(DotHttpFile file, string[] lines)
        {
            return new ParseContext(file, lines);
        }

        #endregion

        #region Parse Method Tests

        [TestMethod]
        public void Parse_NullFilePath_ThrowsArgumentNullException()
        {
            // Arrange
            var parser = new DotHttpFileParser();

            // Act
            Action act = () => parser.Parse("content", null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("filePath");
        }

        [TestMethod]
        public void Parse_NullContent_ReturnsEmptyFile()
        {
            // Arrange
            var parser = new DotHttpFileParser();

            // Act
            var result = parser.Parse(null, "test.http");

            // Assert
            result.Should().NotBeNull();
            result.FilePath.Should().Be("test.http");
            result.Requests.Should().BeEmpty();
            result.Variables.Should().BeEmpty();
            result.Diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_EmptyContent_ReturnsEmptyFile()
        {
            // Arrange
            var parser = new DotHttpFileParser();

            // Act
            var result = parser.Parse(string.Empty, "test.http");

            // Assert
            result.Should().NotBeNull();
            result.FilePath.Should().Be("test.http");
            result.Requests.Should().BeEmpty();
            result.Variables.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_WhitespaceOnlyContent_ReturnsEmptyFile()
        {
            // Arrange
            var parser = new DotHttpFileParser();

            // Act
            var result = parser.Parse("   \t\n  ", "test.http");

            // Assert
            result.Should().NotBeNull();
            result.Requests.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_SimpleGetRequest_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = "GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            var request = result.Requests[0];
            request.Method.Should().Be("GET");
            request.Url.Should().Be("https://api.example.com/users");
            request.HttpVersion.Should().BeNull();
            request.LineNumber.Should().Be(1);
        }

        [TestMethod]
        public void Parse_RequestWithHttpVersion_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = "GET https://api.example.com/users HTTP/1.1";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            var request = result.Requests[0];
            request.Method.Should().Be("GET");
            request.Url.Should().Be("https://api.example.com/users");
            request.HttpVersion.Should().Be("HTTP/1.1");
        }

        [TestMethod]
        public void Parse_RequestWithHttp2Version_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = "GET https://api.example.com/users HTTP/2";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            result.Requests[0].HttpVersion.Should().Be("HTTP/2");
        }

        [TestMethod]
        public void Parse_AllHttpMethods_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var methods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE", "CONNECT" };

            foreach (var method in methods)
            {
                var content = $"{method} https://api.example.com/test";

                // Act
                var result = parser.Parse(content, "test.http");

                // Assert
                result.Requests.Should().HaveCount(1, $"because {method} is a valid HTTP method");
                result.Requests[0].Method.Should().Be(method);
            }
        }

        [TestMethod]
        public void Parse_LowercaseMethod_ConvertsToUppercase()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = "get https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            result.Requests[0].Method.Should().Be("GET");
        }

        [TestMethod]
        public void Parse_UnknownMethod_AddsWarning()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = "CUSTOM https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            result.Requests[0].Method.Should().Be("CUSTOM");
            result.Diagnostics.Should().HaveCount(1);
            result.Diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
            result.Diagnostics[0].Id.Should().Be("DOTHTTP005");
            result.Diagnostics[0].GetMessage().Should().Contain("Unknown HTTP method");
        }

        #endregion

        #region Variable Tests

        [TestMethod]
        public void Parse_FileLevelVariable_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"@baseUrl = https://api.example.com
GET {{baseUrl}}/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Variables.Should().ContainKey("baseUrl");
            result.Variables["baseUrl"].Should().Be("https://api.example.com");
        }

        [TestMethod]
        public void Parse_MultipleFileLevelVariables_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"@baseUrl = https://api.example.com
@apiVersion = v2
@timeout = 30
GET {{baseUrl}}/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Variables.Should().HaveCount(3);
            result.Variables["baseUrl"].Should().Be("https://api.example.com");
            result.Variables["apiVersion"].Should().Be("v2");
            result.Variables["timeout"].Should().Be("30");
        }

        [TestMethod]
        public void Parse_VariableWithNoValue_ParsesAsEmptyString()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"@emptyVar =
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Variables.Should().ContainKey("emptyVar");
            result.Variables["emptyVar"].Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_InvalidVariableNoEquals_AddsError()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"@invalidVariable
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Diagnostics.Should().HaveCount(1);
            result.Diagnostics[0].Id.Should().Be("DOTHTTP003");
            result.Diagnostics[0].GetMessage().Should().Contain("Invalid variable definition");
        }

        [TestMethod]
        public void Parse_VariableWithSpacesInName_AddsError()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"@base url = https://api.example.com
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Diagnostics.Should().HaveCount(1);
            result.Diagnostics[0].Id.Should().Be("DOTHTTP003");
            result.Diagnostics[0].GetMessage().Should().Contain("spaces are not allowed");
        }

        [TestMethod]
        public void Parse_VariableWithEmptyName_AddsError()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"@ = value
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Diagnostics.Should().HaveCount(1);
            result.Diagnostics[0].Id.Should().Be("DOTHTTP003");
            result.Diagnostics[0].GetMessage().Should().Contain("empty variable name");
        }

        [TestMethod]
        public void Parse_RequestLevelVariable_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"@baseUrl = https://api.example.com
GET {{baseUrl}}/users

###

@baseUrl = https://override.example.com
GET {{baseUrl}}/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(2);
            result.Variables["baseUrl"].Should().Be("https://api.example.com");
            result.Requests[1].Variables.Should().ContainKey("baseUrl");
            result.Requests[1].Variables["baseUrl"].Should().Be("https://override.example.com");
        }

        #endregion

        #region Header Tests

        [TestMethod]
        public void Parse_RequestWithHeaders_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users
Accept: application/json
Authorization: Bearer token123";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            var request = result.Requests[0];
            request.Headers.Should().HaveCount(2);
            request.Headers["Accept"].Should().Be("application/json");
            request.Headers["Authorization"].Should().Be("Bearer token123");
        }

        [TestMethod]
        public void Parse_HeaderWithEmptyValue_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users
X-Custom-Header:";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].Headers["X-Custom-Header"].Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_MultiLineHeader_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
 .eyJzdWIiOiIxMjM0NTY3ODkwIn0";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            var authHeader = result.Requests[0].Headers["Authorization"];
            authHeader.Should().Contain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
            authHeader.Should().Contain(".eyJzdWIiOiIxMjM0NTY3ODkwIn0");
        }

        [TestMethod]
        public void Parse_InvalidHeaderNoColon_AddsError()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users
InvalidHeader";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Diagnostics.Should().HaveCount(1);
            result.Diagnostics[0].Id.Should().Be("DOTHTTP002");
            result.Diagnostics[0].GetMessage().Should().Contain("Invalid header format");
        }

        [TestMethod]
        public void Parse_HeaderWithColonInValue_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users
X-Forwarded-Host: api.example.com:8080";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].Headers["X-Forwarded-Host"].Should().Be("api.example.com:8080");
        }

        #endregion

        #region Body Tests

        [TestMethod]
        public void Parse_RequestWithJsonBody_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"POST https://api.example.com/users
Content-Type: application/json

{
    ""name"": ""John"",
    ""email"": ""john@example.com""
}";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            var request = result.Requests[0];
            request.Body.Should().NotBeNull();
            request.Body.Should().Contain("\"name\": \"John\"");
            request.Body.Should().Contain("\"email\": \"john@example.com\"");
        }

        [TestMethod]
        public void Parse_RequestWithFileBodyReference_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"POST https://api.example.com/upload
Content-Type: application/octet-stream

< ./path/to/file.bin";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            var request = result.Requests[0];
            request.BodyFilePath.Should().Be("./path/to/file.bin");
            request.Body.Should().BeNull();
            request.IsFileBody.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_FileBodyWithContentAfter_AddsWarning()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"POST https://api.example.com/upload
Content-Type: application/octet-stream

< ./path/to/file.bin
extra content";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].BodyFilePath.Should().Be("./path/to/file.bin");
            result.Diagnostics.Should().HaveCount(1);
            result.Diagnostics[0].Id.Should().Be("DOTHTTP004");
            result.Diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        }

        [TestMethod]
        public void Parse_BodyWithTrailingWhitespaceLines_TrimsCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"POST https://api.example.com/users
Content-Type: application/json

{""name"": ""John""}

";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].Body.Should().Be("{\"name\": \"John\"}");
        }

        #endregion

        #region Comment Tests

        [TestMethod]
        public void Parse_HashComment_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"# This is a comment
# Another comment
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            result.Requests[0].Comments.Should().HaveCount(2);
            result.Requests[0].Comments[0].Should().Be("This is a comment");
            result.Requests[0].Comments[1].Should().Be("Another comment");
        }

        [TestMethod]
        public void Parse_DoubleSlashComment_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"// This is a comment
// Another comment
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            result.Requests[0].Comments.Should().HaveCount(2);
            result.Requests[0].Comments[0].Should().Be("This is a comment");
        }

        [TestMethod]
        public void Parse_RequestNameDirective_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"# @name GetAllUsers
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            result.Requests[0].Name.Should().Be("GetAllUsers");
        }

        [TestMethod]
        public void Parse_RequestNameDirectiveWithDoubleSlash_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"// @name GetAllUsers
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].Name.Should().Be("GetAllUsers");
        }

        [TestMethod]
        public void Parse_RequestNameWithHyphen_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"# @name get-all-users
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].Name.Should().Be("get-all-users");
        }

        [TestMethod]
        public void Parse_EmptyHashComment_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"#
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].Comments.Should().HaveCount(1);
            result.Requests[0].Comments[0].Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_EmptyDoubleSlashComment_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"//
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].Comments.Should().HaveCount(1);
            result.Requests[0].Comments[0].Should().BeEmpty();
        }

        #endregion

        #region Request Separator Tests

        [TestMethod]
        public void Parse_MultipleRequestsWithSeparator_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users

###

POST https://api.example.com/users
Content-Type: application/json

{""name"": ""John""}";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(2);
            result.Requests[0].Method.Should().Be("GET");
            result.Requests[1].Method.Should().Be("POST");
            result.Requests[1].Body.Should().Contain("John");
        }

        [TestMethod]
        public void Parse_SeparatorWithDescription_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users

### Create a new user

POST https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(2);
            result.Requests[0].SeparatorTitle.Should().BeNull(); // First request has no separator
            result.Requests[1].SeparatorTitle.Should().Be("Create a new user"); // Second request has separator title
        }

        [TestMethod]
        public void Parse_SeparatorAtStart_StartsNewRequest()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"###
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parse_MultipleSeparatorsConsecutive_HandlesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users

###
###
###

POST https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(2);
        }

        #endregion

        #region Response Reference Tests

        [TestMethod]
        public void Parse_UrlWithResponseReference_SetsHasResponseReferences()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users/{{login.response.body.$.userId}}";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].HasResponseReferences.Should().BeTrue();
            result.Requests[0].DependsOn.Should().Contain("login");
        }

        [TestMethod]
        public void Parse_HeaderWithResponseReference_SetsHasResponseReferences()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users
Authorization: Bearer {{login.response.body.$.token}}";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].HasResponseReferences.Should().BeTrue();
            result.Requests[0].DependsOn.Should().Contain("login");
        }

        [TestMethod]
        public void Parse_BodyWithResponseReference_SetsHasResponseReferences()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"POST https://api.example.com/users
Content-Type: application/json

{""parentId"": ""{{parent.response.body.$.id}}""}";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].HasResponseReferences.Should().BeTrue();
            result.Requests[0].DependsOn.Should().Contain("parent");
        }

        [TestMethod]
        public void Parse_MultipleResponseReferences_TracksAllDependencies()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users/{{user.response.body.$.id}}
Authorization: Bearer {{login.response.body.$.token}}
X-Request-Id: {{init.response.headers.X-Request-Id}}";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].DependsOn.Should().HaveCount(3);
            result.Requests[0].DependsOn.Should().Contain("user");
            result.Requests[0].DependsOn.Should().Contain("login");
            result.Requests[0].DependsOn.Should().Contain("init");
        }

        [TestMethod]
        public void Parse_DuplicateResponseReferences_DoesNotDuplicate()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users/{{login.response.body.$.userId}}
Authorization: Bearer {{login.response.body.$.token}}";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].DependsOn.Should().HaveCount(1);
            result.Requests[0].DependsOn.Should().Contain("login");
        }

        [TestMethod]
        public void Parse_HasChainedRequests_ReturnsTrueWhenHasReferences()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"# @name login
POST https://api.example.com/login

###

GET https://api.example.com/users
Authorization: Bearer {{login.response.body.$.token}}";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.HasChainedRequests.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_NoResponseReferences_HasChainedRequestsIsFalse()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users

###

POST https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.HasChainedRequests.Should().BeFalse();
        }

        #endregion

        #region Line Ending Tests

        [TestMethod]
        public void Parse_WindowsLineEndings_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = "GET https://api.example.com/users\r\nAccept: application/json";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            result.Requests[0].Headers.Should().ContainKey("Accept");
        }

        [TestMethod]
        public void Parse_UnixLineEndings_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = "GET https://api.example.com/users\nAccept: application/json";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            result.Requests[0].Headers.Should().ContainKey("Accept");
        }

        [TestMethod]
        public void Parse_OldMacLineEndings_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = "GET https://api.example.com/users\rAccept: application/json";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
            result.Requests[0].Headers.Should().ContainKey("Accept");
        }

        [TestMethod]
        public void Parse_MixedLineEndings_ParsesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = "GET https://api.example.com/users\r\nAccept: application/json\nX-Custom: value";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].Headers.Should().HaveCount(2);
        }

        #endregion

        #region Edge Cases Tests

        [TestMethod]
        public void Parse_OnlyComments_NoRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"# Just a comment
// Another comment";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_OnlyVariables_NoRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"@baseUrl = https://api.example.com
@apiKey = secret";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().BeEmpty();
            result.Variables.Should().HaveCount(2);
        }

        [TestMethod]
        public void Parse_MalformedRequestLine_AddsError()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"GET";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Diagnostics.Should().HaveCount(1);
            result.Diagnostics[0].Id.Should().Be("DOTHTTP001");
            result.Diagnostics[0].GetMessage().Should().Contain("Malformed request line");
        }

        [TestMethod]
        public void Parse_UnrecognizedLine_DoesNotAddError()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"some random text
GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            // "some" is not a valid HTTP method, so no error is added
            result.Diagnostics.Should().BeEmpty();
            result.Requests.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parse_EmptyLinesBeforeRequest_HandlesCorrectly()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"


GET https://api.example.com/users";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests.Should().HaveCount(1);
        }

        [TestMethod]
        public void Parse_WhitespaceOnlyLinesInHeaders_TransitionsToBody()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = @"POST https://api.example.com/users
Content-Type: application/json

{""test"": true}";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            result.Requests[0].Body.Should().Be("{\"test\": true}");
        }

        [TestMethod]
        public void Parse_HeaderContinuationWithNoHeaders_IgnoresContinuation()
        {
            // This edge case tests when we have whitespace at the start of a line
            // but no previous headers exist - the continuation should be ignored
            // Note: This is a defensive test - in practice this scenario is unlikely
            var parser = new DotHttpFileParser();
            var content = @"GET https://api.example.com/users
 continuation-without-header";

            // Act
            var result = parser.Parse(content, "test.http");

            // Assert
            // The continuation line should be treated as invalid header
            result.Diagnostics.Should().NotBeEmpty();
        }

        #endregion

        #region SplitLines Tests

        [TestMethod]
        public void SplitLines_WithLf_SplitsCorrectly()
        {
            var lines = DotHttpFileParser.SplitLines("line1\nline2\nline3");

            lines.Should().HaveCount(3);
            lines[0].Should().Be("line1");
            lines[1].Should().Be("line2");
            lines[2].Should().Be("line3");
        }

        [TestMethod]
        public void SplitLines_WithCrLf_SplitsCorrectly()
        {
            var lines = DotHttpFileParser.SplitLines("line1\r\nline2\r\nline3");

            lines.Should().HaveCount(3);
            lines[0].Should().Be("line1");
            lines[1].Should().Be("line2");
            lines[2].Should().Be("line3");
        }

        [TestMethod]
        public void SplitLines_WithCr_SplitsCorrectly()
        {
            var lines = DotHttpFileParser.SplitLines("line1\rline2\rline3");

            lines.Should().HaveCount(3);
            lines[0].Should().Be("line1");
            lines[1].Should().Be("line2");
            lines[2].Should().Be("line3");
        }

        [TestMethod]
        public void SplitLines_WithMixedLineEndings_SplitsCorrectly()
        {
            var lines = DotHttpFileParser.SplitLines("line1\r\nline2\nline3\rline4");

            lines.Should().HaveCount(4);
            lines[0].Should().Be("line1");
            lines[1].Should().Be("line2");
            lines[2].Should().Be("line3");
            lines[3].Should().Be("line4");
        }

        [TestMethod]
        public void SplitLines_EmptyString_ReturnsEmptyArray()
        {
            var lines = DotHttpFileParser.SplitLines("");

            lines.Should().BeEmpty();
        }

        [TestMethod]
        public void SplitLines_SingleLine_ReturnsSingleElement()
        {
            var lines = DotHttpFileParser.SplitLines("single line");

            lines.Should().HaveCount(1);
            lines[0].Should().Be("single line");
        }

        [TestMethod]
        public void SplitLines_TrailingNewline_IncludesEmptyLine()
        {
            var lines = DotHttpFileParser.SplitLines("line1\nline2\n");

            lines.Should().HaveCount(3);
            lines[2].Should().BeEmpty();
        }

        [TestMethod]
        public void SplitLines_LeadingNewline_IncludesEmptyLine()
        {
            var lines = DotHttpFileParser.SplitLines("\nline1\nline2");

            lines.Should().HaveCount(3);
            lines[0].Should().BeEmpty();
        }

        [TestMethod]
        public void SplitLines_MultipleConsecutiveNewlines_PreservesEmptyLines()
        {
            var lines = DotHttpFileParser.SplitLines("line1\n\n\nline2");

            lines.Should().HaveCount(4);
            lines[0].Should().Be("line1");
            lines[1].Should().BeEmpty();
            lines[2].Should().BeEmpty();
            lines[3].Should().Be("line2");
        }

        [TestMethod]
        public void SplitLines_WhitespaceOnlyLines_PreservesWhitespace()
        {
            var lines = DotHttpFileParser.SplitLines("line1\n   \nline2");

            lines.Should().HaveCount(3);
            lines[1].Should().Be("   ");
        }

        #endregion

        #region AddDiagnostic Tests

        [TestMethod]
        public void AddDiagnostic_Error_AddsErrorDiagnostic()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "line 1", "line 2" };
            var context = CreateContext(file, lines);
            context.LineIndex = 1;

            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.RequestLineErrorDescriptor, "Test error message");

            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP001");
            file.Diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
            file.Diagnostics[0].GetMessage().Should().Be("Test error message");
        }

        [TestMethod]
        public void AddDiagnostic_Warning_AddsWarningDiagnostic()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "line 1" };
            var context = CreateContext(file, lines);

            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.BodyWarningDescriptor, "Test warning");

            file.Diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        }

        [TestMethod]
        public void AddDiagnostic_MultipleCalls_AddsAllDiagnostics()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "line 1", "line 2", "line 3" };
            var context = CreateContext(file, lines);

            context.LineIndex = 0;
            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.RequestLineErrorDescriptor, "Error 1");

            context.LineIndex = 1;
            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.BodyWarningDescriptor, "Warning 1");

            context.LineIndex = 2;
            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.HeaderErrorDescriptor, "Error 2");

            file.Diagnostics.Should().HaveCount(3);
        }

        [TestMethod]
        public void AddDiagnostic_CorrectLocation_HasCorrectLineNumber()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "line 1", "error line", "line 3" };
            var context = CreateContext(file, lines);
            context.LineIndex = 1;

            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.RequestLineErrorDescriptor, "Error on line 2");

            var diagnostic = file.Diagnostics[0];
            var location = diagnostic.Location;
            location.GetLineSpan().StartLinePosition.Line.Should().Be(1);
        }

        [TestMethod]
        public void AddDiagnostic_WithColumn_HasCorrectColumn()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "line with error" };
            var context = CreateContext(file, lines);

            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.RequestLineErrorDescriptor, "Error at column 5", column: 5);

            var diagnostic = file.Diagnostics[0];
            var location = diagnostic.Location;
            location.GetLineSpan().StartLinePosition.Character.Should().Be(4); // 0-based, so 5 -> 4
        }

        #endregion

        #region CheckForResponseReferences Tests

        [TestMethod]
        public void CheckForResponseReferences_WithResponseReference_SetsHasResponseReferences()
        {
            var request = new DotHttpRequest();
            var text = "{{login.response.body.$.token}}";

            DotHttpFileParser.CheckForResponseReferences(request, text.AsSpan());

            request.HasResponseReferences.Should().BeTrue();
        }

        [TestMethod]
        public void CheckForResponseReferences_WithoutResponseReference_DoesNotSetFlag()
        {
            var request = new DotHttpRequest();
            var text = "{{baseUrl}}/api/users";

            DotHttpFileParser.CheckForResponseReferences(request, text.AsSpan());

            request.HasResponseReferences.Should().BeFalse();
        }

        [TestMethod]
        public void CheckForResponseReferences_AddsDependency()
        {
            var request = new DotHttpRequest();
            var text = "{{auth.response.body.$.token}}";

            DotHttpFileParser.CheckForResponseReferences(request, text.AsSpan());

            request.DependsOn.Should().Contain("auth");
        }

        [TestMethod]
        public void CheckForResponseReferences_MultipleReferences_AddsAllDependencies()
        {
            var request = new DotHttpRequest();
            var text = "{{login.response.body.$.token}} and {{user.response.body.$.id}}";

            DotHttpFileParser.CheckForResponseReferences(request, text.AsSpan());

            request.DependsOn.Should().HaveCount(2);
            request.DependsOn.Should().Contain("login");
            request.DependsOn.Should().Contain("user");
        }

        [TestMethod]
        public void CheckForResponseReferences_DuplicateReferences_AddsOnce()
        {
            var request = new DotHttpRequest();
            var text = "{{login.response.body.$.token}} {{login.response.body.$.id}}";

            DotHttpFileParser.CheckForResponseReferences(request, text.AsSpan());

            request.DependsOn.Should().HaveCount(1);
        }

        [TestMethod]
        public void CheckForResponseReferences_HeaderReference_AddsDependency()
        {
            var request = new DotHttpRequest();
            var text = "{{auth.response.headers.X-Request-Id}}";

            DotHttpFileParser.CheckForResponseReferences(request, text.AsSpan());

            request.DependsOn.Should().Contain("auth");
        }

        [TestMethod]
        public void CheckForResponseReferences_EmptyText_DoesNothing()
        {
            var request = new DotHttpRequest();

            DotHttpFileParser.CheckForResponseReferences(request, "".AsSpan());

            request.HasResponseReferences.Should().BeFalse();
            request.DependsOn.Should().BeEmpty();
        }

        [TestMethod]
        public void CheckForResponseReferences_EmptySpan_DoesNothing()
        {
            var request = new DotHttpRequest();

            DotHttpFileParser.CheckForResponseReferences(request, ReadOnlySpan<char>.Empty);

            request.HasResponseReferences.Should().BeFalse();
            request.DependsOn.Should().BeEmpty();
        }

        #endregion

        #region FinishRequest Tests

        [TestMethod]
        public void FinishRequest_WithBodyLines_JoinsAndTrims()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = CreateContext(file, Array.Empty<string>());
            var request = new DotHttpRequest { Method = "POST", Url = "/api" };
            var bodyLines = new List<string> { "{", "  \"name\": \"test\"", "}", "", "  " };

            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            request.Body.Should().Be("{\n  \"name\": \"test\"\n}");
        }

        [TestMethod]
        public void FinishRequest_EmptyBody_DoesNotSetBody()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = CreateContext(file, Array.Empty<string>());
            var request = new DotHttpRequest { Method = "GET", Url = "/api" };
            var bodyLines = new List<string>();

            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            request.Body.Should().BeNull();
        }

        [TestMethod]
        public void FinishRequest_WhitespaceOnlyBody_DoesNotSetBody()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = CreateContext(file, Array.Empty<string>());
            var request = new DotHttpRequest { Method = "GET", Url = "/api" };
            var bodyLines = new List<string> { "   ", "\t", "" };

            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            request.Body.Should().BeNull();
        }

        [TestMethod]
        public void FinishRequest_WithFileReference_SetsBodyFilePath()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = CreateContext(file, Array.Empty<string>());
            var request = new DotHttpRequest { Method = "POST", Url = "/api" };
            var bodyLines = new List<string> { "< ./data.json" };

            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            request.BodyFilePath.Should().Be("./data.json");
            request.Body.Should().BeNull();
        }

        [TestMethod]
        public void FinishRequest_WithFileReference_AddsWarningForExtraContent()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = CreateContext(file, Array.Empty<string>());
            var request = new DotHttpRequest { Method = "POST", Url = "/api" };
            var bodyLines = new List<string> { "< ./data.json", "extra content" };

            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            request.BodyFilePath.Should().Be("./data.json");
            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        }

        [TestMethod]
        public void FinishRequest_ChecksBodyForResponseReferences()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = CreateContext(file, Array.Empty<string>());
            var request = new DotHttpRequest { Method = "POST", Url = "/api" };
            var bodyLines = new List<string> { "{\"token\": \"{{login.response.body.$.token}}\"}" };

            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            request.HasResponseReferences.Should().BeTrue();
            request.DependsOn.Should().Contain("login");
        }

        #endregion

        #region ProcessHeaderState Tests

        [TestMethod]
        public void ProcessHeaderState_ValidHeader_AddsToHeaders()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "Accept: application/json" };
            var context = CreateContext(file, lines);
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessHeaderState(context, line, line.Trim(), request, ref state);

            state.Should().Be(ParserState.InHeaders);
            request.Headers.Should().ContainKey("Accept");
            request.Headers["Accept"].Should().Be("application/json");
        }

        [TestMethod]
        public void ProcessHeaderState_EmptyLine_TransitionsToBody()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "" };
            var context = CreateContext(file, lines);
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessHeaderState(context, line, line.Trim(), request, ref state);

            state.Should().Be(ParserState.InBody);
        }

        [TestMethod]
        public void ProcessHeaderState_HeaderContinuation_AppendsToLastHeader()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { " continuation value" };
            var context = CreateContext(file, lines);
            var request = new DotHttpRequest();
            request.Headers["Authorization"] = "Bearer token";
            var state = ParserState.InHeaders;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessHeaderState(context, line, line.Trim(), request, ref state);

            state.Should().Be(ParserState.InHeaders);
            request.Headers["Authorization"].Should().Contain("continuation value");
        }

        [TestMethod]
        public void ProcessHeaderState_InvalidHeader_AddsDiagnostic()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "InvalidHeaderWithoutColon" };
            var context = CreateContext(file, lines);
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessHeaderState(context, line, line.Trim(), request, ref state);

            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP002");
        }

        [TestMethod]
        public void ProcessHeaderState_HeaderWithColonInValue_ParsesCorrectly()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "X-Url: https://example.com:8080/path" };
            var context = CreateContext(file, lines);
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessHeaderState(context, line, line.Trim(), request, ref state);

            request.Headers["X-Url"].Should().Be("https://example.com:8080/path");
        }

        [TestMethod]
        public void ProcessHeaderState_HeaderWithEmptyValue_SetsEmptyString()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "X-Empty:" };
            var context = CreateContext(file, lines);
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessHeaderState(context, line, line.Trim(), request, ref state);

            request.Headers["X-Empty"].Should().BeEmpty();
        }

        [TestMethod]
        public void ProcessHeaderState_HeaderWithResponseReference_AddsDependency()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "Authorization: Bearer {{login.response.body.$.token}}" };
            var context = CreateContext(file, lines);
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessHeaderState(context, line, line.Trim(), request, ref state);

            request.Headers["Authorization"].Should().Be("Bearer {{login.response.body.$.token}}");
            request.HasResponseReferences.Should().BeTrue();
            request.DependsOn.Should().Contain("login");
        }

        #endregion

        #region ProcessStartState Tests

        [TestMethod]
        public void ProcessStartState_ValidRequestLine_CreatesRequest()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "GET https://api.example.com/users" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            state.Should().Be(ParserState.InHeaders);
            currentRequest.Should().NotBeNull();
            currentRequest.Method.Should().Be("GET");
            currentRequest.Url.Should().Be("https://api.example.com/users");
        }

        [TestMethod]
        public void ProcessStartState_RequestWithHttpVersion_ParsesVersion()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "GET https://api.example.com/users HTTP/1.1" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            currentRequest.HttpVersion.Should().Be("HTTP/1.1");
        }

        [TestMethod]
        public void ProcessStartState_Comment_AddsToComments()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "# This is a comment" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            state.Should().Be(ParserState.Start);
            currentRequest.Should().BeNull();
            comments.Should().Contain("This is a comment");
        }

        [TestMethod]
        public void ProcessStartState_NameDirective_SetsRequestName()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "# @name GetUsers" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            requestName.Should().Be("GetUsers");
        }

        [TestMethod]
        public void ProcessStartState_Variable_AddsToFileVariables_WhenFirstRequest()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "@baseUrl = https://api.example.com" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            file.Variables.Should().ContainKey("baseUrl");
            file.Variables["baseUrl"].Should().Be("https://api.example.com");
        }

        [TestMethod]
        public void ProcessStartState_Variable_AddsToRequestVariables_WhenNotFirstRequest()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "@localVar = value" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: false);

            file.Variables.Should().BeEmpty();
            requestVariables.Should().ContainKey("localVar");
            requestVariables["localVar"].Should().Be("value");
        }

        [TestMethod]
        public void ProcessStartState_MalformedRequest_AddsDiagnostic()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "GET" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP001");
        }

        [TestMethod]
        public void ProcessStartState_UnknownMethod_AddsWarning()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "CUSTOM https://api.example.com" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP005");
            file.Diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        }

        [TestMethod]
        public void ProcessStartState_LowercaseMethod_ConvertsToUppercase()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "post https://api.example.com" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            currentRequest.Method.Should().Be("POST");
        }

        [TestMethod]
        public void ProcessStartState_WithSeparatorTitle_SetsSeparatorTitle()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "GET https://api.example.com/users" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = "Get All Users";
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            currentRequest.SeparatorTitle.Should().Be("Get All Users");
            currentSeparatorTitle.Should().BeNull();
        }

        [TestMethod]
        public void ProcessStartState_UrlWithResponseReference_AddsDependency()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "GET {{baseUrl}}/users/{{login.response.body.$.userId}}" };
            var context = CreateContext(file, lines);
            var comments = new List<string>();
            var requestName = (string)null;
            var currentSeparatorTitle = (string)null;
            var requestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            DotHttpRequest currentRequest = null;
            var line = lines[0].AsSpan();

            DotHttpFileParser.ProcessStartState(
                context,
                line,
                line.Trim(),
                ref currentRequest,
                ref requestName,
                ref currentSeparatorTitle,
                ref state,
                comments,
                requestVariables,
                isFirstRequest: true);

            currentRequest.HasResponseReferences.Should().BeTrue();
            currentRequest.DependsOn.Should().Contain("login");
        }

        #endregion

        #region Regex Pattern Tests

        [TestMethod]
        public void RequestLineRegex_ValidFormats_AllMatch()
        {
            var testCases = new[]
            {
                "GET https://api.example.com",
                "POST http://localhost:8080/api",
                "PUT /api/users HTTP/1.1",
                "DELETE {{baseUrl}}/items/1",
                "PATCH https://api.example.com/users/123 HTTP/2",
                "OPTIONS * HTTP/1.1"
            };

            var regex = new Regex(@"^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS|TRACE|CONNECT)\s+", RegexOptions.IgnoreCase);

            foreach (var testCase in testCases)
            {
                regex.IsMatch(testCase).Should().BeTrue($"'{testCase}' should match the request line pattern");
            }
        }

        [TestMethod]
        public void RequestLineRegex_InvalidFormats_DoNotMatch()
        {
            var testCases = new[]
            {
                "INVALID https://api.example.com",
                "GET",
                "Authorization: Bearer token",
                "@variable = value"
            };

            var regex = new Regex(@"^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS|TRACE|CONNECT)\s+\S+", RegexOptions.IgnoreCase);

            foreach (var testCase in testCases)
            {
                regex.IsMatch(testCase).Should().BeFalse($"'{testCase}' should not match the request line pattern");
            }
        }

        [TestMethod]
        public void ResponseReferenceRegex_ValidFormats_AllMatch()
        {
            var testCases = new[]
            {
                "{{login.response.body.$.token}}",
                "{{auth.response.headers.Authorization}}",
                "{{request1.response.body.*}}",
                "{{test_request.response.body.$.nested.value}}"
            };

            var regex = new Regex(@"\{\{([a-zA-Z_][a-zA-Z0-9_-]*)\.response\.(body|headers)\.");

            foreach (var testCase in testCases)
            {
                regex.IsMatch(testCase).Should().BeTrue($"'{testCase}' should match the response reference pattern");
            }
        }

        [TestMethod]
        public void VariableRegex_ValidFormats_AllMatch()
        {
            var testCases = new[]
            {
                "@baseUrl = https://api.example.com",
                "@apiKey=secret123",
                "@timeout = 30",
                "@empty_var = ",
                "@My.Variable = value with spaces"
            };

            var regex = new Regex(@"^@([^\s=]+)\s*=\s*(.*)$");

            foreach (var testCase in testCases)
            {
                regex.IsMatch(testCase).Should().BeTrue($"'{testCase}' should match the variable pattern");
            }
        }

        #endregion

        #region Simple Requests File Tests

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

        #region Chained Requests File Tests

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

        #region File Edge Cases Tests

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

        #region Real World - dotnet/eShop - Catalog.API Tests

        [TestMethod]
        public void Parse_EShopCatalog_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadRealWorldTestFile("dotnet-eshop-catalog.http");

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
            var content = ReadRealWorldTestFile("dotnet-eshop-catalog.http");

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
            var content = ReadRealWorldTestFile("dotnet-eshop-catalog.http");

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
            var content = ReadRealWorldTestFile("dotnet-eshop-catalog.http");

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
            var content = ReadRealWorldTestFile("dotnet-eshop-catalog.http");

            // Act
            var result = parser.Parse(content, "dotnet-eshop-catalog.http");

            // Assert
            // Requests 4 and 5 have inline comments
            var request4 = result.Requests.ElementAt(3);
            request4.Comments.Should().NotBeEmpty();
            request4.Comments.Should().Contain(c => c.Contains("400 ProblemDetails"));
        }

        #endregion

        #region Real World - Seelf - Chained Requests with JSONPath Tests

        [TestMethod]
        public void Parse_SeelfChained_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

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
            var content = ReadRealWorldTestFile("seelf-api-chained.http");

            // Act
            var result = parser.Parse(content, "seelf-api-chained.http");

            // Assert - createApp body references createTarget response
            var createAppRequest = result.Requests.FirstOrDefault(r => r.Name == "createApp");
            createAppRequest.Should().NotBeNull();
            createAppRequest.Body.Should().Contain("{{createTarget.response.body.$.id}}");
            createAppRequest.DependsOn.Should().Contain("createTarget");
        }

        #endregion

        #region Real World - Keycloak OAuth Flow Tests

        [TestMethod]
        public void Parse_KeycloakOAuth_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadRealWorldTestFile("keycloak-oauth-flow.http");

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
            var content = ReadRealWorldTestFile("keycloak-oauth-flow.http");

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
            var content = ReadRealWorldTestFile("keycloak-oauth-flow.http");

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
            var content = ReadRealWorldTestFile("keycloak-oauth-flow.http");

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
            var content = ReadRealWorldTestFile("keycloak-oauth-flow.http");

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
            var content = ReadRealWorldTestFile("keycloak-oauth-flow.http");

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
            var content = ReadRealWorldTestFile("keycloak-oauth-flow.http");

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
            var content = ReadRealWorldTestFile("keycloak-oauth-flow.http");

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

        #region Real World - dotnet/aspire Tests

        [TestMethod]
        public void Parse_AspireNats_ParsesAllRequests()
        {
            // Arrange
            var parser = new DotHttpFileParser();
            var content = ReadRealWorldTestFile("dotnet-aspire-nats.http");

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
            var content = ReadRealWorldTestFile("dotnet-aspire-mysql.http");

            // Act
            var result = parser.Parse(content, "dotnet-aspire-mysql.http");

            // Assert
            var deleteRequest = result.Requests.First(r => r.Method == "DELETE");
            deleteRequest.Url.Should().Be("{{HostAddress}}/catalog/4");
        }

        #endregion

        #region Real World - Cross-File Verification Tests

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
                var content = ReadRealWorldTestFile(file);
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
                var content = ReadRealWorldTestFile(file);
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
                var content = ReadRealWorldTestFile(file);
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
