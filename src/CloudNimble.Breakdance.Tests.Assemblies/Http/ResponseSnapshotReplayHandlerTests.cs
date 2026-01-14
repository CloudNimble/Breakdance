using CloudNimble.Breakdance.Assemblies.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Assemblies.Http
{

    /// <summary>
    /// Tests the functionality of the <see cref="ResponseSnapshotReplayHandler"/>.
    /// </summary>
    [TestClass]
    public class ResponseSnapshotReplayHandlerTests
    {

        #region Private Properties

        /// <summary>
        /// Root directory for storing response snapshot files.
        /// </summary>
        private static string ResponseSnapshotsPath => ResponseSnapshotHandlerBaseTests.ResponseSnapshotsPath;

        /// <summary>
        /// Local reference to test data is needed here or MSTest will choke.
        /// </summary>
        private static IEnumerable<object[]> GetPathsAndTestUris => ResponseSnapshotHandlerBaseTests.GetPathsAndTestUris;

        #endregion

        #region Tests

        /// <summary>
        /// Tests that the <see cref="ResponseSnapshotReplayHandler"/> can parse the provided set of URIs and read the associated snapshot file.
        /// </summary>
        /// <param name="mediaType">The media type for the request.</param>
        /// <param name="directoryPath">Folder containing response snapshot file.</param>
        /// <param name="fileName">Response snapshot file name.</param>
        /// <param name="requestUri">URI to parse.</param>
        /// <remarks>
        /// If you have not already generated the snapshot files for this test, you should run
        /// the <see cref="ResponseSnapshotCaptureHandlerTests.ResponseSnapshotCaptureHandler_CanWriteFile"/> test first.
        /// </remarks>
        [TestMethod]
#pragma warning disable MSTEST0018 // DynamicData should be valid
        [DynamicData(nameof(GetPathsAndTestUris))]
#pragma warning restore MSTEST0018 // DynamicData should be valid
        public async Task ResponseSnapshotReplayHandler_CanReadFile(string mediaType, string directoryPath, string fileName, string requestUri)
        {
            var handler = new ResponseSnapshotReplayHandler(ResponseSnapshotsPath);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that the <see cref="ResponseSnapshotReplayHandler"/> throws an exception when the expected snapshot file does not exist.
        /// </summary>
        [TestMethod]
        public void ResponseSnapshotReplayHandler_ThrowsException_OnFileMissing()
        {
            var path = Path.Combine(ResponseSnapshotsPath, "services.odata.org", "missing-snapshot-file");
            File.Exists(path).Should().BeFalse();
            var handler = new ResponseSnapshotReplayHandler(ResponseSnapshotsPath);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://services.odata.org/missing-snapshot-file");
            Action act = () => handler.SendAsyncInternal(request).GetAwaiter().GetResult();
            act.Should().Throw<InvalidOperationException>().WithMessage("No response snapshot file could be found at the path: *");
        }

        /// <summary>
        /// Tests that providing a <see cref="MediaTypeWithQualityHeaderValue"/> in the Accept header will cause the <see cref="ResponseSnapshotReplayHandler"/>
        /// to look for a file with a specific extension. The file must already exist.
        /// </summary>
        /// <remarks>
        /// The <see cref="ResponseSnapshotReplayHandler"/> will automatically set the response ContentType to the
        /// appropriate value based on the Accept header ContentType.
        /// </remarks>
        [TestMethod]
        public async Task ResponseSnapshotReplayHandler_MediaType_ReflectsFileExtension()
        {
            var path = Path.Combine(ResponseSnapshotsPath, "services.odata.org", "metadata.xml");
            var dir = Directory.GetCurrentDirectory();
            File.Exists(path).Should().BeTrue();
            var handler = new ResponseSnapshotReplayHandler(ResponseSnapshotsPath);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://services.odata.org/$metadata");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.ToString().Should().StartWith("text/xml");
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        #endregion

    }

}
