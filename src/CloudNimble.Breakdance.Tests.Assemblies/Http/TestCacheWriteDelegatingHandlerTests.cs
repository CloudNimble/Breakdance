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

        /// <summary>
        /// Tests that the <see cref="TestCacheWriteDelegatingHandler"/> can parse the provided set of URIs and read the associated file.
        /// </summary>
        /// <param name="directoryPath">Folder containing test cache file.</param>
        /// <param name="fileName">Test cache file name.</param>
        /// <param name="requestUri">URI to parse.</param>
        /// <returns></returns>
        /// <remarks>If you have not already generated the files for this test, you should run the TestCacheWriteDelegatingHandler_CanWriteFile test first.</remarks>
        [TestMethod]
        [DynamicData(nameof(GetPathsAndTestUris))]
        public async Task TestCacheWriteDelegatingHandler_CanWriteFile(string mediaType, string directoryPath, string fileName, string requestUri)
        {
            var handler = new TestCacheWriteDelegatingHandler(ResponseFilesPath);
            handler.InnerHandler = new FakeHttpResponseHandler();

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            File.Exists(Path.Combine(ResponseFilesPath, directoryPath, $"{fileName}{TestCacheDelegatingHandlerBase.GetFileExtensionString(request)}")).Should().BeTrue();
        }

        /// <summary>
        /// Tests that providing a <see cref="MediaTypeWithQualityHeaderValue"/> in the Accept header will cause the <see cref="TestCacheWriteDelegatingHandler"/>
        /// to generate a file with a specific extension.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// A fake <see cref="DelegatingHandler"/> to provide the required base handler.
        /// </summary>
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
