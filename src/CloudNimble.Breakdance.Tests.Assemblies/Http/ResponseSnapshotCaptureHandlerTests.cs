using CloudNimble.Breakdance.Assemblies.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Assemblies.Http
{

    /// <summary>
    /// Tests the functionality of the <see cref="ResponseSnapshotCaptureHandler"/>.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ResponseSnapshotCaptureHandlerTests
    {

        #region Properties

        /// <summary>
        /// Gets or sets the test context for the current test.
        /// </summary>
        public TestContext TestContext { get; set; }

        #endregion

        #region Private Properties

        /// <summary>
        /// Root directory for storing response snapshot files.
        /// </summary>
        private static string ResponseSnapshotsPath => ResponseSnapshotHandlerBaseTests.ResponseSnapshotsPath;

        /// <summary>
        /// Local reference to test data is needed here or MSTest will choke.
        /// </summary>
        internal static IEnumerable<object[]> GetPathsAndTestUris => ResponseSnapshotHandlerBaseTests.GetPathsAndTestUris;

        #endregion

        #region Tests

        /// <summary>
        /// Tests that the <see cref="ResponseSnapshotCaptureHandler"/> can parse the provided set of URIs and write the response to a snapshot file.
        /// </summary>
        /// <param name="mediaType">The media type for the request.</param>
        /// <param name="directoryPath">Folder containing response snapshot file.</param>
        /// <param name="fileName">Response snapshot file name.</param>
        /// <param name="requestUri">URI to parse.</param>
        [TestMethod]
#pragma warning disable MSTEST0018 // DynamicData should be valid
        [DynamicData(nameof(GetPathsAndTestUris))]
#pragma warning restore MSTEST0018 // DynamicData should be valid
        public async Task ResponseSnapshotCaptureHandler_CanWriteFile(string mediaType, string directoryPath, string fileName, string requestUri)
        {
            var handler = new ResponseSnapshotCaptureHandler(ResponseSnapshotsPath)
            {
                InnerHandler = new FakeHttpResponseHandler()
            };

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            content.Should().NotBeNullOrEmpty();

            File.Exists(Path.Combine(ResponseSnapshotsPath, directoryPath, $"{fileName}{ResponseSnapshotHandlerBase.GetFileExtensionString(request)}")).Should().BeTrue();
        }

        /// <summary>
        /// Tests that providing a <see cref="MediaTypeWithQualityHeaderValue"/> in the Accept header will cause the <see cref="ResponseSnapshotCaptureHandler"/>
        /// to generate a snapshot file with the appropriate extension.
        /// </summary>
        [TestMethod]
        public async Task ResponseSnapshotCaptureHandler_FileExtension_ReflectsMediaType()
        {
            var handler = new ResponseSnapshotCaptureHandler(ResponseSnapshotsPath)
            {
                InnerHandler = new FakeHttpResponseHandler()
            };

            var request = new HttpRequestMessage(HttpMethod.Get, "https://services.odata.org/$metadata");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            content.Should().NotBeNullOrEmpty();

            File.Exists(Path.Combine(ResponseSnapshotsPath, "services.odata.org", "metadata.xml")).Should().BeTrue();
        }

        #endregion

        #region Private Classes

        /// <summary>
        /// A fake <see cref="DelegatingHandler"/> to provide the required base handler for testing.
        /// </summary>
        private class FakeHttpResponseHandler : DelegatingHandler
        {

            /// <summary>
            /// Sends the request and returns a fake response.
            /// </summary>
            /// <param name="request">The <see cref="HttpRequestMessage"/> to process.</param>
            /// <param name="cancellationToken">Token for cancelling the asynchronous operation.</param>
            /// <returns>A fake <see cref="HttpResponseMessage"/> with empty JSON content.</returns>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{ }", Encoding.UTF8)
                };
                return Task.FromResult(response);
            }

        }

        #endregion

    }

}
