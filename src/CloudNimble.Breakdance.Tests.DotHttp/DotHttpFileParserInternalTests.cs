using System;
using System.Collections.Generic;
using CloudNimble.Breakdance.DotHttp;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Unit tests for the internal methods of <see cref="DotHttpFileParser"/>.
    /// </summary>
    [TestClass]
    public class DotHttpFileParserInternalTests
    {

        #region SplitLines Tests

        [TestMethod]
        public void SplitLines_NullContent_ReturnsEmptyArray()
        {
            // Act
            var result = DotHttpFileParser.SplitLines(null);

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void SplitLines_EmptyContent_ReturnsEmptyArray()
        {
            // Act
            var result = DotHttpFileParser.SplitLines(string.Empty);

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void SplitLines_SingleLine_ReturnsSingleElement()
        {
            // Act
            var result = DotHttpFileParser.SplitLines("GET https://example.com");

            // Assert
            result.Should().HaveCount(1);
            result[0].Should().Be("GET https://example.com");
        }

        [TestMethod]
        public void SplitLines_WindowsLineEndings_SplitsCorrectly()
        {
            // Act
            var result = DotHttpFileParser.SplitLines("line1\r\nline2\r\nline3");

            // Assert
            result.Should().HaveCount(3);
            result[0].Should().Be("line1");
            result[1].Should().Be("line2");
            result[2].Should().Be("line3");
        }

        [TestMethod]
        public void SplitLines_UnixLineEndings_SplitsCorrectly()
        {
            // Act
            var result = DotHttpFileParser.SplitLines("line1\nline2\nline3");

            // Assert
            result.Should().HaveCount(3);
        }

        [TestMethod]
        public void SplitLines_OldMacLineEndings_SplitsCorrectly()
        {
            // Act
            var result = DotHttpFileParser.SplitLines("line1\rline2\rline3");

            // Assert
            result.Should().HaveCount(3);
        }

        [TestMethod]
        public void SplitLines_TrailingNewline_IncludesEmptyElement()
        {
            // Act
            var result = DotHttpFileParser.SplitLines("line1\nline2\n");

            // Assert
            result.Should().HaveCount(3);
            result[2].Should().BeEmpty();
        }

        #endregion

        #region AddDiagnostic Tests

        [TestMethod]
        public void AddDiagnostic_AddsDiagnosticToContext()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "line1", "line2" });
            context.LineIndex = 1;

            // Act
            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.RequestLineErrorDescriptor, "Test error message");

            // Assert
            file.Diagnostics.Should().HaveCount(1);
            var diagnostic = file.Diagnostics[0];
            diagnostic.Id.Should().Be("DOTHTTP001");
            diagnostic.GetMessage().Should().Be("Test error message");
            diagnostic.Location.GetLineSpan().StartLinePosition.Line.Should().Be(1); // 0-based
            diagnostic.Location.GetLineSpan().StartLinePosition.Character.Should().Be(0);
            diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        }

        [TestMethod]
        public void AddDiagnostic_WithWarningDescriptor_SetsSeverityCorrectly()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "line1" });

            // Act
            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.BodyWarningDescriptor, "Warning message");

            // Assert
            file.Diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        }

        [TestMethod]
        public void AddDiagnostic_WithCustomColumn_SetsColumnCorrectly()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "line1" });

            // Act
            DotHttpFileParser.AddDiagnostic(context, DotHttpFileParser.VariableErrorDescriptor, "Error at column", 15);

            // Assert
            file.Diagnostics[0].Location.GetLineSpan().StartLinePosition.Character.Should().Be(14); // 0-based
        }

        #endregion

        #region CheckForResponseReferences Tests

        [TestMethod]
        public void CheckForResponseReferences_EmptyText_DoesNothing()
        {
            // Arrange
            var request = new DotHttpRequest();

            // Act
            DotHttpFileParser.CheckForResponseReferences(request, ReadOnlySpan<char>.Empty);

            // Assert
            request.HasResponseReferences.Should().BeFalse();
            request.DependsOn.Should().BeEmpty();
        }

        [TestMethod]
        public void CheckForResponseReferences_NoReferences_DoesNothing()
        {
            // Arrange
            var request = new DotHttpRequest();
            var text = "https://api.example.com/users".AsSpan();

            // Act
            DotHttpFileParser.CheckForResponseReferences(request, text);

            // Assert
            request.HasResponseReferences.Should().BeFalse();
            request.DependsOn.Should().BeEmpty();
        }

        [TestMethod]
        public void CheckForResponseReferences_WithBodyReference_SetsFlag()
        {
            // Arrange
            var request = new DotHttpRequest();
            var text = "{{login.response.body.$.token}}".AsSpan();

            // Act
            DotHttpFileParser.CheckForResponseReferences(request, text);

            // Assert
            request.HasResponseReferences.Should().BeTrue();
            request.DependsOn.Should().Contain("login");
        }

        [TestMethod]
        public void CheckForResponseReferences_WithHeaderReference_SetsFlag()
        {
            // Arrange
            var request = new DotHttpRequest();
            var text = "{{auth.response.headers.X-Request-Id}}".AsSpan();

            // Act
            DotHttpFileParser.CheckForResponseReferences(request, text);

            // Assert
            request.HasResponseReferences.Should().BeTrue();
            request.DependsOn.Should().Contain("auth");
        }

        [TestMethod]
        public void CheckForResponseReferences_WithRequestReference_SetsFlag()
        {
            // Arrange
            var request = new DotHttpRequest();
            var text = "{{prev.request.body.$.data}}".AsSpan();

            // Act
            DotHttpFileParser.CheckForResponseReferences(request, text);

            // Assert
            request.HasResponseReferences.Should().BeTrue();
            request.DependsOn.Should().Contain("prev");
        }

        [TestMethod]
        public void CheckForResponseReferences_MultipleReferences_AddsAllDependencies()
        {
            // Arrange
            var request = new DotHttpRequest();
            var text = "{{login.response.body.$.token}} {{user.response.body.$.id}}".AsSpan();

            // Act
            DotHttpFileParser.CheckForResponseReferences(request, text);

            // Assert
            request.HasResponseReferences.Should().BeTrue();
            request.DependsOn.Should().HaveCount(2);
            request.DependsOn.Should().Contain("login");
            request.DependsOn.Should().Contain("user");
        }

        [TestMethod]
        public void CheckForResponseReferences_DuplicateReferences_DoesNotDuplicate()
        {
            // Arrange
            var request = new DotHttpRequest();
            var text = "{{login.response.body.$.token}} {{login.response.body.$.refresh}}".AsSpan();

            // Act
            DotHttpFileParser.CheckForResponseReferences(request, text);

            // Assert
            request.DependsOn.Should().HaveCount(1);
        }

        [TestMethod]
        public void CheckForResponseReferences_SimpleVariable_DoesNotMatch()
        {
            // Arrange
            var request = new DotHttpRequest();
            var text = "{{baseUrl}}".AsSpan();

            // Act
            DotHttpFileParser.CheckForResponseReferences(request, text);

            // Assert
            request.HasResponseReferences.Should().BeFalse();
            request.DependsOn.Should().BeEmpty();
        }

        #endregion

        #region FinishRequest Tests

        [TestMethod]
        public void FinishRequest_EmptyBodyLines_DoesNotSetBody()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string>();

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.Body.Should().BeNull();
        }

        [TestMethod]
        public void FinishRequest_WhitespaceOnlyLines_DoesNotSetBody()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "   ", "\t", "" };

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.Body.Should().BeNull();
        }

        [TestMethod]
        public void FinishRequest_WithFileReference_SetsBodyFilePath()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "< ./data.json" };

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.BodyFilePath.Should().Be("./data.json");
            request.Body.Should().BeNull();
        }

        [TestMethod]
        public void FinishRequest_FileReferenceWithSpaces_TrimsPath()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "<    ./data.json   " };

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.BodyFilePath.Should().Be("./data.json");
        }

        [TestMethod]
        public void FinishRequest_FileReferenceWithExtraContent_AddsWarning()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "< ./data.json", "extra" });
            context.LineIndex = 0;
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "< ./data.json", "extra content" };

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.BodyFilePath.Should().Be("./data.json");
            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP004");
        }

        [TestMethod]
        public void FinishRequest_RegularBody_SetsBody()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "{", "  \"name\": \"John\"", "}" };

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.Body.Should().Be("{\n  \"name\": \"John\"\n}");
        }

        [TestMethod]
        public void FinishRequest_BodyWithResponseReferences_ChecksForReferences()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "{\"token\": \"{{login.response.body.$.token}}\"}" };

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.HasResponseReferences.Should().BeTrue();
            request.DependsOn.Should().Contain("login");
        }

        [TestMethod]
        public void FinishRequest_FilePathWithResponseReference_ChecksForReferences()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "< ./{{config.response.body.$.file}}" };

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.HasResponseReferences.Should().BeTrue();
            request.DependsOn.Should().Contain("config");
        }

        [TestMethod]
        public void FinishRequest_EmptyFileReference_DoesNotSetPath()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "<" };

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.BodyFilePath.Should().BeNull();
            // The line "< " with just the marker and whitespace should be treated as body
            request.Body.Should().Be("<");
        }

        [TestMethod]
        public void FinishRequest_FileReferenceWithOnlyWhitespace_TreatsAsBody()
        {
            // Arrange - covers line 186 (empty filePath falls through to body handling)
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "<    " }; // < followed by only whitespace

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.BodyFilePath.Should().BeNull();
            request.IsFileBody.Should().BeFalse();
            // Should be treated as regular body content (body preserves original content)
            request.Body.Should().Be("<    ");
        }

        [TestMethod]
        public void FinishRequest_FileReferenceMarkerOnly_TreatsAsBody()
        {
            // Arrange - edge case: single < character
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, Array.Empty<string>());
            var request = new DotHttpRequest();
            var bodyLines = new List<string> { "< " }; // < with single space

            // Act
            DotHttpFileParser.FinishRequest(context, request, bodyLines);

            // Assert
            request.BodyFilePath.Should().BeNull();
            request.Body.Should().Be("< ");
        }

        #endregion

        #region ProcessHeaderState Tests

        [TestMethod]
        public void ProcessHeaderState_EmptyLine_TransitionsToBody()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "" });
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;

            // Act
            DotHttpFileParser.ProcessHeaderState(context, "".AsSpan(), "".AsSpan(), request, ref state);

            // Assert
            state.Should().Be(ParserState.InBody);
        }

        [TestMethod]
        public void ProcessHeaderState_ValidHeader_AddsToHeaders()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "Content-Type: application/json" });
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = "Content-Type: application/json".AsSpan();

            // Act
            DotHttpFileParser.ProcessHeaderState(context, line, line, request, ref state);

            // Assert
            request.Headers.Should().ContainKey("Content-Type");
            request.Headers["Content-Type"].Should().Be("application/json");
            state.Should().Be(ParserState.InHeaders);
        }

        [TestMethod]
        public void ProcessHeaderState_HeaderContinuation_AppendsToLastHeader()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { " continued value" });
            var request = new DotHttpRequest();
            request.Headers["Authorization"] = "Bearer token";
            var state = ParserState.InHeaders;
            var line = " continued value".AsSpan();
            var trimmedLine = "continued value".AsSpan();

            // Act
            DotHttpFileParser.ProcessHeaderState(context, line, trimmedLine, request, ref state);

            // Assert
            request.Headers["Authorization"].Should().Be("Bearer token continued value");
        }

        [TestMethod]
        public void ProcessHeaderState_HeaderWithResponseReference_ChecksForReferences()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "Authorization: Bearer {{login.response.body.$.token}}" });
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = "Authorization: Bearer {{login.response.body.$.token}}".AsSpan();

            // Act
            DotHttpFileParser.ProcessHeaderState(context, line, line, request, ref state);

            // Assert
            request.HasResponseReferences.Should().BeTrue();
            request.DependsOn.Should().Contain("login");
        }

        [TestMethod]
        public void ProcessHeaderState_InvalidHeaderNoColon_AddsError()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "InvalidHeader" });
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = "InvalidHeader".AsSpan();

            // Act
            DotHttpFileParser.ProcessHeaderState(context, line, line, request, ref state);

            // Assert
            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP002");
        }

        [TestMethod]
        public void ProcessHeaderState_EmptyHeaderName_AddsError()
        {
            // Arrange - covers lines 321-324 (empty header name after trimming)
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "   : value" });
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = "   : value".AsSpan();
            var trimmedLine = ": value".AsSpan();

            // Act
            DotHttpFileParser.ProcessHeaderState(context, line, trimmedLine, request, ref state);

            // Assert
            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP002");
            file.Diagnostics[0].GetMessage().Should().Contain("empty header name");
            request.Headers.Should().BeEmpty();
        }

        [TestMethod]
        public void ProcessHeaderState_ColonAtStart_AddsInvalidFormatError()
        {
            // Arrange - colon at position 0, colonIndex = 0 which fails (colonIndex > 0)
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { ": value" });
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = ": value".AsSpan();

            // Act
            DotHttpFileParser.ProcessHeaderState(context, line, line, request, ref state);

            // Assert
            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP002");
            file.Diagnostics[0].GetMessage().Should().Contain("Invalid header format");
        }

        [TestMethod]
        public void ProcessHeaderState_HeaderContinuationWithResponseReference_ChecksForReferences()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { " {{login.response.body.$.more}}" });
            var request = new DotHttpRequest();
            request.Headers["X-Custom"] = "value";
            var state = ParserState.InHeaders;
            var line = " {{login.response.body.$.more}}".AsSpan();
            var trimmedLine = "{{login.response.body.$.more}}".AsSpan();

            // Act
            DotHttpFileParser.ProcessHeaderState(context, line, trimmedLine, request, ref state);

            // Assert
            request.HasResponseReferences.Should().BeTrue();
        }

        [TestMethod]
        public void ProcessHeaderState_WhitespaceLineNoHeaders_FallsThroughToHeaderParsing()
        {
            // Arrange - line starts with whitespace but no headers exist yet
            // This bypasses the multiline continuation check (Headers.Count > 0 is false)
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "  Content-Type: application/json" });
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = "  Content-Type: application/json".AsSpan();
            var trimmedLine = "Content-Type: application/json".AsSpan();

            // Act
            DotHttpFileParser.ProcessHeaderState(context, line, trimmedLine, request, ref state);

            // Assert
            // Since colonIndex would be based on the original line (which has leading whitespace)
            // The header should still be parsed correctly
            request.Headers.Should().ContainKey("Content-Type");
            request.Headers["Content-Type"].Should().Be("application/json");
        }

        [TestMethod]
        public void ProcessHeaderState_WhitespaceLineNoHeaders_InvalidHeader()
        {
            // Arrange - line starts with whitespace, no headers, and no colon
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "  InvalidContent" });
            var request = new DotHttpRequest();
            var state = ParserState.InHeaders;
            var line = "  InvalidContent".AsSpan();
            var trimmedLine = "InvalidContent".AsSpan();

            // Act
            DotHttpFileParser.ProcessHeaderState(context, line, trimmedLine, request, ref state);

            // Assert
            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP002");
        }

        #endregion

        #region ProcessStartState Tests

        [TestMethod]
        public void ProcessStartState_EmptyLine_DoesNothing()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "" });
            DotHttpRequest currentRequest = null;
            string currentRequestName = null;
            var state = ParserState.Start;
            var comments = new List<string>();
            var variables = new Dictionary<string, string>();

            // Act
            DotHttpFileParser.ProcessStartState(
                context, "".AsSpan(), "".AsSpan(),
                ref currentRequest, ref currentRequestName, ref state,
                comments, variables, true);

            // Assert
            currentRequest.Should().BeNull();
            state.Should().Be(ParserState.Start);
        }

        [TestMethod]
        public void ProcessStartState_FileLevelVariable_AddsToFileVariables()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "@baseUrl = https://example.com" });
            DotHttpRequest currentRequest = null;
            string currentRequestName = null;
            var state = ParserState.Start;
            var comments = new List<string>();
            var variables = new Dictionary<string, string>();
            var line = "@baseUrl = https://example.com".AsSpan();

            // Act
            DotHttpFileParser.ProcessStartState(
                context, line, line,
                ref currentRequest, ref currentRequestName, ref state,
                comments, variables, true);

            // Assert
            file.Variables.Should().ContainKey("baseUrl");
            file.Variables["baseUrl"].Should().Be("https://example.com");
        }

        [TestMethod]
        public void ProcessStartState_RequestLevelVariable_AddsToRequestVariables()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "@baseUrl = https://override.com" });
            DotHttpRequest currentRequest = null;
            string currentRequestName = null;
            var state = ParserState.Start;
            var comments = new List<string>();
            var variables = new Dictionary<string, string>();
            var line = "@baseUrl = https://override.com".AsSpan();

            // Act
            DotHttpFileParser.ProcessStartState(
                context, line, line,
                ref currentRequest, ref currentRequestName, ref state,
                comments, variables, false);

            // Assert
            file.Variables.Should().BeEmpty();
            variables.Should().ContainKey("baseUrl");
        }

        [TestMethod]
        public void ProcessStartState_RequestNameDirective_SetsCurrentName()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "# @name GetUsers" });
            DotHttpRequest currentRequest = null;
            string currentRequestName = null;
            var state = ParserState.Start;
            var comments = new List<string>();
            var variables = new Dictionary<string, string>();
            var line = "# @name GetUsers".AsSpan();

            // Act
            DotHttpFileParser.ProcessStartState(
                context, line, line,
                ref currentRequest, ref currentRequestName, ref state,
                comments, variables, true);

            // Assert
            currentRequestName.Should().Be("GetUsers");
        }

        [TestMethod]
        public void ProcessStartState_RegularComment_AddsToComments()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "# This is a comment" });
            DotHttpRequest currentRequest = null;
            string currentRequestName = null;
            var state = ParserState.Start;
            var comments = new List<string>();
            var variables = new Dictionary<string, string>();
            var line = "# This is a comment".AsSpan();

            // Act
            DotHttpFileParser.ProcessStartState(
                context, line, line,
                ref currentRequest, ref currentRequestName, ref state,
                comments, variables, true);

            // Assert
            comments.Should().Contain("This is a comment");
        }

        [TestMethod]
        public void ProcessStartState_ValidRequestLine_CreatesRequest()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "GET https://api.example.com/users" });
            DotHttpRequest currentRequest = null;
            string currentRequestName = "TestRequest";
            var state = ParserState.Start;
            var comments = new List<string> { "Test comment" };
            var variables = new Dictionary<string, string> { { "key", "value" } };
            var line = "GET https://api.example.com/users".AsSpan();

            // Act
            DotHttpFileParser.ProcessStartState(
                context, line, line,
                ref currentRequest, ref currentRequestName, ref state,
                comments, variables, true);

            // Assert
            currentRequest.Should().NotBeNull();
            currentRequest.Method.Should().Be("GET");
            currentRequest.Url.Should().Be("https://api.example.com/users");
            currentRequest.Name.Should().Be("TestRequest");
            currentRequest.Comments.Should().Contain("Test comment");
            currentRequest.Variables.Should().ContainKey("key");
            state.Should().Be(ParserState.InHeaders);
            currentRequestName.Should().BeNull(); // Cleared after use
            comments.Should().BeEmpty(); // Cleared after use
        }

        [TestMethod]
        public void ProcessStartState_RequestWithResponseReference_ChecksForReferences()
        {
            // Arrange
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new[] { "GET https://api.example.com/users/{{login.response.body.$.id}}" });
            DotHttpRequest currentRequest = null;
            string currentRequestName = null;
            var state = ParserState.Start;
            var comments = new List<string>();
            var variables = new Dictionary<string, string>();
            var line = "GET https://api.example.com/users/{{login.response.body.$.id}}".AsSpan();

            // Act
            DotHttpFileParser.ProcessStartState(
                context, line, line,
                ref currentRequest, ref currentRequestName, ref state,
                comments, variables, true);

            // Assert
            currentRequest.HasResponseReferences.Should().BeTrue();
            currentRequest.DependsOn.Should().Contain("login");
        }

        #endregion

        #region Regex Pattern Tests

        [TestMethod]
        public void RequestLineRegex_SimpleRequest_Matches()
        {
            // Arrange
            var input = "GET https://api.example.com/users";

            // Act
            var match = DotHttpFileParser.RequestLineRegex().Match(input);

            // Assert
            match.Success.Should().BeTrue();
            match.Groups[1].Value.Should().Be("GET");
            match.Groups[2].Value.Should().Be("https://api.example.com/users");
            match.Groups[3].Success.Should().BeFalse();
        }

        [TestMethod]
        public void RequestLineRegex_RequestWithVersion_Matches()
        {
            // Arrange
            var input = "POST https://api.example.com/users HTTP/1.1";

            // Act
            var match = DotHttpFileParser.RequestLineRegex().Match(input);

            // Assert
            match.Success.Should().BeTrue();
            match.Groups[1].Value.Should().Be("POST");
            match.Groups[2].Value.Should().Be("https://api.example.com/users");
            match.Groups[3].Value.Should().Be("HTTP/1.1");
        }

        [TestMethod]
        public void RequestNameRegex_HashStyle_Matches()
        {
            // Arrange
            var input = "# @name GetAllUsers";

            // Act
            var match = DotHttpFileParser.RequestNameRegex().Match(input);

            // Assert
            match.Success.Should().BeTrue();
            match.Groups[1].Value.Should().Be("GetAllUsers");
        }

        [TestMethod]
        public void RequestNameRegex_DoubleSlashStyle_Matches()
        {
            // Arrange
            var input = "// @name CreateUser";

            // Act
            var match = DotHttpFileParser.RequestNameRegex().Match(input);

            // Assert
            match.Success.Should().BeTrue();
            match.Groups[1].Value.Should().Be("CreateUser");
        }

        [TestMethod]
        public void RequestNameRegex_WithHyphen_Matches()
        {
            // Arrange
            var input = "# @name get-all-users";

            // Act
            var match = DotHttpFileParser.RequestNameRegex().Match(input);

            // Assert
            match.Success.Should().BeTrue();
            match.Groups[1].Value.Should().Be("get-all-users");
        }

        [TestMethod]
        public void ResponseReferenceRegex_BodyJsonPath_Matches()
        {
            // Arrange
            var input = "{{login.response.body.$.token}}";

            // Act
            var match = DotHttpFileParser.ResponseReferenceRegex().Match(input);

            // Assert
            match.Success.Should().BeTrue();
            match.Groups[1].Value.Should().Be("login");
            match.Groups[2].Value.Should().Be("response");
            match.Groups[3].Value.Should().Be("body");
            match.Groups[4].Value.Should().Be("$.token");
        }

        [TestMethod]
        public void ResponseReferenceRegex_HeaderReference_Matches()
        {
            // Arrange
            var input = "{{auth.response.headers.X-Request-Id}}";

            // Act
            var match = DotHttpFileParser.ResponseReferenceRegex().Match(input);

            // Assert
            match.Success.Should().BeTrue();
            match.Groups[1].Value.Should().Be("auth");
            match.Groups[3].Value.Should().Be("headers");
            match.Groups[4].Value.Should().Be("X-Request-Id");
        }

        [TestMethod]
        public void ResponseReferenceRegex_RequestBody_Matches()
        {
            // Arrange
            var input = "{{prev.request.body.$.data}}";

            // Act
            var match = DotHttpFileParser.ResponseReferenceRegex().Match(input);

            // Assert
            match.Success.Should().BeTrue();
            match.Groups[2].Value.Should().Be("request");
        }

        #endregion

    }

}
