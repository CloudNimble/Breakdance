using System;
using System.Collections.Generic;

namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Represents a captured HTTP response with its body, headers, and related metadata.
    /// </summary>
    internal class CapturedResponse
    {

        #region Properties

        /// <summary>
        /// Gets or sets the original request body.
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        /// Gets or sets the response body content.
        /// </summary>
        public string ResponseBody { get; set; }

        /// <summary>
        /// Gets the response headers.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        #endregion

    }

}
