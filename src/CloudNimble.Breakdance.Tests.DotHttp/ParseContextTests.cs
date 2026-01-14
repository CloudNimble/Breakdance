using System;
using CloudNimble.Breakdance.DotHttp;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="ParseContext"/> class.
    /// </summary>
    [TestClass]
    public class ParseContextTests
    {

        #region Constructor Tests

        [TestMethod]
        public void ParseContext_Constructor_SetsFileAndLines()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var lines = new[] { "GET /api", "Accept: application/json" };

            var context = new ParseContext(file, lines);

            context.File.Should().BeSameAs(file);
            context.Lines.Should().BeSameAs(lines);
        }

        #endregion

        #region LineIndex Property Tests

        [TestMethod]
        public void LineIndex_DefaultsToZero()
        {
            var file = new DotHttpFile();
            var lines = new[] { "line 1" };

            var context = new ParseContext(file, lines);

            context.LineIndex.Should().Be(0);
        }

        [TestMethod]
        public void LineIndex_CanBeModified()
        {
            var file = new DotHttpFile();
            var lines = new[] { "line 1", "line 2" };
            var context = new ParseContext(file, lines);

            context.LineIndex = 5;

            context.LineIndex.Should().Be(5);
            context.LineNumber.Should().Be(6);
        }

        #endregion

        #region LineNumber Property Tests

        [TestMethod]
        public void LineNumber_ReturnsOneBasedIndex()
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
        public void LineNumber_UpdatesWithLineIndex()
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

        #region File Property Tests

        [TestMethod]
        public void File_IsReadOnly()
        {
            var file = new DotHttpFile { FilePath = "test.http" };
            var context = new ParseContext(file, new string[0]);

            // File property is get-only
            context.File.Should().BeSameAs(file);
            context.File.FilePath.Should().Be("test.http");
        }

        #endregion

        #region Lines Property Tests

        [TestMethod]
        public void Lines_IsReadOnly()
        {
            var lines = new[] { "GET /api" };
            var context = new ParseContext(new DotHttpFile(), lines);

            // Lines property is get-only
            context.Lines.Should().BeSameAs(lines);
            context.Lines[0].Should().Be("GET /api");
        }

        #endregion

        #region Empty Lines Tests

        [TestMethod]
        public void WithEmptyLines_WorksCorrectly()
        {
            var file = new DotHttpFile();
            var lines = Array.Empty<string>();

            var context = new ParseContext(file, lines);

            context.Lines.Should().BeEmpty();
            context.LineIndex.Should().Be(0);
            context.LineNumber.Should().Be(1);
        }

        #endregion

    }

}
