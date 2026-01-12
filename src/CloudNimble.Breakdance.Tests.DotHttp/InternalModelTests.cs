using System;
using CloudNimble.Breakdance.DotHttp;
using CloudNimble.Breakdance.DotHttp.Generator;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for internal DotHttp classes.
    /// </summary>
    [TestClass]
    public class InternalModelTests
    {

        #region ParserState Tests

        [TestMethod]
        public void ParserState_Start_HasCorrectValue()
        {
            ParserState.Start.Should().Be((ParserState)0);
        }

        [TestMethod]
        public void ParserState_InHeaders_HasCorrectValue()
        {
            ParserState.InHeaders.Should().Be((ParserState)1);
        }

        [TestMethod]
        public void ParserState_InBody_HasCorrectValue()
        {
            ParserState.InBody.Should().Be((ParserState)2);
        }

        [TestMethod]
        public void ParserState_HasExpectedMembers()
        {
            var values = (ParserState[])Enum.GetValues(typeof(ParserState));

            values.Should().HaveCount(3);
            values.Should().Contain(ParserState.Start);
            values.Should().Contain(ParserState.InHeaders);
            values.Should().Contain(ParserState.InBody);
        }

        #endregion

        #region DotHttpConfig Tests

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

        [TestMethod]
        public void DotHttpConfig_BasePath_CanBeSet()
        {
            var config = new DotHttpConfig { BasePath = "HttpRequests" };

            config.BasePath.Should().Be("HttpRequests");
        }

        [TestMethod]
        public void DotHttpConfig_CheckBodyForErrors_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { CheckBodyForErrors = false };

            config.CheckBodyForErrors.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpConfig_CheckContentType_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { CheckContentType = false };

            config.CheckContentType.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpConfig_CheckStatusCode_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { CheckStatusCode = false };

            config.CheckStatusCode.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpConfig_Environment_CanBeSet()
        {
            var config = new DotHttpConfig { Environment = "prod" };

            config.Environment.Should().Be("prod");
        }

        [TestMethod]
        public void DotHttpConfig_HttpClientType_CanBeSet()
        {
            var config = new DotHttpConfig { HttpClientType = "MyProject.CustomHttpClient" };

            config.HttpClientType.Should().Be("MyProject.CustomHttpClient");
        }

        [TestMethod]
        public void DotHttpConfig_LogResponseOnFailure_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { LogResponseOnFailure = false };

            config.LogResponseOnFailure.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpConfig_Namespace_CanBeSet()
        {
            var config = new DotHttpConfig { Namespace = "MyProject.Tests" };

            config.Namespace.Should().Be("MyProject.Tests");
        }

        [TestMethod]
        public void DotHttpConfig_TestFramework_CanBeSetToXUnit()
        {
            var config = new DotHttpConfig { TestFramework = "XUnit" };

            config.TestFramework.Should().Be("XUnit");
        }

        [TestMethod]
        public void DotHttpConfig_UseFluentAssertions_CanBeSetToFalse()
        {
            var config = new DotHttpConfig { UseFluentAssertions = false };

            config.UseFluentAssertions.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpConfig_AllProperties_CanBeSetTogether()
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

        #region ParseContext Tests

        [TestMethod]
        public void ParseContext_Constructor_SetsFileAndLines()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "GET /api", "Accept: application/json" };

            var context = new ParseContext(file, lines);

            context.File.Should().BeSameAs(file);
            context.Lines.Should().BeSameAs(lines);
        }

        [TestMethod]
        public void ParseContext_LineIndex_DefaultsToZero()
        {
            var file = new DotHttpFile();
            var lines = new[] { "line 1" };

            var context = new ParseContext(file, lines);

            context.LineIndex.Should().Be(0);
        }

        [TestMethod]
        public void ParseContext_LineNumber_ReturnsOneBasedIndex()
        {
            var file = new DotHttpFile();
            var lines = new[] { "line 1", "line 2", "line 3" };
            var context = new ParseContext(file, lines);

            context.LineIndex = 0;
            context.LineNumber.Should().Be(1);

            context.LineIndex = 1;
            context.LineNumber.Should().Be(2);

            context.LineIndex = 2;
            context.LineNumber.Should().Be(3);
        }

        [TestMethod]
        public void ParseContext_LineIndex_CanBeModified()
        {
            var file = new DotHttpFile();
            var lines = new[] { "line 1", "line 2" };
            var context = new ParseContext(file, lines);

            context.LineIndex = 5;

            context.LineIndex.Should().Be(5);
            context.LineNumber.Should().Be(6);
        }

        [TestMethod]
        public void ParseContext_File_IsReadOnly()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new string[0]);

            // File property is get-only
            context.File.Should().BeSameAs(file);
            context.File.FilePath.Should().Be("test.http");
        }

        [TestMethod]
        public void ParseContext_Lines_IsReadOnly()
        {
            var lines = new[] { "GET /api" };
            var context = new ParseContext(new DotHttpFile(), lines);

            // Lines property is get-only
            context.Lines.Should().BeSameAs(lines);
            context.Lines[0].Should().Be("GET /api");
        }

        [TestMethod]
        public void ParseContext_WithEmptyLines_WorksCorrectly()
        {
            var file = new DotHttpFile();
            var lines = Array.Empty<string>();

            var context = new ParseContext(file, lines);

            context.Lines.Should().BeEmpty();
            context.LineIndex.Should().Be(0);
            context.LineNumber.Should().Be(1);
        }

        [TestMethod]
        public void ParseContext_LineNumber_UpdatesWithLineIndex()
        {
            var file = new DotHttpFile();
            var lines = new[] { "a", "b", "c", "d", "e" };
            var context = new ParseContext(file, lines);

            for (int i = 0; i < lines.Length; i++)
            {
                context.LineIndex = i;
                context.LineNumber.Should().Be(i + 1);
            }
        }

        #endregion

    }

}
