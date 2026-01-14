using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Assemblies.Http
{

    /// <summary>
    /// A <see cref="DelegatingHandler"/> that captures HTTP responses and saves them as snapshot files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler passes requests through to the actual endpoint, then captures the response
    /// and saves it as a snapshot file. This enables recording real API responses for later
    /// replay during testing.
    /// </para>
    /// <para>
    /// Use this handler during an initial recording phase to capture responses from third-party
    /// APIs, then use <see cref="ResponseSnapshotReplayHandler"/> to replay those responses
    /// during test execution without hitting live endpoints.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a handler that captures responses to snapshot files
    /// var innerHandler = new HttpClientHandler();
    /// var captureHandler = new ResponseSnapshotCaptureHandler("TestData/Snapshots")
    /// {
    ///     InnerHandler = innerHandler
    /// };
    /// var client = new HttpClient(captureHandler);
    ///
    /// // Requests go to the real endpoint, responses are saved as snapshots
    /// var response = await client.GetAsync("https://api.example.com/users");
    /// </code>
    /// </example>
    public class ResponseSnapshotCaptureHandler : ResponseSnapshotHandlerBase
    {

        #region Fields

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
        /// Creates a new <see cref="ResponseSnapshotCaptureHandler"/> that saves response snapshots to the specified path.
        /// </summary>
        /// <param name="responseSnapshotsPath">Root folder path for storing response snapshot files.</param>
        public ResponseSnapshotCaptureHandler(string responseSnapshotsPath) : base(responseSnapshotsPath)
        {
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Allows test projects to call the otherwise inaccessible <see cref="SendAsync"/> method directly.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to process.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> from the actual endpoint.</returns>
        internal async Task<HttpResponseMessage> SendAsyncInternal(HttpRequestMessage request)
        {
            return await SendAsync(request, new CancellationToken()).ConfigureAwait(false);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Sends the request to the actual endpoint and captures the response as a snapshot file.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> being intercepted.</param>
        /// <param name="cancellationToken">Token for cancelling the asynchronous operation.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> from the actual endpoint.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Ensure.ArgumentNotNull(request, nameof(request));

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response is null)
            {
                // The inner handler returned null - nothing to capture
                return response;
            }

            var (DirectoryPath, FilePath) = GetPathInfo(request, ResponseSnapshotsPath);

            var folderPath = Path.Combine(ResponseSnapshotsPath, DirectoryPath);
            Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(ResponseSnapshotsPath, DirectoryPath, FilePath);

            var fileContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            await WriteFileWithRetryAsync(fullPath, fileContent, cancellationToken).ConfigureAwait(false);

            var taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
            taskCompletionSource.SetResult(response);

            return await taskCompletionSource.Task.ConfigureAwait(false);
        }

        #endregion

        #region Private Methods

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
                    delay *= 2;
                }
            }
        }

        #endregion

    }

}
