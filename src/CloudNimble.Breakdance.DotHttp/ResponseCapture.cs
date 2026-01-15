using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Captures HTTP responses from named requests for use in request chaining.
    /// </summary>
    /// <example>
    /// <code>
    /// var capture = new ResponseCapture();
    /// await capture.CaptureAsync("login", loginResponse);
    ///
    /// // Later, resolve a reference from the captured response
    /// var token = capture.ResolveReference("{{login.response.body.$.token}}");
    /// var header = capture.ResolveReference("{{login.response.headers.X-Request-Id}}");
    /// </code>
    /// </example>
    /// <remarks>
    /// Supports `{{name.response.body.$.path}}` for JSONPath, `{{name.response.body./xpath}}` for XPath,
    /// and `{{name.response.headers.HeaderName}}` for header extraction.
    /// </remarks>
    public sealed partial class ResponseCapture
    {

        #region Fields

        private readonly Dictionary<string, CapturedResponse> _responses = new Dictionary<string, CapturedResponse>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Public Methods

        /// <summary>
        /// Captures a response for later reference.
        /// </summary>
        /// <param name="name">The request name (from # @name directive).</param>
        /// <param name="response">The HTTP response.</param>
        /// <param name="requestBody">The original request body (for `{{name.request.body}}` references).</param>
        /// <example>
        /// <code>
        /// var capture = new ResponseCapture();
        /// var response = await httpClient.SendAsync(request);
        /// await capture.CaptureAsync("createUser", response, requestBodyJson);
        /// </code>
        /// </example>
        public async Task CaptureAsync(string name, HttpResponseMessage response, string requestBody = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var body = response.Content != null
                ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                : string.Empty;

            var captured = new CapturedResponse
            {
                ResponseBody = body,
                RequestBody = requestBody,
                StatusCode = (int)response.StatusCode
            };

            // Capture headers
            foreach (var header in response.Headers)
            {
                captured.ResponseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            if (response.Content?.Headers != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    captured.ResponseHeaders[header.Key] = string.Join(", ", header.Value);
                }
            }

            _responses[name] = captured;
        }

        /// <summary>
        /// Clears all captured responses.
        /// </summary>
        /// <example>
        /// <code>
        /// var capture = new ResponseCapture();
        /// // ... capture some responses ...
        /// capture.Clear(); // Reset for new test
        /// </code>
        /// </example>
        public void Clear()
        {
            _responses.Clear();
        }

        /// <summary>
        /// Gets the captured response body for a named request.
        /// </summary>
        /// <param name="name">The request name.</param>
        /// <returns>The response body, or null if not captured.</returns>
        /// <example>
        /// <code>
        /// var capture = new ResponseCapture();
        /// await capture.CaptureAsync("login", response);
        /// var body = capture.GetResponseBody("login");
        /// </code>
        /// </example>
        public string GetResponseBody(string name)
        {
            return _responses.TryGetValue(name, out var captured) ? captured.ResponseBody : null;
        }

        /// <summary>
        /// Checks if a captured response exists for the given name.
        /// </summary>
        /// <param name="name">The request name.</param>
        /// <returns>True if the response has been captured.</returns>
        /// <example>
        /// <code>
        /// if (capture.HasResponse("login"))
        /// {
        ///     var token = capture.ResolveReference("{{login.response.body.$.token}}");
        /// }
        /// </code>
        /// </example>
        public bool HasResponse(string name)
        {
            return _responses.ContainsKey(name);
        }

        /// <summary>
        /// Resolves all response references in a string.
        /// </summary>
        /// <param name="input">The input string containing response references.</param>
        /// <returns>The resolved string.</returns>
        /// <example>
        /// <code>
        /// var url = "https://api.example.com/users/{{login.response.body.$.userId}}";
        /// var resolvedUrl = capture.ResolveAllReferences(url);
        /// </code>
        /// </example>
        public string ResolveAllReferences(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return ResponseReferenceRegex().Replace(input, match => ResolveReference(match.Value));
        }

        /// <summary>
        /// Resolves a response reference to its actual value.
        /// </summary>
        /// <param name="reference">The full reference string (e.g., `"{{login.response.body.$.token}}"`).</param>
        /// <returns>The resolved value, or the original reference if not found.</returns>
        /// <example>
        /// <code>
        /// var capture = new ResponseCapture();
        /// await capture.CaptureAsync("login", response);
        /// var token = capture.ResolveReference("{{login.response.body.$.token}}");
        /// </code>
        /// </example>
        public string ResolveReference(string reference)
        {
            var match = ResponseReferenceRegex().Match(reference);
            if (!match.Success)
            {
                return reference;
            }

            var requestName = match.Groups[1].Value;
            var sourceType = match.Groups[2].Value.ToLowerInvariant(); // response or request
            var partType = match.Groups[3].Value.ToLowerInvariant();   // body or headers
            var path = match.Groups[4].Value;

            if (!_responses.TryGetValue(requestName, out var captured))
            {
                return reference; // Request not captured yet
            }

            if (partType == "headers")
            {
                if (captured.ResponseHeaders.TryGetValue(path, out var headerValue))
                {
                    return headerValue;
                }
                return reference;
            }

            if (partType == "body")
            {
                var body = sourceType == "request" ? captured.RequestBody : captured.ResponseBody;

                if (path == "*")
                {
                    return body ?? string.Empty;
                }

                if (path.StartsWith("$."))
                {
                    return EvaluateJsonPath(body, path);
                }

                if (path.StartsWith("/"))
                {
                    return EvaluateXPath(body, path);
                }

                // Fallback: try as simple JSON property path
                return EvaluateSimpleJsonPath(body, path);
            }

            return reference;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Evaluates a JSONPath expression against JSON content.
        /// </summary>
        /// <param name="json">The JSON string to evaluate.</param>
        /// <param name="path">The JSONPath expression (e.g., "$.user.name").</param>
        /// <returns>The extracted value, or empty string if not found.</returns>
        /// <remarks>
        /// Supports basic JSONPath including property access and array indexers (e.g., $.items[0].id).
        /// </remarks>
        internal string EvaluateJsonPath(string json, string path)
        {
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                var element = document.RootElement;

                // Simple JSONPath evaluation (supports basic paths like $.user.name or $.items[0].id)
                var pathParts = path.Substring(2).Split('.');

                foreach (var part in pathParts)
                {
                    if (string.IsNullOrEmpty(part))
                    {
                        continue;
                    }

                    // Check for array indexer
                    var arrayMatch = Regex.Match(part, @"^(\w+)\[(\d+)\]$");
                    if (arrayMatch.Success)
                    {
                        var propertyName = arrayMatch.Groups[1].Value;
                        var index = int.Parse(arrayMatch.Groups[2].Value);

                        if (element.TryGetProperty(propertyName, out var arrayElement) &&
                            arrayElement.ValueKind == JsonValueKind.Array &&
                            index < arrayElement.GetArrayLength())
                        {
                            element = arrayElement[index];
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                    else if (element.TryGetProperty(part, out var property))
                    {
                        element = property;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }

                return GetJsonElementValue(element);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Evaluates a simple JSON property path without the $. prefix.
        /// </summary>
        /// <param name="json">The JSON string to evaluate.</param>
        /// <param name="propertyPath">The property path (e.g., "user.name").</param>
        /// <returns>The extracted value, or empty string if not found.</returns>
        internal string EvaluateSimpleJsonPath(string json, string propertyPath)
        {
            // Handle simple property paths without $. prefix
            return EvaluateJsonPath(json, "$." + propertyPath);
        }

        /// <summary>
        /// Evaluates an XPath expression against XML content.
        /// </summary>
        /// <param name="xml">The XML string to evaluate.</param>
        /// <param name="path">The XPath expression.</param>
        /// <returns>The extracted value, or empty string if not found.</returns>
        internal string EvaluateXPath(string xml, string path)
        {
            if (string.IsNullOrEmpty(xml))
            {
                return string.Empty;
            }

            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                var node = doc.SelectSingleNode(path);
                return node?.InnerText ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the string representation of a JSON element.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <returns>The string value.</returns>
        internal string GetJsonElementValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    return element.GetRawText();
                case JsonValueKind.True:
                    return "true";
                case JsonValueKind.False:
                    return "false";
                case JsonValueKind.Null:
                    return string.Empty;
                default:
                    return element.GetRawText();
            }
        }

        #endregion

        #region Regex Patterns

        // Pattern constants - single source of truth for both GeneratedRegex and compiled Regex
        private const string ResponseReferencePattern = @"\{\{(\w+)\.(response|request)\.(body|headers)\.([^}]+)\}\}";

#if NET7_0_OR_GREATER
        /// <summary>
        /// Regex pattern for response variable references.
        /// Matches: {{name.response.body.$.path}} or {{name.response.headers.HeaderName}}
        /// </summary>
        [GeneratedRegex(ResponseReferencePattern, RegexOptions.CultureInvariant)]
        private static partial Regex ResponseReferenceRegex();
#else
        private static readonly Regex _responseReferenceRegex = new Regex(
            ResponseReferencePattern,
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static Regex ResponseReferenceRegex() => _responseReferenceRegex;
#endif

        #endregion

    }

}
