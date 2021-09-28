using MimeTypes;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace CloudNimble.Breakdance.Assemblies.Http
{

    /// <summary>
    /// Base class for implementation of TestCache handlers for unit testing.
    /// </summary>
    public class TestCacheDelegatingHandlerBase : DelegatingHandler
    {

        #region Properties

        /// <summary>
        /// Stores the root folder for reading/writing static response files.
        /// </summary>
        public string ResponseFilesPath { get; private set; }

        /// <summary>
        /// Pattern used in RegEx to remove invalid characters from the file path
        /// </summary>
        private static string InvalidCharacterPattern = @"[\/\?&:]";

        private static string GroupingCharacterPattern = @"[\(\)\,\$]";

        /// <summary>
        /// Pattern used in RegEx to extract $expand segments for conversion to directories
        /// </summary>
        private static string ExpandSegmentPattern = @"(\$expand=[^\$]+)+";

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor overload for specifying the root folder path.
        /// </summary>
        /// <param name="responseFilesPath">Root folder path for storing static response files.</param>
        public TestCacheDelegatingHandlerBase(string responseFilesPath)
        {
            ResponseFilesPath = responseFilesPath;
        }

        #endregion

        /// <summary>
        /// Parses the RequestUri in the <see cref="HttpRequestMessage"/> into a <see cref="Path"/>-safe string.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to parse.</param>
        /// <param name="responseFilePath">Root folder for storing cache files.</param>
        /// <returns></returns>
        internal static (string DirectoryPath, string FilePath) GetPathInfo(HttpRequestMessage request, string responseFilePath)
        {
            string directory;
            string fileName = string.Empty;

            var segmentCount = request.RequestUri.Segments.Length;
            var hasQuery = !string.IsNullOrEmpty(request.RequestUri.Query);

            if (segmentCount == 0)
            {
                // an invalid request was provided
                throw new ArgumentException(nameof(request), "The specified HttpRequestMessage has an invalid RequestUri.");
            }

            if (segmentCount == 1)
            {
                // return the full host as the directory with a root file
                return (request.RequestUri.DnsSafeHost, $"root{GetFileExtensionString(request)}");
            }

            // if the URI includes a query, we will use it as the filename instead of the last segment
            if (hasQuery)
            {
                directory = JoinSegments(request, segmentCount - 1);

                // extract the last segment as the file name and strip invalid characters
                var query = Regex.Replace(request.RequestUri.Query.Replace("%20", "_"), InvalidCharacterPattern, "");

                // parse out any $expand segments
                var match = Regex.Match(query, ExpandSegmentPattern, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

                // the remainder is the filename
                fileName = match.Length > 0 ? query.Replace(match.Value, "") : query;

                // break up the match into nested folders and remove parens
                var matchDirectories = match.Value.Replace("$", "\\");
                directory += matchDirectories;

            }
            else if (request.RequestUri.Segments[segmentCount - 1].StartsWith("$"))
            {
                // use the final segment as the fileName (e.g. $metadata, $top=10, $count, etc)
                fileName = Regex.Replace(request.RequestUri.Segments[segmentCount - 1].Replace("%20", "_"), InvalidCharacterPattern, "");

                // directory may be empty if there are no other segments
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
                // extract all the inside segments as the directory name
                directory = JoinSegments(request, segmentCount - 1);
            }

            // one more pass to clean out extra characters
            directory = Regex.Replace(directory, GroupingCharacterPattern, "");
            fileName = Regex.Replace(fileName, GroupingCharacterPattern, "");

            // ensure default fileName
            fileName = string.IsNullOrEmpty(fileName) ? "root" : fileName;

            // the full path must not exceed 260 characters, so try to reduce the length of the fileName if necessary
            var fullPath = Path.Combine(Path.GetFullPath(responseFilePath), directory, $"{fileName}{GetFileExtensionString(request)}");

            if (fullPath.Length > 260)
            {
                var correction = fullPath.Length - 260;

                if (fileName.Length >= correction)
                {
                    fileName = fileName.Substring(0, fileName.Length - correction - 1);
                }
                else
                {
                    throw new InvalidOperationException($"Unable to convert the specified URI into a path that can be stored on the file system because the path is too long.  Full path = {fullPath}");
                }
            }

            // pre-pend the DNS-save host name and return the components in a tuple
            return (directory, $"{fileName}{GetFileExtensionString(request)}");

        }

        /// <summary>
        /// Joins the segments of the request together and returns the string as a path, prepended with the host name
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to parse.</param>
        /// <param name="index">Index to use whe joining segments.</param>
        /// <returns></returns>
        private static string JoinSegments(HttpRequestMessage request, int index)
        {
            var directory = Regex.Replace(string.Join("\\", request.RequestUri.Segments, 1, index).Replace("(", "\\(").Replace("%20", "_"), InvalidCharacterPattern, "");
            return Path.Combine(request.RequestUri.DnsSafeHost, directory ?? "");
        }

        /// <summary>
        /// Maps the file extension in the specified path to a known list of media types.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetResponseMediaTypeString(string filePath)
        {
            // get the file extension from the path
            var extension = Path.GetExtension(filePath);
            return MimeTypeMap.GetMimeType(extension);
        }

        /// <summary>
        /// Maps the MediaType header in the <see cref="HttpRequestMessage"/> to a known list of file extensions.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetFileExtensionString(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // get the acceptable content types specified by the request message
            var acceptHeaders = request.Headers?.Accept;
            return MimeTypeMap.GetExtension(acceptHeaders.FirstOrDefault()?.MediaType);
        }

    }
}
