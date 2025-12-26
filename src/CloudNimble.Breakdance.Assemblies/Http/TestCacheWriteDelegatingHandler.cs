using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Assemblies.Http
{

    /// <summary>
    /// Handler for mocking the HttpResponse returned by an HttpRequest using a UTF-8 encoded file.
    /// </summary>
    public class TestCacheWriteDelegatingHandler : TestCacheDelegatingHandlerBase
    {

        #region Private Members

        /// <summary>
        /// Maximum number of retry attempts when file is locked.
        /// </summary>
        private const int MaxRetryAttempts = 5;

        /// <summary>
        /// Initial delay in milliseconds before retrying.
        /// </summary>
        private const int InitialRetryDelayMs = 50;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor overload for specifying the root folder path.
        /// </summary>
        /// <param name="responseFilesPath">Root folder path for storing static response files.</param>
        public TestCacheWriteDelegatingHandler(string responseFilesPath) : base(responseFilesPath)
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
            Ensure.ArgumentNotNull(request, nameof(request));

            // allow the request to complete
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response is null)
            {
                // throw ??
            }

            // parse the URI into a content file path structure
            var (DirectoryPath, FilePath) = GetPathInfo(request, ResponseFilesPath);

            // make sure the folder exists to store the query file
            var folderPath = Path.Combine(ResponseFilesPath, DirectoryPath);
            Directory.CreateDirectory(folderPath);

            // get the full path for the response file
            var fullPath = Path.Combine(ResponseFilesPath, DirectoryPath, FilePath);

            var fileContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            await WriteFileWithRetryAsync(fullPath, fileContent, cancellationToken).ConfigureAwait(false);

            var taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
            taskCompletionSource.SetResult(response);

            return await taskCompletionSource.Task.ConfigureAwait(false);

        }

        /// <summary>
        /// Writes content to a file with retry logic to handle concurrent access from multiple test assemblies.
        /// </summary>
        /// <param name="fullPath">The full path to the file.</param>
        /// <param name="content">The content to write.</param>
        /// <param name="cancellationToken">Token for cancelling the operation.</param>
        private static async Task WriteFileWithRetryAsync(string fullPath, string content, CancellationToken cancellationToken)
        {
            var retryCount = 0;
            var delay = InitialRetryDelayMs;

            while (true)
            {
                try
                {
#if NETCOREAPP3_1_OR_GREATER
                    await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
#else
                    File.WriteAllText(fullPath, content, Encoding.UTF8);
#endif
                    return;
                }
                catch (IOException) when (retryCount < MaxRetryAttempts)
                {
                    retryCount++;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay *= 2; // Exponential backoff
                }
            }
        }

    }

}
