using System.Collections.Generic;
using CloudNimble.Breakdance.DotHttp;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="DotHttpFile"/> class.
    /// </summary>
    [TestClass]
    public class DotHttpFileTests
    {

        #region Constructor Tests

        [TestMethod]
        public void DotHttpFile_DefaultConstructor_InitializesCollections()
        {
            var file = new DotHttpFile();

            file.Diagnostics.Should().NotBeNull();
            file.Diagnostics.Should().BeEmpty();
            file.Requests.Should().NotBeNull();
            file.Requests.Should().BeEmpty();
            file.Variables.Should().NotBeNull();
            file.Variables.Should().BeEmpty();
        }

        [TestMethod]
        public void DotHttpFile_DefaultConstructor_HasNullFilePath()
        {
            var file = new DotHttpFile();

            file.FilePath.Should().BeNull();
        }

        #endregion

        #region HasChainedRequests Property Tests

        [TestMethod]
        public void HasChainedRequests_ReturnsFalse_WhenNoRequests()
        {
            var file = new DotHttpFile();

            file.HasChainedRequests.Should().BeFalse();
        }

        [TestMethod]
        public void HasChainedRequests_ReturnsFalse_WhenNoRequestsHaveReferences()
        {
            var file = new DotHttpFile();
            file.Requests.Add(new DotHttpRequest { Name = "request1", HasResponseReferences = false });
            file.Requests.Add(new DotHttpRequest { Name = "request2", HasResponseReferences = false });

            file.HasChainedRequests.Should().BeFalse();
        }

        [TestMethod]
        public void HasChainedRequests_ReturnsTrue_WhenAnyRequestHasReferences()
        {
            var file = new DotHttpFile();
            file.Requests.Add(new DotHttpRequest { Name = "request1", HasResponseReferences = false });
            file.Requests.Add(new DotHttpRequest { Name = "request2", HasResponseReferences = true });

            file.HasChainedRequests.Should().BeTrue();
        }

        [TestMethod]
        public void HasChainedRequests_ReturnsTrue_WhenAllRequestsHaveReferences()
        {
            var file = new DotHttpFile();
            file.Requests.Add(new DotHttpRequest { Name = "request1", HasResponseReferences = true });
            file.Requests.Add(new DotHttpRequest { Name = "request2", HasResponseReferences = true });

            file.HasChainedRequests.Should().BeTrue();
        }

        #endregion

        #region Variables Property Tests

        [TestMethod]
        public void Variables_AreCaseSensitive()
        {
            var file = new DotHttpFile();

            file.Variables["baseUrl"] = "https://lower.com";
            file.Variables["BaseUrl"] = "https://upper.com";

            file.Variables.Should().HaveCount(2);
            file.Variables["baseUrl"].Should().Be("https://lower.com");
            file.Variables["BaseUrl"].Should().Be("https://upper.com");
        }

        #endregion

        #region Diagnostics Property Tests

        [TestMethod]
        public void CanAddDiagnostics()
        {
            var file = new DotHttpFile { FilePath = "test.http" };

            var diagnostic = Diagnostic.Create(
                DotHttpFileParser.RequestLineErrorDescriptor,
                Location.Create(
                    "test.http",
                    new TextSpan(0, 0),
                    new LinePositionSpan(new LinePosition(4, 0), new LinePosition(4, 1))),
                "Test error");

            file.Diagnostics.Add(diagnostic);

            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP001");
        }

        #endregion

        #region Requests Property Tests

        [TestMethod]
        public void CanAddRequests()
        {
            var file = new DotHttpFile();

            file.Requests.Add(new DotHttpRequest { Method = "GET", Url = "/users" });
            file.Requests.Add(new DotHttpRequest { Method = "POST", Url = "/users" });

            file.Requests.Should().HaveCount(2);
        }

        #endregion

        #region Full Property Tests

        [TestMethod]
        public void AllProperties_CanBeSet()
        {
            var file = new DotHttpFile
            {
                FilePath = "api.http"
            };
            file.Variables["baseUrl"] = "https://api.example.com";
            file.Requests.Add(new DotHttpRequest { Method = "GET", Url = "{{baseUrl}}/users" });

            var diagnostic = Diagnostic.Create(
                DotHttpFileParser.BodyWarningDescriptor,
                Location.None,
                "Warning");
            file.Diagnostics.Add(diagnostic);

            file.FilePath.Should().Be("api.http");
            file.Variables.Should().ContainKey("baseUrl");
            file.Requests.Should().HaveCount(1);
            file.Diagnostics.Should().HaveCount(1);
        }

        [TestMethod]
        public void CanReplaceCollections()
        {
            var file = new DotHttpFile();
            var newDiagnostics = new List<Diagnostic>();
            var newRequests = new List<DotHttpRequest>();
            var newVariables = new Dictionary<string, string>();

            file.Diagnostics = newDiagnostics;
            file.Requests = newRequests;
            file.Variables = newVariables;

            file.Diagnostics.Should().BeSameAs(newDiagnostics);
            file.Requests.Should().BeSameAs(newRequests);
            file.Variables.Should().BeSameAs(newVariables);
        }

        #endregion

    }

}
