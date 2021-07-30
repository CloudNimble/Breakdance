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

        [TestMethod]
        [DynamicData(nameof(GetPathsAndTestUris))]
        public async Task TestCacheReadDelegatingHandler_CanReadFile(string directoryPath, string fileName, string requestUri)
        {
            var handler = new TestCacheReadDelegatingHandler(ResponseFilesPath);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var response = await handler.SendAsyncInternal(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        [TestMethod]
        public void TestCacheReadDelegatingHandler_ThrowsException_OnFileMissing()
        {
            File.Exists(Path.Combine(ResponseFilesPath, "services.odata.org", "missing-testcache-file")).Should().BeFalse();
            var handler = new TestCacheReadDelegatingHandler(ResponseFilesPath);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://services.odata.org/missing-testcache-file");
            Action act = () => handler.SendAsyncInternal(request).GetAwaiter().GetResult();
            act.Should().Throw<InvalidOperationException>().WithMessage("No test cache response file could be found at the path: *");
        }

    }
}
