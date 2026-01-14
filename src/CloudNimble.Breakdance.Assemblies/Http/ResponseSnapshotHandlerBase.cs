using MimeTypes;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace CloudNimble.Breakdance.Assemblies.Http
{

    /// <summary>
    /// Base class for Response Snapshot handlers that enable testing with real captured HTTP responses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Response Snapshots are real HTTP responses captured from actual API calls and stored as files.
    /// This allows testing against real response data without hitting live endpoints or polluting
    /// third-party services with test data.
    /// </para>
    /// <para>
    /// Use <see cref="ResponseSnapshotCaptureHandler"/> to capture responses from live APIs,
    /// then use <see cref="ResponseSnapshotReplayHandler"/> to replay those responses in tests.
    /// </para>
    /// </remarks>
    public class ResponseSnapshotHandlerBase : DelegatingHandler
    {

        #region Fields

        /// <summary>
        /// Pattern used in RegEx to remove invalid characters from the file path.
        /// </summary>
        private static readonly string InvalidCharacterPattern = @"[\/\?&:]";

        /// <summary>
        /// Pattern used in RegEx to remove grouping characters from the file path.
        /// </summary>
        private static readonly string GroupingCharacterPattern = @"[\(\)\,\$]";

        /// <summary>
        /// Pattern used in RegEx to extract $expand segments for conversion to directories.
        /// </summary>
        private static readonly string ExpandSegmentPattern = @"(\$expand=[^\$]+)+";

        #endregion

        #region Properties

        /// <summary>
        /// Gets the root folder path where response snapshot files are stored.
        /// </summary>
        public string ResponseSnapshotsPath { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="ResponseSnapshotHandlerBase"/> with the specified snapshot storage path.
        /// </summary>
        /// <param name="responseSnapshotsPath">Root folder path for storing response snapshot files.</param>
        /// <example>
        /// <code>
        /// var handler = new ResponseSnapshotReplayHandler("TestData/Snapshots");
        /// var client = new HttpClient(handler);
        /// </code>
        /// </example>
        public ResponseSnapshotHandlerBase(string responseSnapshotsPath)
        {
            ResponseSnapshotsPath = responseSnapshotsPath;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Parses the RequestUri in the <see cref="HttpRequestMessage"/> into a <see cref="Path"/>-safe string
        /// suitable for storing response snapshots on the file system.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to parse.</param>
        /// <param name="responseSnapshotsPath">Root folder for storing snapshot files.</param>
        /// <returns>A tuple containing the directory path and file path components.</returns>
        /// <exception cref="ArgumentException">Thrown when the request has an invalid RequestUri.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the URI cannot be converted to a valid file path.</exception>
        internal static (string DirectoryPath, string FilePath) GetPathInfo(HttpRequestMessage request, string responseSnapshotsPath)
        {
            string directory;
            string fileName = string.Empty;

            var segmentCount = request.RequestUri.Segments.Length;
            var hasQuery = !string.IsNullOrEmpty(request.RequestUri.Query);

            if (segmentCount == 0)
            {
                throw new ArgumentException(nameof(request), "The specified HttpRequestMessage has an invalid RequestUri.");
            }

            if (segmentCount == 1)
            {
                return (request.RequestUri.DnsSafeHost, $"root{GetFileExtensionString(request)}");
            }

            if (hasQuery)
            {
                directory = JoinSegments(request, segmentCount - 1);

                var query = Regex.Replace(request.RequestUri.Query.Replace("%20", "_"), InvalidCharacterPattern, "");

                var match = Regex.Match(query, ExpandSegmentPattern, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

                fileName = match.Length > 0 ? query.Replace(match.Value, "") : query;

                var matchDirectories = match.Value.Replace("$", "\\");
                directory += matchDirectories;
            }
            else if (request.RequestUri.Segments[segmentCount - 1].StartsWith("$"))
            {
                fileName = Regex.Replace(request.RequestUri.Segments[segmentCount - 1].Replace("%20", "_"), InvalidCharacterPattern, "");

                if (segmentCount > 2)
                {
                    directory = JoinSegments(request, segmentCount - 2);
                }
                else
                {
                    directory = request.RequestUri.DnsSafeHost;
                }
            }
            else
            {
                directory = JoinSegments(request, segmentCount - 1);
            }

            directory = Regex.Replace(directory, GroupingCharacterPattern, "");
            fileName = Regex.Replace(fileName, GroupingCharacterPattern, "");

            fileName = string.IsNullOrEmpty(fileName) ? "root" : fileName;

            var fullPath = Path.Combine(Path.GetFullPath(responseSnapshotsPath), directory, $"{fileName}{GetFileExtensionString(request)}");

            if (fullPath.Length > 260)
            {
                var correction = fullPath.Length - 260;

                if (fileName.Length >= correction)
                {
                    fileName = fileName.Substring(0, fileName.Length - correction - 1);
                }
                else
                {
                    throw new InvalidOperationException($"Unable to convert the specified URI into a path that can be stored on the file system because the path is too long. Full path = {fullPath}");
                }
            }

            return (directory, $"{fileName}{GetFileExtensionString(request)}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Joins the segments of the request together and returns the string as a path, prepended with the host name.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to parse.</param>
        /// <param name="index">Index to use when joining segments.</param>
        /// <returns>The joined path string.</returns>
        private static string JoinSegments(HttpRequestMessage request, int index)
        {
            var directory = Regex.Replace(string.Join("\\", request.RequestUri.Segments, 1, index).Replace("(", "\\(").Replace("%20", "_"), InvalidCharacterPattern, "");

            return Path.Combine(request.RequestUri.DnsSafeHost, directory ?? "");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Maps the file extension in the specified path to a known list of media types.
        /// </summary>
        /// <param name="filePath">The file path to examine.</param>
        /// <returns>The MIME type string for the file extension.</returns>
        public static string GetResponseMediaTypeString(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            return MimeTypeMap.GetMimeType(extension);
        }

        /// <summary>
        /// Maps the MediaType header in the <see cref="HttpRequestMessage"/> to a known list of file extensions.
        /// </summary>
        /// <param name="request">The request to examine.</param>
        /// <returns>The file extension string for the request's Accept header.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        public static string GetFileExtensionString(HttpRequestMessage request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var acceptHeaders = request.Headers?.Accept;

            return MimeTypeMap.GetExtension(acceptHeaders.FirstOrDefault()?.MediaType);
        }

        #endregion

    }

}
