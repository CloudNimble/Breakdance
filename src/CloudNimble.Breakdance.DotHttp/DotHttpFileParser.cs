using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CloudNimble.Breakdance.DotHttp.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Parses .http files into structured <see cref="DotHttpFile"/> objects.
    /// </summary>
    /// <example>
    /// <code>
    /// var parser = new DotHttpFileParser();
    /// var file = parser.Parse(httpFileContent, "api.http");
    ///
    /// // Check for parse diagnostics
    /// foreach (var diagnostic in file.Diagnostics)
    /// {
    ///     Console.WriteLine($"{diagnostic.Location}: {diagnostic.GetMessage()}");
    /// }
    ///
    /// // Process requests
    /// foreach (var request in file.Requests)
    /// {
    ///     Console.WriteLine($"{request.Method} {request.Url}");
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Implements the full .http file specification as documented at
    /// https://learn.microsoft.com/en-us/aspnet/core/test/http-files
    /// including variables, request chaining, file references, and multi-line headers.
    /// </remarks>
    public sealed partial class DotHttpFileParser
    {

        #region Fields

        /// <summary>
        /// Diagnostic descriptor for HTTP request line errors (DOTHTTP001).
        /// </summary>
        internal static readonly DiagnosticDescriptor RequestLineErrorDescriptor = new(
            id: "DOTHTTP001",
            title: "HTTP request line error",
            messageFormat: "{0}",
            category: "DotHttp",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// Diagnostic descriptor for HTTP header errors (DOTHTTP002).
        /// </summary>
        internal static readonly DiagnosticDescriptor HeaderErrorDescriptor = new(
            id: "DOTHTTP002",
            title: "HTTP header error",
            messageFormat: "{0}",
            category: "DotHttp",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// Diagnostic descriptor for HTTP variable errors (DOTHTTP003).
        /// </summary>
        internal static readonly DiagnosticDescriptor VariableErrorDescriptor = new(
            id: "DOTHTTP003",
            title: "HTTP variable error",
            messageFormat: "{0}",
            category: "DotHttp",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// Diagnostic descriptor for HTTP body warnings (DOTHTTP004).
        /// </summary>
        internal static readonly DiagnosticDescriptor BodyWarningDescriptor = new(
            id: "DOTHTTP004",
            title: "HTTP body warning",
            messageFormat: "{0}",
            category: "DotHttp",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// Diagnostic descriptor for unknown HTTP method warnings (DOTHTTP005).
        /// </summary>
        internal static readonly DiagnosticDescriptor UnknownMethodWarningDescriptor = new(
            id: "DOTHTTP005",
            title: "Unknown HTTP method",
            messageFormat: "{0}",
            category: "DotHttp",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// Set of valid HTTP methods per the specification.
        /// </summary>
        private static readonly HashSet<string> ValidHttpMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "OPTIONS", "GET", "HEAD", "POST", "PUT", "PATCH", "DELETE", "TRACE", "CONNECT"
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses a .http file content into a structured model.
        /// </summary>
        /// <param name="content">The content of the .http file.</param>
        /// <param name="filePath">The file path for reference in the model.</param>
        /// <returns>A <see cref="DotHttpFile"/> containing the parsed requests, variables, and any parse errors.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <example>
        /// <code>
        /// var parser = new DotHttpFileParser();
        /// var content = File.ReadAllText("api.http");
        /// var file = parser.Parse(content, "api.http");
        /// </code>
        /// </example>
        public DotHttpFile Parse(string content, string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var file = new DotHttpFile
            {
                FilePath = filePath
            };

            if (string.IsNullOrWhiteSpace(content))
            {
                return file;
            }

            var lines = SplitLines(content);
            var context = new ParseContext(file, lines);

            ParseContent(context);

            return file;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Adds a diagnostic to the context.
        /// </summary>
        /// <param name="context">The parse context.</param>
        /// <param name="descriptor">The diagnostic descriptor.</param>
        /// <param name="message">The diagnostic message.</param>
        /// <param name="column">The column number (optional, 1-based).</param>
        internal static void AddDiagnostic(ParseContext context, DiagnosticDescriptor descriptor, string message, int column = 1)
        {
            var location = Location.Create(
                context.File.FilePath,
                new TextSpan(0, 0),
                new LinePositionSpan(
                    new LinePosition(context.LineNumber - 1, column - 1),
                    new LinePosition(context.LineNumber - 1, column)));

            context.File.Diagnostics.Add(Diagnostic.Create(descriptor, location, message));
        }

        /// <summary>
        /// Checks for response variable references in the given text and updates the request's dependency tracking.
        /// </summary>
        /// <param name="request">The request to update with dependency information.</param>
        /// <param name="text">The text to search for response references.</param>
        /// <remarks>
        /// Detects patterns like {{login.response.body.$.token}} and adds "login" to the request's DependsOn list.
        /// </remarks>
        internal static void CheckForResponseReferences(DotHttpRequest request, ReadOnlySpan<char> text)
        {
            if (text.IsEmpty)
            {
                return;
            }

            var textString = text.ToString();
            var matches = ResponseReferenceRegex().Matches(textString);

            foreach (Match match in matches)
            {
                request.HasResponseReferences = true;
                var referencedRequestName = match.Groups[1].Value;
                if (!request.DependsOn.Contains(referencedRequestName))
                {
                    request.DependsOn.Add(referencedRequestName);
                }
            }
        }

        /// <summary>
        /// Finalizes a request by setting its body content from accumulated lines.
        /// </summary>
        /// <param name="context">The parse context.</param>
        /// <param name="request">The request to finalize.</param>
        /// <param name="bodyLines">The accumulated body lines.</param>
        internal static void FinishRequest(ParseContext context, DotHttpRequest request, List<string> bodyLines)
        {
            if (bodyLines.Count == 0)
            {
                return;
            }

            // Trim trailing empty lines
            while (bodyLines.Count > 0 && string.IsNullOrWhiteSpace(bodyLines[^1]))
            {
                bodyLines.RemoveAt(bodyLines.Count - 1);
            }

            if (bodyLines.Count == 0)
            {
                return;
            }

            // Check for file reference syntax: < path/to/file
            // Note: We check Length > 1 to ensure there's at least one character after '<'
            // Since outer Trim() removes trailing whitespace, if Length > 1, the path will be non-empty
            var firstLine = bodyLines[0].AsSpan().Trim();
            if (firstLine.Length > 1 && firstLine[0] == '<')
            {
                var filePath = firstLine[1..].Trim().ToString();
                request.BodyFilePath = filePath;
                CheckForResponseReferences(request, filePath.AsSpan());

                // Warn if there are additional lines after file reference
                if (bodyLines.Count > 1)
                {
                    AddDiagnostic(context, BodyWarningDescriptor, "Content after file reference will be ignored");
                }
                return;
            }

            // Regular body content
            var body = string.Join("\n", bodyLines);
            request.Body = body;
            CheckForResponseReferences(request, body.AsSpan());
        }

        /// <summary>
        /// Parses all content in the context.
        /// </summary>
        /// <param name="context">The parse context.</param>
        internal static void ParseContent(ParseContext context)
        {
            DotHttpRequest currentRequest = null;
            var currentComments = new List<string>();
            string currentRequestName = null;
            string currentSeparatorTitle = null;
            var currentRequestVariables = new Dictionary<string, string>(StringComparer.Ordinal);
            var state = ParserState.Start;
            var bodyLines = new List<string>();
            var isFirstRequest = true;

            while (context.LineIndex < context.Lines.Length)
            {
                var line = context.Lines[context.LineIndex];
                var lineSpan = line.AsSpan();
                var trimmedSpan = lineSpan.Trim();

                // Check for request separator
                if (trimmedSpan.StartsWith("###".AsSpan()))
                {
                    // Finish current request if any
                    if (currentRequest is not null)
                    {
                        FinishRequest(context, currentRequest, bodyLines);
                        context.File.Requests.Add(currentRequest);
                    }

                    // Capture any text after ### as the separator title for the next request
                    var separatorText = trimmedSpan.Slice(3).Trim();
                    currentSeparatorTitle = separatorText.Length > 0 ? separatorText.ToString() : null;

                    // Reset state for next request
                    currentRequest = null;
                    currentComments.Clear();
                    currentRequestName = null;
                    currentRequestVariables.Clear();
                    state = ParserState.Start;
                    bodyLines.Clear();
                    isFirstRequest = false;
                    context.LineIndex++;
                    continue;
                }

                // Handle each parser state
                if (state == ParserState.Start)
                {
                    ProcessStartState(
                        context,
                        lineSpan,
                        trimmedSpan,
                        ref currentRequest,
                        ref currentRequestName,
                        ref currentSeparatorTitle,
                        ref state,
                        currentComments,
                        currentRequestVariables,
                        isFirstRequest);
                }
                else if (state == ParserState.InHeaders)
                {
                    ProcessHeaderState(context, lineSpan, trimmedSpan, currentRequest, ref state);
                }
                else
                {
                    // ParserState.InBody
                    bodyLines.Add(line);
                }

                context.LineIndex++;
            }

            // Handle final request if exists
            if (currentRequest is not null)
            {
                FinishRequest(context, currentRequest, bodyLines);
                context.File.Requests.Add(currentRequest);
            }
        }

        /// <summary>
        /// Processes a line when in the header parsing state.
        /// </summary>
        /// <param name="context">The parse context.</param>
        /// <param name="line">The original line with whitespace preserved.</param>
        /// <param name="trimmedLine">The trimmed line for comparison.</param>
        /// <param name="currentRequest">The current request being built.</param>
        /// <param name="state">The current parser state, may transition to InBody.</param>
        internal static void ProcessHeaderState(
            ParseContext context,
            ReadOnlySpan<char> line,
            ReadOnlySpan<char> trimmedLine,
            DotHttpRequest currentRequest,
            ref ParserState state)
        {
            // Empty line transitions to body
            if (trimmedLine.IsEmpty)
            {
                state = ParserState.InBody;
                return;
            }

            // Check for multi-line header continuation (starts with whitespace)
            // Note: Headers.Count > 0 ensures the foreach will find at least one header
            if (!line.IsEmpty && char.IsWhiteSpace(line[0]) && currentRequest.Headers.Count > 0)
            {
                // This is a continuation of the previous header
                // Find the last header and append this value
                string lastHeaderName = null;
                foreach (var key in currentRequest.Headers.Keys)
                {
                    lastHeaderName = key;
                }

                // lastHeaderName is guaranteed non-null because Headers.Count > 0
                currentRequest.Headers[lastHeaderName] += " " + trimmedLine.ToString();
                CheckForResponseReferences(currentRequest, trimmedLine);
                return;
            }

            // Parse header: Name: Value
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var headerName = line[..colonIndex].Trim().ToString();
                var headerValue = colonIndex < line.Length - 1
                    ? line[(colonIndex + 1)..].Trim().ToString()
                    : string.Empty;

                if (string.IsNullOrEmpty(headerName))
                {
                    AddDiagnostic(context, HeaderErrorDescriptor, "Invalid header: empty header name");
                    return;
                }

                currentRequest.Headers[headerName] = headerValue;
                CheckForResponseReferences(currentRequest, headerValue.AsSpan());
            }
            else
            {
                AddDiagnostic(context, HeaderErrorDescriptor, $"Invalid header format: '{trimmedLine.ToString()}'. Expected 'Name: Value'");
            }
        }

        /// <summary>
        /// Processes a line when in the start state (before a request line is found).
        /// </summary>
        /// <param name="context">The parse context.</param>
        /// <param name="line">The original line.</param>
        /// <param name="trimmedLine">The trimmed line to process.</param>
        /// <param name="currentRequest">The current request, set when a request line is found.</param>
        /// <param name="currentRequestName">The current request name from @name directive.</param>
        /// <param name="currentSeparatorTitle">The text after ### separator, used for test method naming.</param>
        /// <param name="state">The current parser state, may transition to InHeaders.</param>
        /// <param name="currentComments">The accumulated comments for the next request.</param>
        /// <param name="currentRequestVariables">The request-level variables.</param>
        /// <param name="isFirstRequest">Whether this is the first request (for file-level vs request-level variable handling).</param>
        internal static void ProcessStartState(
            ParseContext context,
            ReadOnlySpan<char> line,
            ReadOnlySpan<char> trimmedLine,
            ref DotHttpRequest currentRequest,
            ref string currentRequestName,
            ref string currentSeparatorTitle,
            ref ParserState state,
            List<string> currentComments,
            Dictionary<string, string> currentRequestVariables,
            bool isFirstRequest)
        {
            if (trimmedLine.IsEmpty)
            {
                return;
            }

            var trimmedString = trimmedLine.ToString();

            // Check for variable definition: @variableName=value
            if (trimmedLine[0] == '@')
            {
                var equalsIndex = trimmedLine.IndexOf('=');
                if (equalsIndex > 1)
                {
                    var variableName = trimmedLine[1..equalsIndex].Trim().ToString();
                    var variableValue = equalsIndex < trimmedLine.Length - 1
                        ? trimmedLine[(equalsIndex + 1)..].Trim().ToString()
                        : string.Empty;

                    if (string.IsNullOrEmpty(variableName))
                    {
                        AddDiagnostic(context, VariableErrorDescriptor, "Invalid variable: empty variable name");
                        return;
                    }

                    // Check for spaces in variable name
                    if (variableName.IndexOf(' ') >= 0)
                    {
                        AddDiagnostic(context, VariableErrorDescriptor, $"Invalid variable name '{variableName}': spaces are not allowed");
                        return;
                    }

                    // File-level variables go in File.Variables, request-level go in currentRequestVariables
                    if (isFirstRequest)
                    {
                        context.File.Variables[variableName] = variableValue;
                    }
                    else
                    {
                        currentRequestVariables[variableName] = variableValue;
                    }
                    return;
                }
                else
                {
                    AddDiagnostic(context, VariableErrorDescriptor, $"Invalid variable definition: '{trimmedString}'. Expected '@name=value'");
                    return;
                }
            }

            // Check for comment
            if (trimmedLine[0] == '#' || (trimmedLine.Length > 1 && trimmedLine[0] == '/' && trimmedLine[1] == '/'))
            {
                // Check for request name directive: # @name RequestName or // @name RequestName
                var nameMatch = RequestNameRegex().Match(trimmedString);
                if (nameMatch.Success)
                {
                    currentRequestName = nameMatch.Groups[1].Value;
                    return;
                }

                // Regular comment - preserve the original text after the comment marker
                string commentText;
                if (trimmedLine[0] == '#')
                {
                    commentText = trimmedLine.Length > 1 ? trimmedLine[1..].TrimStart().ToString() : string.Empty;
                }
                else
                {
                    // "//" comment
                    commentText = trimmedLine.Length > 2 ? trimmedLine[2..].TrimStart().ToString() : string.Empty;
                }
                currentComments.Add(commentText);
                return;
            }

            // Try to parse as request line: METHOD URL [HTTP/VERSION]
            var requestMatch = RequestLineRegex().Match(trimmedString);
            if (requestMatch.Success)
            {
                var method = requestMatch.Groups[1].Value.ToUpperInvariant();

                if (!ValidHttpMethods.Contains(method))
                {
                    AddDiagnostic(context, UnknownMethodWarningDescriptor, $"Unknown HTTP method: '{method}'");
                }

                currentRequest = new DotHttpRequest
                {
                    Method = method,
                    Url = requestMatch.Groups[2].Value,
                    HttpVersion = requestMatch.Groups[3].Success ? requestMatch.Groups[3].Value : null,
                    Name = currentRequestName,
                    SeparatorTitle = currentSeparatorTitle,
                    LineNumber = context.LineNumber,
                    Comments = new List<string>(currentComments)
                };

                // Copy request-level variables
                foreach (var kvp in currentRequestVariables)
                {
                    currentRequest.Variables[kvp.Key] = kvp.Value;
                }

                // Check for response references in URL
                CheckForResponseReferences(currentRequest, currentRequest.Url.AsSpan());

                currentComments.Clear();
                currentRequestName = null;
                currentSeparatorTitle = null;
                state = ParserState.InHeaders;
                return;
            }

            // Unknown line - could be a malformed request or other content
            // Only report as error if it looks like it might be a request
            if (char.IsLetter(trimmedLine[0]))
            {
                var firstWord = GetFirstWord(trimmedLine);
                if (ValidHttpMethods.Contains(firstWord.ToString()))
                {
                    AddDiagnostic(context, RequestLineErrorDescriptor, $"Malformed request line: '{trimmedString}'");
                }
            }
        }

        /// <summary>
        /// Splits content into lines, handling different line ending styles.
        /// </summary>
        /// <param name="content">The content to split.</param>
        /// <returns>An array of lines.</returns>
        internal static string[] SplitLines(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return Array.Empty<string>();
            }

            return content.Split(LineSeparators, StringSplitOptions.None);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the first word from a span.
        /// </summary>
        /// <param name="span">The span to extract from.</param>
        /// <returns>The first word.</returns>
        private static ReadOnlySpan<char> GetFirstWord(ReadOnlySpan<char> span)
        {
            var spaceIndex = span.IndexOf(' ');
            return spaceIndex > 0 ? span[..spaceIndex] : span;
        }

        #endregion

        #region Regex Patterns

        private static readonly string[] LineSeparators = ["\r\n", "\r", "\n"];

        // Pattern constants - single source of truth for both GeneratedRegex and compiled Regex
        private const string RequestLinePattern = @"^([A-Z]+)\s+(\S+)(?:\s+(HTTP/[\d.]+))?$";
        private const string RequestNamePattern = @"^(?:#|//)\s*@name\s+([\w-]+)";
        private const string ResponseReferencePattern = @"\{\{(\w+)\.(response|request)\.(body|headers)\.([^}]+)\}\}";

#if NET7_0_OR_GREATER
        /// <summary>
        /// Regex for parsing HTTP request lines. Matches: METHOD URL [HTTP/VERSION]
        /// </summary>
        [GeneratedRegex(RequestLinePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        internal static partial Regex RequestLineRegex();

        /// <summary>
        /// Regex for parsing request name directives. Matches: # @name RequestName or // @name RequestName
        /// </summary>
        [GeneratedRegex(RequestNamePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        internal static partial Regex RequestNameRegex();

        /// <summary>
        /// Regex for detecting response variable references. Matches: {{name.response.body.$.path}}
        /// </summary>
        [GeneratedRegex(ResponseReferencePattern, RegexOptions.CultureInvariant)]
        internal static partial Regex ResponseReferenceRegex();
#else
        private static readonly Regex _requestLineRegex = new Regex(
            RequestLinePattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex _requestNameRegex = new Regex(
            RequestNamePattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex _responseReferenceRegex = new Regex(
            ResponseReferencePattern,
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal static Regex RequestLineRegex() => _requestLineRegex;
        internal static Regex RequestNameRegex() => _requestNameRegex;
        internal static Regex ResponseReferenceRegex() => _responseReferenceRegex;
#endif

        #endregion

    }

}
