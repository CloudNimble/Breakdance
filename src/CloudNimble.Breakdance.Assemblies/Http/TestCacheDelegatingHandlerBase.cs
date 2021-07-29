using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        private static string InvalidCharacterPattern = @"[\/\?&:]";

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
        /// <returns></returns>
        internal static (string DirectoryPath, string FilePath) GetStaticFilePath(HttpRequestMessage request)
        {
            string directory;
            string fileName;

            var segments = request.RequestUri.Segments.Length;
            var hasQuery = !string.IsNullOrEmpty(request.RequestUri.Query);

            if (segments == 0)
            {
                // an invalid request was provided
                throw new ArgumentException(nameof(request), "The specified HttpRequestMessage has an invalid RequestUri.");
            }

            if (segments == 1)
            {
                // return the full host as the directory with a root file
                return (request.RequestUri.DnsSafeHost, "root");
            }

            // if the URI includes a query, we will use it as the filename instead of the last segment
            if (hasQuery)
            {
                // extract the last segment as the file name and strip invalid characters
                fileName = Regex.Replace(request.RequestUri.Query.Replace("%20", "_"), InvalidCharacterPattern, "");
                directory = JoinSegments(request, segments - 1);
            }
            else if (request.RequestUri.Segments[segments - 1].StartsWith("$"))
            {
                // use the final segment as the fileName (e.g. $metadata, $top=10, $count, etc)
                fileName = Regex.Replace(request.RequestUri.Segments[segments - 1].Replace("%20", "_"), InvalidCharacterPattern, "");

                // directory may be empty if there are no other segments
                if (segments > 2)
                {
                    directory = JoinSegments(request, segments - 2);
                }
                else
                {
                    directory = request.RequestUri.DnsSafeHost;
                }
            }
            else
            {
                // if there is no query, the file name will always be "root"
                fileName = "root";

                // extract all the inside segments as the directory name
                directory = JoinSegments(request, segments - 1);
            }

            // pre-pend the DNS-save host name and return the components in a tuple
            return (directory, fileName);

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

    }
}
