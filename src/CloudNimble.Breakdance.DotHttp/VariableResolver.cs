// This file contains runtime code, not analyzer code. Environment variable access is allowed at runtime.
#pragma warning disable RS1035

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Resolves {{variable}} placeholders in .http file content.
    /// </summary>
    /// <example>
    /// <code>
    /// var resolver = new VariableResolver();
    /// resolver.SetVariable("baseUrl", "https://api.example.com");
    /// resolver.SetVariable("apiKey", "my-secret-key");
    /// var result = resolver.Resolve("GET {{baseUrl}}/users?key={{apiKey}}");
    /// // result = "GET https://api.example.com/users?key=my-secret-key"
    /// </code>
    /// </example>
    /// <remarks>
    /// Supports simple variables, dynamic variables ($datetime, $randomInt, etc.),
    /// and response references for request chaining per the Microsoft .http file specification.
    /// </remarks>
    public sealed partial class VariableResolver
    {

        #region Fields

        private readonly Dictionary<string, string> _variables = new(StringComparer.Ordinal);
        private readonly Random _random = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears all stored variables.
        /// </summary>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// resolver.SetVariable("foo", "bar");
        /// resolver.Clear();
        /// // All variables are now removed
        /// </code>
        /// </example>
        public void Clear()
        {
            _variables.Clear();
        }

        /// <summary>
        /// Extracts all response reference names from the input.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>Set of request names that are referenced.</returns>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// var input = "Authorization: Bearer {{login.response.body.$.token}}";
        /// var names = resolver.GetResponseReferenceNames(input);
        /// // names contains "login"
        /// </code>
        /// </example>
        public HashSet<string> GetResponseReferenceNames(string input)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrEmpty(input))
            {
                return names;
            }

            var matches = ResponseReferenceRegex().Matches(input);
            foreach (Match match in matches)
            {
                names.Add(match.Groups[1].Value);
            }

            return names;
        }

        /// <summary>
        /// Extracts all variable names from the input string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>Set of variable names found.</returns>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// var input = "{{baseUrl}}/users/{{userId}}";
        /// var names = resolver.GetVariableNames(input);
        /// // names contains "baseUrl" and "userId"
        /// </code>
        /// </example>
        public HashSet<string> GetVariableNames(string input)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrEmpty(input))
            {
                return names;
            }

            var matches = SimpleVariableRegex().Matches(input);
            foreach (Match match in matches)
            {
                names.Add(match.Groups[1].Value);
            }

            return names;
        }

        /// <summary>
        /// Checks if a string contains response references ({{name.response.*}}).
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if response references are present.</returns>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// var hasRefs = resolver.HasResponseReferences("Bearer {{login.response.body.$.token}}");
        /// // hasRefs = true
        /// </code>
        /// </example>
        public bool HasResponseReferences(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return ResponseReferenceRegex().IsMatch(input);
        }

        /// <summary>
        /// Checks if a string contains any unresolved variable references.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if unresolved variables remain.</returns>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// resolver.SetVariable("baseUrl", "https://api.example.com");
        /// var resolved = resolver.Resolve("{{baseUrl}}/{{userId}}");
        /// var hasUnresolved = resolver.HasUnresolvedVariables(resolved);
        /// // hasUnresolved = true (userId was not set)
        /// </code>
        /// </example>
        public bool HasUnresolvedVariables(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return SimpleVariableRegex().IsMatch(input) ||
                   ResponseReferenceRegex().IsMatch(input) ||
                   DynamicVariableRegex().IsMatch(input);
        }

        /// <summary>
        /// Resolves all variable references in the input string.
        /// </summary>
        /// <param name="input">The input string containing {{variable}} placeholders.</param>
        /// <returns>The resolved string with variables replaced.</returns>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// resolver.SetVariable("baseUrl", "https://api.example.com");
        /// var result = resolver.Resolve("GET {{baseUrl}}/users");
        /// // result = "GET https://api.example.com/users"
        /// </code>
        /// </example>
        public string Resolve(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // First resolve dynamic variables
            var result = ResolveDynamicVariables(input);

            // Then resolve simple variables
            result = ResolveSimpleVariables(result);

            return result;
        }

        /// <summary>
        /// Resolves dynamic variables like $datetime, $randomInt, $timestamp, etc.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The resolved string.</returns>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// var result = resolver.ResolveDynamicVariables("ID: {{$guid}}");
        /// // result = "ID: 550e8400-e29b-41d4-a716-446655440000" (example GUID)
        /// </code>
        /// </example>
        public string ResolveDynamicVariables(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return DynamicVariableRegex().Replace(input, match =>
            {
                var functionName = match.Groups[1].Value.ToLowerInvariant();
                var arguments = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

                return ResolveDynamicVariable(functionName, arguments);
            });
        }

        /// <summary>
        /// Resolves simple {{variable}} placeholders.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The resolved string.</returns>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// resolver.SetVariable("name", "John");
        /// var result = resolver.ResolveSimpleVariables("Hello, {{name}}!");
        /// // result = "Hello, John!"
        /// </code>
        /// </example>
        public string ResolveSimpleVariables(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return SimpleVariableRegex().Replace(input, match =>
            {
                var variableName = match.Groups[1].Value;
                if (_variables.TryGetValue(variableName, out var value))
                {
                    return value;
                }
                // Return original if not found (may be resolved later or is invalid)
                return match.Value;
            });
        }

        /// <summary>
        /// Sets a variable value for resolution.
        /// </summary>
        /// <param name="name">The variable name (without @ prefix or {{}} wrapper).</param>
        /// <param name="value">The variable value.</param>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// resolver.SetVariable("baseUrl", "https://api.example.com");
        /// </code>
        /// </example>
        public void SetVariable(string name, string value)
        {
            _variables[name] = value;
        }

        /// <summary>
        /// Sets multiple variables at once.
        /// </summary>
        /// <param name="variables">Dictionary of variable names and values.</param>
        /// <example>
        /// <code>
        /// var resolver = new VariableResolver();
        /// resolver.SetVariables(new Dictionary&lt;string, string&gt;
        /// {
        ///     ["baseUrl"] = "https://api.example.com",
        ///     ["apiKey"] = "secret-key"
        /// });
        /// </code>
        /// </example>
        public void SetVariables(IDictionary<string, string> variables)
        {
            if (variables is null)
            {
                return;
            }

            foreach (var kvp in variables)
            {
                _variables[kvp.Key] = kvp.Value;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Applies an offset to a DateTimeOffset.
        /// </summary>
        /// <param name="dateTime">The base datetime.</param>
        /// <param name="offsetValue">The numeric offset value.</param>
        /// <param name="offsetUnit">The offset unit (ms, s, m, h, d, w, M, y).</param>
        /// <returns>The adjusted datetime.</returns>
        /// <remarks>
        /// Supported units: ms (milliseconds), s (seconds), m (minutes), h (hours),
        /// d (days), w (weeks), M (months), y (years).
        /// </remarks>
        internal static DateTimeOffset ApplyOffset(DateTimeOffset dateTime, int offsetValue, string offsetUnit)
        {
            return offsetUnit switch
            {
                "ms" => dateTime.AddMilliseconds(offsetValue),
                "s" => dateTime.AddSeconds(offsetValue),
                "m" => dateTime.AddMinutes(offsetValue),
                "h" => dateTime.AddHours(offsetValue),
                "d" => dateTime.AddDays(offsetValue),
                "w" => dateTime.AddDays(offsetValue * 7),
                "M" => dateTime.AddMonths(offsetValue),
                "y" => dateTime.AddYears(offsetValue),
                _ => dateTime
            };
        }

        /// <summary>
        /// Parses offset arguments from a dynamic variable.
        /// </summary>
        /// <param name="arguments">The arguments string (e.g., "rfc1123 1 d" or "-5 h").</param>
        /// <param name="format">Output: the format string, if any.</param>
        /// <param name="offsetValue">Output: the offset value.</param>
        /// <param name="offsetUnit">Output: the offset unit.</param>
        /// <remarks>
        /// Offset syntax is "number unit" where unit is ms|s|m|h|d|w|M|y.
        /// Format can be "rfc1123", "iso8601", or a custom format string in quotes.
        /// </remarks>
        internal static void ParseDateTimeArguments(string arguments, out string format, out int offsetValue, out string offsetUnit)
        {
            format = null;
            offsetValue = 0;
            offsetUnit = null;

            if (string.IsNullOrWhiteSpace(arguments))
            {
                return;
            }

            var parts = arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var index = 0;

            // Check for format (rfc1123, iso8601, or quoted string)
            if (parts.Length > 0)
            {
                var firstPart = parts[0];

                // Check for quoted format string
                if (firstPart.StartsWith("\"") || firstPart.StartsWith("'"))
                {
                    // Find the closing quote
                    var quoteChar = firstPart[0];
                    var formatBuilder = new System.Text.StringBuilder();
                    formatBuilder.Append(firstPart[1..]);

                    while (index < parts.Length)
                    {
                        var part = parts[index];
                        if (part.EndsWith(quoteChar.ToString()))
                        {
                            if (index == 0)
                            {
                                format = part[1..^1];
                            }
                            else
                            {
                                formatBuilder.Append(' ');
                                formatBuilder.Append(part[..^1]);
                                format = formatBuilder.ToString();
                            }
                            index++;
                            break;
                        }
                        else if (index > 0)
                        {
                            formatBuilder.Append(' ');
                            formatBuilder.Append(part);
                        }
                        index++;
                    }
                }
                // Check for named format
                else if (firstPart.Equals("rfc1123", StringComparison.OrdinalIgnoreCase) ||
                         firstPart.Equals("iso8601", StringComparison.OrdinalIgnoreCase))
                {
                    format = firstPart.ToLowerInvariant();
                    index++;
                }
                // Check if it's a number (offset without format)
                else if (int.TryParse(firstPart, out _))
                {
                    // No format, starts with offset
                }
                else
                {
                    // Assume it's a custom format without quotes
                    format = firstPart;
                    index++;
                }
            }

            // Parse offset: number unit
            if (index < parts.Length && int.TryParse(parts[index], out var parsedOffset))
            {
                offsetValue = parsedOffset;
                index++;

                if (index < parts.Length)
                {
                    offsetUnit = parts[index];
                }
            }
        }

        /// <summary>
        /// Resolves a $datetime variable with optional format and offset.
        /// </summary>
        /// <param name="arguments">Optional arguments: format and/or offset.</param>
        /// <returns>The formatted UTC datetime string.</returns>
        /// <example>
        /// <code>
        /// // {{$datetime}} -> ISO 8601 format
        /// // {{$datetime rfc1123}} -> RFC 1123 format
        /// // {{$datetime "dd-MM-yyyy"}} -> Custom format
        /// // {{$datetime rfc1123 1 d}} -> Tomorrow in RFC 1123 format
        /// // {{$datetime iso8601 -1 y}} -> One year ago in ISO 8601 format
        /// </code>
        /// </example>
        internal string ResolveDatetime(string arguments)
        {
            ParseDateTimeArguments(arguments, out var format, out var offsetValue, out var offsetUnit);

            var now = DateTimeOffset.UtcNow;

            if (offsetUnit is not null)
            {
                now = ApplyOffset(now, offsetValue, offsetUnit);
            }

            return FormatDateTime(now, format);
        }

        /// <summary>
        /// Resolves a .env file variable.
        /// </summary>
        /// <param name="variableName">The name of the variable to resolve.</param>
        /// <returns>The variable value, or empty string if not found.</returns>
        /// <remarks>
        /// Currently falls back to process environment variables.
        /// Full .env file loading would need project-specific implementation.
        /// </remarks>
        internal string ResolveDotEnv(string variableName)
        {
            // .env file loading would need to be implemented based on project requirements
            // For now, fall back to process environment
            return ResolveProcessEnv(variableName);
        }

        /// <summary>
        /// Resolves a dynamic variable by function name and optional arguments.
        /// </summary>
        /// <param name="functionName">The dynamic variable function name (e.g., "datetime", "guid").</param>
        /// <param name="arguments">Optional arguments for the function.</param>
        /// <returns>The resolved value.</returns>
        /// <remarks>
        /// Supported functions: datetime, localDatetime, timestamp, randomInt, guid, processEnv, dotEnv.
        /// </remarks>
        internal string ResolveDynamicVariable(string functionName, string arguments)
        {
            switch (functionName)
            {
                case "datetime":
                    return ResolveDatetime(arguments);

                case "localdatetime":
                    return ResolveLocalDatetime(arguments);

                case "timestamp":
                    return ResolveTimestamp(arguments);

                case "randomint":
                    return ResolveRandomInt(arguments);

                case "guid":
                    return Guid.NewGuid().ToString();

                case "processenv":
                    return ResolveProcessEnv(arguments);

                case "dotenv":
                    return ResolveDotEnv(arguments);

                default:
                    // Return original for unknown dynamic variables
                    return arguments is not null ? $"{{{{${functionName} {arguments}}}}}" : $"{{{{${functionName}}}}}";
            }
        }

        /// <summary>
        /// Resolves a $localDatetime variable with optional format and offset.
        /// </summary>
        /// <param name="arguments">Optional arguments: format and/or offset.</param>
        /// <returns>The formatted local datetime string with timezone offset.</returns>
        /// <example>
        /// <code>
        /// // {{$localDatetime}} -> Local time with timezone
        /// // {{$localDatetime "dd-MM-yyyy"}} -> Custom format
        /// // {{$localDatetime 1 h}} -> One hour from now
        /// </code>
        /// </example>
        internal string ResolveLocalDatetime(string arguments)
        {
            ParseDateTimeArguments(arguments, out var format, out var offsetValue, out var offsetUnit);

            var now = DateTimeOffset.Now;

            if (offsetUnit is not null)
            {
                now = ApplyOffset(now, offsetValue, offsetUnit);
            }

            return FormatLocalDateTime(now, format);
        }

        /// <summary>
        /// Resolves a $processEnv variable from environment variables.
        /// </summary>
        /// <param name="variableName">The name of the environment variable.</param>
        /// <returns>The environment variable value, or empty string if not found.</returns>
        internal string ResolveProcessEnv(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                return string.Empty;
            }

            return Environment.GetEnvironmentVariable(variableName.Trim()) ?? string.Empty;
        }

        /// <summary>
        /// Resolves a $randomInt variable with optional min/max range.
        /// </summary>
        /// <param name="arguments">Optional argument in format "max" or "min max".</param>
        /// <returns>A random integer as a string.</returns>
        /// <example>
        /// <code>
        /// // {{$randomInt}} -> random int from 0 to int.MaxValue
        /// // {{$randomInt 100}} -> random int from 0 to 100
        /// // {{$randomInt 10 100}} -> random int from 10 to 100
        /// </code>
        /// </example>
        internal string ResolveRandomInt(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                return _random.Next(0, int.MaxValue).ToString();
            }

            // Parse min max format: "min max" or just "max"
            var parts = arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1 && int.TryParse(parts[0], out var max))
            {
                return _random.Next(0, max).ToString();
            }

            if (parts.Length >= 2 &&
                int.TryParse(parts[0], out var min) &&
                int.TryParse(parts[1], out var maxVal))
            {
                return _random.Next(min, maxVal).ToString();
            }

            return _random.Next(0, int.MaxValue).ToString();
        }

        /// <summary>
        /// Resolves a $timestamp variable with optional offset.
        /// </summary>
        /// <param name="arguments">Optional offset arguments (e.g., "1 d" for one day from now).</param>
        /// <returns>The Unix timestamp as a string.</returns>
        /// <example>
        /// <code>
        /// // {{$timestamp}} -> Current Unix timestamp
        /// // {{$timestamp 1 d}} -> Tomorrow's Unix timestamp
        /// // {{$timestamp -1 h}} -> One hour ago
        /// </code>
        /// </example>
        internal string ResolveTimestamp(string arguments)
        {
            var now = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(arguments))
            {
                ParseDateTimeArguments(arguments, out _, out var offsetValue, out var offsetUnit);

                if (offsetUnit is not null)
                {
                    now = ApplyOffset(now, offsetValue, offsetUnit);
                }
            }

            return now.ToUnixTimeSeconds().ToString();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Formats a datetime according to the specified format.
        /// </summary>
        /// <param name="dateTime">The datetime to format.</param>
        /// <param name="format">The format string (rfc1123, iso8601, or custom).</param>
        /// <returns>The formatted datetime string.</returns>
        private static string FormatDateTime(DateTimeOffset dateTime, string format)
        {
            if (string.IsNullOrEmpty(format) || format == "iso8601")
            {
                return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            }

            if (format == "rfc1123")
            {
                return dateTime.ToString("R", CultureInfo.InvariantCulture);
            }

            try
            {
                return dateTime.ToString(format, CultureInfo.InvariantCulture);
            }
            catch
            {
                return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Formats a local datetime according to the specified format.
        /// </summary>
        /// <param name="dateTime">The datetime to format.</param>
        /// <param name="format">The format string (rfc1123, iso8601, or custom).</param>
        /// <returns>The formatted datetime string with timezone offset.</returns>
        private static string FormatLocalDateTime(DateTimeOffset dateTime, string format)
        {
            if (string.IsNullOrEmpty(format) || format == "iso8601")
            {
                return dateTime.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
            }

            if (format == "rfc1123")
            {
                return dateTime.ToString("R", CultureInfo.InvariantCulture);
            }

            try
            {
                return dateTime.ToString(format, CultureInfo.InvariantCulture);
            }
            catch
            {
                return dateTime.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
            }
        }

        #endregion

        #region Regex Patterns

        // Pattern constants - single source of truth for both GeneratedRegex and compiled Regex
        private const string DynamicVariablePattern = @"\{\{\$(\w+)(?:\s+(.+?))?\}\}";
        private const string ResponseReferencePattern = @"\{\{(\w+)\.(response|request)\.(body|headers)\.([^}]+)\}\}";
        private const string SimpleVariablePattern = @"\{\{(\w+)\}\}";

#if NET7_0_OR_GREATER
        /// <summary>
        /// Regex for dynamic variables. Matches: {{$functionName arguments}}
        /// </summary>
        [GeneratedRegex(DynamicVariablePattern, RegexOptions.CultureInvariant)]
        internal static partial Regex DynamicVariableRegex();

        /// <summary>
        /// Regex for response variable references. Matches: {{name.response.body.$.path}}
        /// </summary>
        [GeneratedRegex(ResponseReferencePattern, RegexOptions.CultureInvariant)]
        internal static partial Regex ResponseReferenceRegex();

        /// <summary>
        /// Regex for simple variable references. Matches: {{variableName}}
        /// </summary>
        [GeneratedRegex(SimpleVariablePattern, RegexOptions.CultureInvariant)]
        internal static partial Regex SimpleVariableRegex();
#else
        private static readonly Regex _dynamicVariableRegex = new Regex(
            DynamicVariablePattern,
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex _responseReferenceRegex = new Regex(
            ResponseReferencePattern,
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex _simpleVariableRegex = new Regex(
            SimpleVariablePattern,
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal static Regex DynamicVariableRegex() => _dynamicVariableRegex;
        internal static Regex ResponseReferenceRegex() => _responseReferenceRegex;
        internal static Regex SimpleVariableRegex() => _simpleVariableRegex;
#endif

        #endregion

    }

}
