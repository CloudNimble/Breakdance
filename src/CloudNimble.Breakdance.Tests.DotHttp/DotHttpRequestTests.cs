using System.Collections.Generic;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="DotHttpRequest"/> class.
    /// </summary>
    [TestClass]
    public class DotHttpRequestTests
    {

        #region Constructor Tests

        [TestMethod]
        public void DotHttpRequest_DefaultConstructor_InitializesCollections()
        {
            var request = new DotHttpRequest();

            request.Comments.Should().NotBeNull();
            request.Comments.Should().BeEmpty();
            request.DependsOn.Should().NotBeNull();
            request.DependsOn.Should().BeEmpty();
            request.Headers.Should().NotBeNull();
            request.Headers.Should().BeEmpty();
            request.Variables.Should().NotBeNull();
            request.Variables.Should().BeEmpty();
        }

        [TestMethod]
        public void DotHttpRequest_DefaultConstructor_HasNullStringProperties()
        {
            var request = new DotHttpRequest();

            request.Body.Should().BeNull();
            request.BodyFilePath.Should().BeNull();
            request.HttpVersion.Should().BeNull();
            request.Method.Should().BeNull();
            request.Name.Should().BeNull();
            request.Url.Should().BeNull();
        }

        [TestMethod]
        public void DotHttpRequest_DefaultConstructor_HasDefaultLineNumber()
        {
            var request = new DotHttpRequest();

            request.LineNumber.Should().Be(0);
        }

        [TestMethod]
        public void DotHttpRequest_DefaultConstructor_HasNoResponseReferences()
        {
            var request = new DotHttpRequest();

            request.HasResponseReferences.Should().BeFalse();
        }

        #endregion

        #region IsFileBody Property Tests

        [TestMethod]
        public void IsFileBody_ReturnsFalse_WhenBodyFilePathIsNull()
        {
            var request = new DotHttpRequest { BodyFilePath = null };

            request.IsFileBody.Should().BeFalse();
        }

        [TestMethod]
        public void IsFileBody_ReturnsFalse_WhenBodyFilePathIsEmpty()
        {
            var request = new DotHttpRequest { BodyFilePath = "" };

            request.IsFileBody.Should().BeFalse();
        }

        [TestMethod]
        public void IsFileBody_ReturnsTrue_WhenBodyFilePathIsSet()
        {
            var request = new DotHttpRequest { BodyFilePath = "./data.json" };

            request.IsFileBody.Should().BeTrue();
        }

        #endregion

        #region Headers Property Tests

        [TestMethod]
        public void Headers_AreCaseInsensitive()
        {
            var request = new DotHttpRequest();

            request.Headers["Content-Type"] = "application/json";
            request.Headers["content-type"].Should().Be("application/json");
            request.Headers["CONTENT-TYPE"].Should().Be("application/json");
        }

        #endregion

        #region Variables Property Tests

        [TestMethod]
        public void Variables_AreCaseInsensitive()
        {
            var request = new DotHttpRequest();

            request.Variables["BaseUrl"] = "https://example.com";
            request.Variables["baseurl"].Should().Be("https://example.com");
            request.Variables["BASEURL"].Should().Be("https://example.com");
        }

        #endregion

        #region Comments Property Tests

        [TestMethod]
        public void Comments_CanAddMultiple()
        {
            var request = new DotHttpRequest();

            request.Comments.Add("First comment");
            request.Comments.Add("Second comment");

            request.Comments.Should().HaveCount(2);
            request.Comments[0].Should().Be("First comment");
            request.Comments[1].Should().Be("Second comment");
        }

        #endregion

        #region DependsOn Property Tests

        [TestMethod]
        public void DependsOn_CanAddDependencies()
        {
            var request = new DotHttpRequest();

            request.DependsOn.Add("login");
            request.DependsOn.Add("getUser");

            request.DependsOn.Should().HaveCount(2);
            request.DependsOn.Should().Contain("login");
            request.DependsOn.Should().Contain("getUser");
        }

        #endregion

        #region Full Property Tests

        [TestMethod]
        public void AllProperties_CanBeSet()
        {
            var request = new DotHttpRequest
            {
                Body = "{\"name\": \"test\"}",
                BodyFilePath = null,
                HttpVersion = "HTTP/1.1",
                LineNumber = 10,
                Method = "POST",
                Name = "CreateUser",
                Url = "{{baseUrl}}/users",
                HasResponseReferences = true
            };

            request.Body.Should().Be("{\"name\": \"test\"}");
            request.BodyFilePath.Should().BeNull();
            request.HttpVersion.Should().Be("HTTP/1.1");
            request.LineNumber.Should().Be(10);
            request.Method.Should().Be("POST");
            request.Name.Should().Be("CreateUser");
            request.Url.Should().Be("{{baseUrl}}/users");
            request.HasResponseReferences.Should().BeTrue();
            request.IsFileBody.Should().BeFalse();
        }

        [TestMethod]
        public void CanReplaceCollections()
        {
            var request = new DotHttpRequest();
            var newComments = new List<string> { "New comment" };
            var newDependsOn = new List<string> { "dependency" };
            var newHeaders = new Dictionary<string, string> { { "Accept", "application/json" } };
            var newVariables = new Dictionary<string, string> { { "key", "value" } };

            request.Comments = newComments;
            request.DependsOn = newDependsOn;
            request.Headers = newHeaders;
            request.Variables = newVariables;

            request.Comments.Should().BeSameAs(newComments);
            request.DependsOn.Should().BeSameAs(newDependsOn);
            request.Headers.Should().BeSameAs(newHeaders);
            request.Variables.Should().BeSameAs(newVariables);
        }

        #endregion

    }

}
