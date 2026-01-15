using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CloudNimble.Breakdance.DotHttp.Models
{

    /// <summary>
    /// Represents a complete parsed .http file containing variables and requests.
    /// </summary>
    /// <example>
    /// <code>
    /// @baseUrl = https://api.example.com
    ///
    /// ### Get all users
    /// GET {{baseUrl}}/users
    ///
    /// ### Create user
    /// POST {{baseUrl}}/users
    /// Content-Type: application/json
    ///
    /// {"name": "John"}
    /// </code>
    /// </example>
    /// <remarks>
    /// A .http file can contain multiple requests separated by ### and file-level variables.
    /// Variables are case-sensitive per the Microsoft specification.
    /// </remarks>
    public class DotHttpFile
    {

        #region Properties

        /// <summary>
        /// Gets or sets the file path relative to the project.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets a value indicating whether any requests in this file have dependencies on other requests.
        /// </summary>
        /// <remarks>
        /// Returns true when any request uses response variable references like `{{login.response.body.$.token}}`.
        /// </remarks>
        public bool HasChainedRequests => Requests.Any(r => r.HasResponseReferences);

        /// <summary>
        /// Gets or sets the parsing diagnostics encountered while parsing this file.
        /// </summary>
        /// <remarks>
        /// Contains Roslyn diagnostic information for malformed content that could not be parsed.
        /// </remarks>
        public List<Diagnostic> Diagnostics { get; set; } = [];

        /// <summary>
        /// Gets or sets all HTTP requests defined in the file.
        /// </summary>
        /// <remarks>
        /// Requests are separated by ### in the source file.
        /// </remarks>
        public List<DotHttpRequest> Requests { get; set; } = [];

        /// <summary>
        /// Gets or sets the file-level variables defined with @name=value syntax.
        /// </summary>
        /// <example>
        /// <code>
        /// @baseUrl = https://api.example.com
        /// @apiVersion = v2
        /// </code>
        /// </example>
        /// <remarks>
        /// Variable names are case-sensitive per the Microsoft specification.
        /// </remarks>
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        #endregion

    }

}
