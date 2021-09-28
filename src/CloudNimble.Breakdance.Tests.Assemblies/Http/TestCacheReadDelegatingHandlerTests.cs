using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.Assemblies.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Net.Http.Headers;

namespace CloudNimble.Breakdance.Tests.Assemblies.Http
{

    /// <summary>
    /// Tests the functionality of the <see cref="TestCacheReadDelegatingHandler"/>.
    /// </summary>
    [TestClass]
    public class TestCacheReadDelegatingHandlerTests
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
        /// Tests that the <see cref="TestCacheReadDelegatingHandler"/> can parse the provided set of URIs and read the associated file.
        /// </summary>
        /// <param name="directoryPath">Folder containing test cache file.</param>
        /// <param name="fileName">Test cache file name.</param>
        /// <param name="requestUri">URI to parse.</param>
        /// <returns></returns>
        /// <remarks>If you have not already generated the files for this test, you should run the TestCacheWriteDelegatingHandler_CanWriteFile test first.</remarks>
        [TestMethod]
        [DynamicData(nameof(GetPathsAndTestUris))]
        public async Task TestCacheReadDelegatingHandler_CanReadFile(string mediaType, string directoryPath, string fileName, string requestUri)
        {
            var handler = new TestCacheReadDelegatingHandler(ResponseFilesPath);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// Tests that teh <see cref="TestCacheReadDelegatingHandler"/> throws an exception when the expected file does not exist.
        /// </summary>
        [TestMethod]
        public void TestCacheReadDelegatingHandler_ThrowsException_OnFileMissing()
        {
            File.Exists(Path.Combine(ResponseFilesPath, "services.odata.org", "missing-testcache-file")).Should().BeFalse();
            var handler = new TestCacheReadDelegatingHandler(ResponseFilesPath);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://services.odata.org/missing-testcache-file");
            Action act = () => handler.SendAsyncInternal(request).GetAwaiter().GetResult();
            act.Should().Throw<InvalidOperationException>().WithMessage("No test cache response file could be found at the path: *");
        }

        /// <summary>
        /// Tests that providing a <see cref="MediaTypeWithQualityHeaderValue"/> in the Accept header will cause the <see cref="TestCacheReadDelegatingHandler"/>
        /// to look for a file with a specific extension.  The file must already exist.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The <see cref="TestCacheReadDelegatingHandler"/> will automatically set the response ContentType to the
        /// appropriate value based on the Accept header ContentType
        /// </remarks>
        [TestMethod]
        public async Task TestCacheReadDelegatingHandler_MediaType_ReflectsFileExtension()
        {
            File.Exists(Path.Combine(ResponseFilesPath, "services.odata.org", "$metadata.xml")).Should().BeTrue();
            var handler = new TestCacheReadDelegatingHandler(ResponseFilesPath);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://services.odata.org/$metadata");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.ToString().Should().StartWith("text/xml");
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

    }
}
