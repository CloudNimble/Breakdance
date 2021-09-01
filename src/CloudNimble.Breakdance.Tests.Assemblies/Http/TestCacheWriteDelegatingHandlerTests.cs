using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies.Http;
using FluentAssertions;
using System.Threading;
using System.IO;
using System.Net.Http.Headers;

namespace CloudNimble.Breakdance.Tests.Assemblies.Http
{

    /// <summary>
    /// Tests the functionality of the <see cref="TestCacheWriteDelegatingHandler"/>.
    /// </summary>
    [TestClass]
    public class TestCacheWriteDelegatingHandlerTests
    {

        #region Private Properties

        /// <summary>
        /// Root directory for storing response files.
        /// </summary>
        private static string ResponseFilesPath => TestCacheDelegatingHandlerBaseTests.ResponseFilesPath;

        /// <summary>
        /// Local reference to test data is needed here or MSTest will choke.
        /// </summary>
        private static IEnumerable<object[]> GetPathsAndTestUris => TestCacheDelegatingHandlerBaseTests.GetPathsAndTestUris;

        #endregion

        [TestMethod]
        [DynamicData(nameof(GetPathsAndTestUris))]
        public async Task TestCacheWriteDelegatingHandler_CanWriteFile(string directoryPath, string fileName, string requestUri)
        {
            var handler = new TestCacheWriteDelegatingHandler(ResponseFilesPath);
            handler.InnerHandler = new FakeHttpResponseHandler();

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            File.Exists(Path.Combine(ResponseFilesPath, directoryPath, fileName)).Should().BeTrue();
        }

        [TestMethod]
        public async Task TestCacheWriteDelegatingHandler_FileExtension_ReflectsMediaType()
        {
            var handler = new TestCacheWriteDelegatingHandler(ResponseFilesPath);
            handler.InnerHandler = new FakeHttpResponseHandler();

            var request = new HttpRequestMessage(HttpMethod.Get, "https://services.odata.org/$metadata");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            File.Exists(Path.Combine(ResponseFilesPath, "services.odata.org", "$metadata.xml")).Should().BeTrue();
        }

        private class FakeHttpResponseHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent("{ }", Encoding.UTF8);
                return Task.FromResult(response);
            }
        }

    }
}
