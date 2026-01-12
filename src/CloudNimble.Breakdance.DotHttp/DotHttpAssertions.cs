using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Provides smart assertion helpers for HTTP responses that go beyond simple status code checking.
    /// </summary>
    /// <example>
    /// <code>
    /// var response = await httpClient.SendAsync(request);
    ///
    /// // Validate response meets common API expectations
    /// await DotHttpAssertions.AssertValidResponseAsync(response);
    ///
    /// // Or with custom options
    /// await DotHttpAssertions.AssertValidResponseAsync(response,
    ///     checkStatusCode: true,
    ///     checkContentType: true,
    ///     checkBodyForErrors: true);
    /// </code>
    /// </example>
    /// <remarks>
    /// Detects common API error patterns like 200 OK responses that contain error payloads.
    /// </remarks>
    public static class DotHttpAssertions
    {

        #region Constants

        /// <summary>
        /// The default maximum length for body preview in error messages.
        /// </summary>
        public const int DefaultMaxBodyPreviewLength = 500;

        #endregion

        #region Public Methods

        /// <summary>
        /// Validates that the response body contains the specified text.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="expectedText">The text that should be present in the body.</param>
        /// <param name="maxBodyPreviewLength">Maximum length of body content to include in error messages.</param>
        /// <example>
        /// <code>
        /// await DotHttpAssertions.AssertBodyContainsAsync(response, "\"success\":true");
        /// </code>
        /// </example>
        public static async Task AssertBodyContainsAsync(HttpResponseMessage response, string expectedText, int maxBodyPreviewLength = DefaultMaxBodyPreviewLength)
        {
            // On .NET Framework 4.8, Content may be null
            var body = response.Content != null
                ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                : string.Empty;

            if (!body.Contains(expectedText))
            {
                throw new DotHttpAssertionException(
                    $"Expected response body to contain '{expectedText}' but it was not found. " +
                    $"Body preview: {Truncate(body, maxBodyPreviewLength)}");
            }
        }

        /// <summary>
        /// Validates that the response Content-Type matches the expected value.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="expectedContentType">The expected Content-Type (e.g., "application/json").</param>
        /// <example>
        /// <code>
        /// DotHttpAssertions.AssertContentType(response, "application/json");
        /// </code>
        /// </example>
        public static void AssertContentType(HttpResponseMessage response, string expectedContentType)
        {
            var actualContentType = response.Content?.Headers?.ContentType?.MediaType;

            if (actualContentType is null)
            {
                throw new DotHttpAssertionException(
                    $"Expected Content-Type '{expectedContentType}' but no Content-Type header was present.");
            }

            if (!actualContentType.Equals(expectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new DotHttpAssertionException(
                    $"Expected Content-Type '{expectedContentType}' but got '{actualContentType}'.");
            }
        }

        /// <summary>
        /// Validates that the response contains a specific header.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="headerName">The expected header name.</param>
        /// <param name="expectedValue">Optional expected header value.</param>
        /// <example>
        /// <code>
        /// DotHttpAssertions.AssertHeader(response, "X-Request-Id");
        /// DotHttpAssertions.AssertHeader(response, "Cache-Control", "no-store");
        /// </code>
        /// </example>
        public static void AssertHeader(HttpResponseMessage response, string headerName, string expectedValue = null)
        {
            string actualValue = null;

            if (response.Headers.TryGetValues(headerName, out var values))
            {
                actualValue = string.Join(", ", values);
            }
            else if (response.Content?.Headers is not null)
            {
                if (response.Content.Headers.TryGetValues(headerName, out var contentValues))
                {
                    actualValue = string.Join(", ", contentValues);
                }
            }

            if (actualValue is null)
            {
                throw new DotHttpAssertionException(
                    $"Expected header '{headerName}' but it was not present in the response.");
            }

            if (expectedValue is not null && !actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                throw new DotHttpAssertionException(
                    $"Expected header '{headerName}' to have value '{expectedValue}' but got '{actualValue}'.");
            }
        }

        /// <summary>
        /// Validates that the response body does not contain error patterns.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="maxBodyPreviewLength">Maximum length of body content to include in error messages.</param>
        /// <example>
        /// <code>
        /// await DotHttpAssertions.AssertNoErrorsInBodyAsync(response);
        /// </code>
        /// </example>
        public static async Task AssertNoErrorsInBodyAsync(HttpResponseMessage response, int maxBodyPreviewLength = DefaultMaxBodyPreviewLength)
        {
            // On .NET Framework 4.8, Content may be null
            var body = response.Content != null
                ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                : string.Empty;
            CheckForErrorPatterns(body, response.StatusCode, maxBodyPreviewLength);
        }

        /// <summary>
        /// Validates that the response status code matches the expected value.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="expectedStatusCode">The expected status code.</param>
        /// <param name="maxBodyPreviewLength">Maximum length of body content to include in error messages.</param>
        /// <example>
        /// <code>
        /// await DotHttpAssertions.AssertStatusCodeAsync(response, 201); // Created
        /// await DotHttpAssertions.AssertStatusCodeAsync(response, 204); // No Content
        /// </code>
        /// </example>
        public static async Task AssertStatusCodeAsync(HttpResponseMessage response, int expectedStatusCode, int maxBodyPreviewLength = DefaultMaxBodyPreviewLength)
        {
            if ((int)response.StatusCode != expectedStatusCode)
            {
                var bodyPreview = await GetBodyPreviewAsync(response, maxBodyPreviewLength).ConfigureAwait(false);
                throw new DotHttpAssertionException(
                    $"Expected status code {expectedStatusCode} but got {(int)response.StatusCode} {response.StatusCode}. " +
                    $"Body preview: {bodyPreview}");
            }
        }

        /// <summary>
        /// Validates that the response meets common API contract expectations.
        /// </summary>
        /// <param name="response">The HTTP response to validate.</param>
        /// <param name="checkStatusCode">Whether to check the status code for success. Default is true.</param>
        /// <param name="checkContentType">Whether to verify Content-Type is present when body exists. Default is true.</param>
        /// <param name="checkBodyForErrors">Whether to check for error patterns in the response body. Default is true.</param>
        /// <param name="logResponseOnFailure">Whether to include the response body in failure messages. Default is true.</param>
        /// <param name="maxBodyPreviewLength">Maximum length of body content to include in error messages. Default is 500.</param>
        /// <returns>A task that completes when validation is done.</returns>
        /// <exception cref="DotHttpAssertionException">Thrown when an assertion fails.</exception>
        /// <example>
        /// <code>
        /// var response = await httpClient.SendAsync(request);
        /// await DotHttpAssertions.AssertValidResponseAsync(response);
        /// </code>
        /// </example>
        public static async Task AssertValidResponseAsync(
            HttpResponseMessage response,
            bool checkStatusCode = true,
            bool checkContentType = true,
            bool checkBodyForErrors = true,
            bool logResponseOnFailure = true,
            int maxBodyPreviewLength = DefaultMaxBodyPreviewLength)
        {
            // 1. Status code check
            if (checkStatusCode && !response.IsSuccessStatusCode)
            {
                var message = $"Expected success status code but got {(int)response.StatusCode} {response.StatusCode}.";
                if (logResponseOnFailure)
                {
                    var bodyPreview = await GetBodyPreviewAsync(response, maxBodyPreviewLength).ConfigureAwait(false);
                    message += $" Body preview: {bodyPreview}";
                }

                throw new DotHttpAssertionException(message);
            }

            // 2. Content-Type validation
            if (checkContentType)
            {
                var contentLength = response.Content?.Headers?.ContentLength ?? 0;
                if (contentLength > 0 && response.Content?.Headers?.ContentType is null)
                {
                    throw new DotHttpAssertionException(
                        "Response has content but no Content-Type header was specified.");
                }
            }

            // 3. Check for error patterns in body
            if (checkBodyForErrors && response.IsSuccessStatusCode && response.Content != null)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                CheckForErrorPatterns(body, response.StatusCode, logResponseOnFailure ? maxBodyPreviewLength : 0);
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Checks for common error patterns in response body content.
        /// </summary>
        /// <param name="body">The response body content.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="maxBodyPreviewLength">Maximum length for body preview in error messages.</param>
        /// <remarks>
        /// Detects patterns like "error":", "errors":", "success":false, etc.
        /// </remarks>
        internal static void CheckForErrorPatterns(string body, System.Net.HttpStatusCode statusCode, int maxBodyPreviewLength)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            // Common API error patterns to detect
            var errorPatterns = new[]
            {
                ("\"error\":", "JSON error field detected"),
                ("\"errors\":", "JSON errors array detected"),
                ("\"success\":false", "Success flag is false"),
                ("\"success\": false", "Success flag is false"),
                ("\"status\":\"error\"", "Status field indicates error"),
                ("\"status\": \"error\"", "Status field indicates error"),
                ("<error>", "XML error element detected"),
                ("<Error>", "XML Error element detected"),
                ("\"fault\":", "JSON fault field detected"),
                ("\"Fault\":", "JSON Fault field detected"),
            };

            foreach (var (pattern, message) in errorPatterns)
            {
                if (body.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    var errorMessage = $"{message} in {(int)statusCode} {statusCode} response.";
                    if (maxBodyPreviewLength > 0)
                    {
                        errorMessage += $" Body preview: {Truncate(body, maxBodyPreviewLength)}";
                    }

                    throw new DotHttpAssertionException(errorMessage);
                }
            }
        }

        /// <summary>
        /// Gets a preview of the response body for error messages.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="maxLength">Maximum preview length.</param>
        /// <returns>A truncated body preview string.</returns>
        internal static async Task<string> GetBodyPreviewAsync(HttpResponseMessage response, int maxLength)
        {
            // On .NET Framework 4.8, HttpResponseMessage.Content is null by default.
            // On modern .NET, it's initialized to EmptyContent.
            // Return "(empty)" for both cases for consistency.
            if (response.Content is null)
            {
                return "(empty)";
            }

            try
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return Truncate(body, maxLength);
            }
            catch
            {
                return "(unable to read body)";
            }
        }

        /// <summary>
        /// Truncates a string to the specified maximum length.
        /// </summary>
        /// <param name="value">The string to truncate.</param>
        /// <param name="maxLength">Maximum length.</param>
        /// <returns>The truncated string with "..." suffix if truncated.</returns>
        internal static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "(empty)";
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength) + "...";
        }

        #endregion

    }

}
