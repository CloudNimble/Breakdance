using System;
using System.Collections.Generic;

namespace CloudNimble.Breakdance.DotHttp.Models
{

    /// <summary>
    /// Represents a single HTTP request parsed from a .http file.
    /// </summary>
    /// <example>
    /// <code>
    /// # @name GetUsers
    /// GET {{baseUrl}}/users
    /// Accept: application/json
    /// Authorization: Bearer {{login.response.body.$.token}}
    /// </code>
    /// </example>
    /// <remarks>
    /// Supports all standard HTTP methods, request chaining via response variable references,
    /// file-based request bodies, and request-level variable overrides.
    /// </remarks>
    public class DotHttpRequest
    {

        #region Properties

        /// <summary>
        /// Gets or sets the request body content for POST, PUT, and PATCH requests.
        /// </summary>
        /// <remarks>
        /// May contain {{variable}} references that are resolved at runtime.
        /// For file references, this will be null and <see cref="BodyFilePath"/> will be set.
        /// </remarks>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the file path for request body when using file reference syntax.
        /// </summary>
        /// <example>
        /// <code>
        /// POST {{baseUrl}}/upload
        /// Content-Type: application/octet-stream
        ///
        /// &lt; ./path/to/file.bin
        /// </code>
        /// </example>
        /// <remarks>
        /// When set, the body content should be loaded from this file path at runtime.
        /// The path is relative to the .http file location.
        /// </remarks>
        public string BodyFilePath { get; set; }

        /// <summary>
        /// Gets or sets the comments and documentation that appeared before this request.
        /// </summary>
        /// <remarks>
        /// Comments are parsed from lines starting with # or // that precede the request line.
        /// The comment markers are preserved for accurate representation.
        /// </remarks>
        public List<string> Comments { get; set; } = [];

        /// <summary>
        /// Gets or sets the names of requests this request depends on for chaining.
        /// </summary>
        /// <example>
        /// <code>
        /// // A request using {{login.response.body.$.token}} would have "login" in DependsOn
        /// </code>
        /// </example>
        public List<string> DependsOn { get; set; } = [];

        /// <summary>
        /// Gets a value indicating whether this request references variables from previous responses.
        /// </summary>
        /// <example>
        /// <code>
        /// // True when the request contains syntax like {{login.response.body.$.token}}
        /// </code>
        /// </example>
        public bool HasResponseReferences { get; set; }

        /// <summary>
        /// Gets or sets the request headers as key-value pairs.
        /// </summary>
        /// <remarks>
        /// Headers are stored with case-insensitive keys per RFC 7230.
        /// Header values may contain {{variable}} references that are resolved at runtime.
        /// </remarks>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the HTTP version (HTTP/1.1, HTTP/2, HTTP/3).
        /// </summary>
        /// <remarks>
        /// Optional. When not specified, the default HTTP version is used.
        /// </remarks>
        public string HttpVersion { get; set; }

        /// <summary>
        /// Gets a value indicating whether the request body should be loaded from a file.
        /// </summary>
        public bool IsFileBody => !string.IsNullOrEmpty(BodyFilePath);

        /// <summary>
        /// Gets or sets the line number in the source file where this request begins.
        /// </summary>
        /// <remarks>
        /// Used for diagnostics and error reporting.
        /// </remarks>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method (GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS, TRACE, CONNECT).
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the optional name for the request, parsed from "# @name RequestName" comment.
        /// </summary>
        /// <remarks>
        /// Used for generating test method names and for referencing in request chaining.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the text that appeared after the ### separator on the same line.
        /// </summary>
        /// <example>
        /// <code>
        /// ### Get All Users
        /// GET {{baseUrl}}/users
        /// </code>
        /// </example>
        /// <remarks>
        /// This text is used for generating descriptive test method names when @name is not specified.
        /// </remarks>
        public string SeparatorTitle { get; set; }

        /// <summary>
        /// Gets or sets the request URL.
        /// </summary>
        /// <remarks>
        /// May contain {{variable}} references that are resolved at runtime.
        /// </remarks>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the request-level variables that override file-level variables.
        /// </summary>
        /// <example>
        /// <code>
        /// @baseUrl = https://override.example.com
        /// GET {{baseUrl}}/users
        /// </code>
        /// </example>
        /// <remarks>
        /// Variables defined after a request separator (###) but before the next request line
        /// apply only to that request and override file-level variables with the same name.
        /// </remarks>
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        #endregion

    }

}
