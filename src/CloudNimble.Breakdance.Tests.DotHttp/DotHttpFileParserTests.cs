using System;
using CloudNimble.Breakdance.DotHttp;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Unit tests for the <see cref="DotHttpFileParser"/> class.
    /// </summary>
    [TestClass]
    public class DotHttpFileParserTests
    {

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

        #region Edge Cases

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

    }

}
