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
    /// A <see cref="DelegatingHandler"/> that replays previously captured HTTP responses from snapshot files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler intercepts outgoing HTTP requests and returns responses from snapshot files
    /// instead of making actual network calls. This enables deterministic testing against real
    /// response data without hitting live endpoints.
    /// </para>
    /// <para>
    /// Response snapshots are typically captured using <see cref="ResponseSnapshotCaptureHandler"/>
    /// during an initial recording phase, then replayed during test execution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a handler that reads from snapshot files
    /// var handler = new ResponseSnapshotReplayHandler("TestData/Snapshots");
    /// var client = new HttpClient(handler);
    ///
    /// // Requests will be served from snapshot files instead of the network
    /// var response = await client.GetAsync("https://api.example.com/users");
    /// </code>
    /// </example>
    public class ResponseSnapshotReplayHandler : ResponseSnapshotHandlerBase
    {

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="ResponseSnapshotReplayHandler"/> that reads response snapshots from the specified path.
        /// </summary>
        /// <param name="responseSnapshotsPath">Root folder path containing response snapshot files.</param>
        public ResponseSnapshotReplayHandler(string responseSnapshotsPath) : base(responseSnapshotsPath)
        {
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Allows test projects to call the otherwise inaccessible <see cref="SendAsync"/> method directly.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to process.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> loaded from the snapshot file.</returns>
        internal async Task<HttpResponseMessage> SendAsyncInternal(HttpRequestMessage request)
        {
            return await SendAsync(request, new CancellationToken()).ConfigureAwait(false);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Intercepts the HTTP request and returns a response loaded from a snapshot file.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> being intercepted.</param>
        /// <param name="cancellationToken">Token for cancelling the asynchronous operation.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> with content loaded from the corresponding snapshot file.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no snapshot file exists for the request.</exception>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Ensure.ArgumentNotNull(request, nameof(request));

            var pathComponents = GetPathInfo(request, ResponseSnapshotsPath);

            var fullPath = Path.Combine(ResponseSnapshotsPath, pathComponents.DirectoryPath, pathComponents.FilePath);

            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"No response snapshot file could be found at the path: {fullPath}.");
            }

#if NETCOREAPP3_1_OR_GREATER
            var fileContent = await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
#else
            var fileContent = File.ReadAllText(fullPath, Encoding.UTF8);
#endif

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fileContent, Encoding.UTF8, GetResponseMediaTypeString(pathComponents.FilePath))
            };

            var taskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
            taskCompletionSource.SetResult(response);

            return await taskCompletionSource.Task.ConfigureAwait(false);
        }

        #endregion

    }

}
