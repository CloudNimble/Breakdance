using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Assemblies.Http
{

    /// <summary>
    /// Handler for mocking the HttpResponse returned by an HttpRequest using a UTF-8 encoded file.
    /// </summary>
    public class TestCacheReadDelegatingHandler : TestCacheDelegatingHandlerBase
    {

        #region Constructors

        /// <summary>
        /// Constructor overload for specifying the root folder path.
        /// </summary>
        /// <param name="responseFilesPath">Root folder path for storing static response files.</param>
        public TestCacheReadDelegatingHandler(string responseFilesPath) : base(responseFilesPath)
        { }

        #endregion

        /// <summary>
        /// This internal method is here just to allow the test projects to call the otherwise inaccessibile SendAsync() method
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that is being intercepted by the <see cref="DelegatingHandler"/>.</param>
        /// <returns></returns>
        internal async Task<HttpResponseMessage> SendAsyncInternal(HttpRequestMessage request)
        {
            return await SendAsync(request, new CancellationToken()).ConfigureAwait(false);
        }

        /// <summary>
        /// Overrides the method in the base <see cref="DelegatingHandler"/> to create a custom <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> that is being intercepted by the <see cref="DelegatingHandler"/>.</param>
        /// <param name="cancellationToken">Token for cancelling the asynchronous request.</param>
        /// <returns></returns>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // parse the URI into a content file path structure
            var pathComponents = GetStaticFilePath(request);

            // get the full path for the response file
            var fullPath = Path.Combine(ResponseFilesPath, pathComponents.DirectoryPath, pathComponents.FilePath);

            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"No test cache response file could be found at the path: {fullPath}.");
            }

#if NETCOREAPP3_1_OR_GREATER
            var fileContent = await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
#else
            var fileContent = File.ReadAllText(fullPath, Encoding.UTF8);
#endif

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fileContent)
            };

            var taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
            taskCompletionSource.SetResult(response);

            return await taskCompletionSource.Task.ConfigureAwait(false);
        }

    }
}
