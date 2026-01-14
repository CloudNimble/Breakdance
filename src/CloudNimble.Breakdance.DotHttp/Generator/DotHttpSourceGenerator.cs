using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CloudNimble.Breakdance.DotHttp.Models;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CloudNimble.Breakdance.DotHttp.Generator
{

    /// <summary>
    /// Source generator that creates test classes from .http files.
    /// </summary>
    /// <example>
    /// <code>
    /// // Given an api.http file with:
    /// // @baseUrl = https://api.example.com
    /// //
    /// // ### Get all users
    /// // # @name GetAllUsers
    /// // GET {{baseUrl}}/users
    /// // Accept: application/json
    /// //
    /// // The generator produces ApiTests.g.cs with:
    /// // [TestClass]
    /// // public partial class ApiTests : DotHttpTestBase
    /// // {
    /// //     [TestMethod]
    /// //     public async Task GetAllUsers() { ... }
    /// // }
    /// </code>
    /// </example>
    /// <remarks>
    /// Supports MSTest and XUnit frameworks via the TestFramework configuration property.
    /// Generated classes are partial to allow custom setup and assertion overrides.
    /// </remarks>
    [Generator(LanguageNames.CSharp)]
    public sealed class DotHttpSourceGenerator : IIncrementalGenerator
    {

        #region Fields

        /// <summary>
        /// Matches variable references like {{variableName}}.
        /// </summary>
        private static readonly Regex VariableReferenceRegex = new(@"\{\{[^}]+\}\}", RegexOptions.Compiled);

        /// <summary>
        /// Matches HTTP/HTTPS protocol prefixes.
        /// </summary>
        private static readonly Regex ProtocolPrefixRegex = new(@"^https?://", RegexOptions.Compiled);

        /// <summary>
        /// Matches any character that is not valid in a C# identifier.
        /// </summary>
        private static readonly Regex InvalidIdentifierCharRegex = new(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);

        /// <summary>
        /// Matches consecutive underscores.
        /// </summary>
        private static readonly Regex ConsecutiveUnderscoreRegex = new(@"_+", RegexOptions.Compiled);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get configuration from build properties
            var config = context.AnalyzerConfigOptionsProvider
                .Select((provider, _) => GetConfiguration(provider.GlobalOptions));

            // Get all .http files
            var httpFiles = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(".http", StringComparison.OrdinalIgnoreCase));

            // Parse each file
            var parsed = httpFiles.Select((file, ct) =>
            {
                var content = file.GetText(ct)?.ToString() ?? "";
                var parser = new DotHttpFileParser();
                return (File: parser.Parse(content, file.Path), AdditionalText: file);
            });

            // Combine parsed files with config
            var combined = parsed.Collect().Combine(config);

            // Generate source for each file
            context.RegisterSourceOutput(combined, (spc, source) =>
            {
                var (files, cfg) = source;

                foreach (var (file, additionalText) in files)
                {
                    // Report parse diagnostics
                    ReportDiagnostics(spc, file);

                    if (file.Requests.Count == 0)
                    {
                        continue;
                    }

                    var code = GenerateTestClass(file, cfg);
                    var fileName = SanitizeClassName(Path.GetFileNameWithoutExtension(file.FilePath));
                    spc.AddSource($"{fileName}Tests.g.cs", SourceText.From(code, Encoding.UTF8));
                }
            });
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Escapes a string value for use in a C# string literal.
        /// Uses single-pass span iteration to avoid intermediate string allocations.
        /// </summary>
        /// <param name="value">The value to escape.</param>
        /// <returns>The escaped string.</returns>
        internal static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // Quick check if any escaping is needed
            var span = value.AsSpan();
            var needsEscaping = false;
            foreach (var c in span)
            {
                if (c is '\\' or '"' or '\r' or '\n' or '\t')
                {
                    needsEscaping = true;
                    break;
                }
            }

            if (!needsEscaping)
            {
                return value;
            }

            // Single pass to build escaped string
            var sb = new StringBuilder(value.Length + 16);
            foreach (var c in span)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Escapes a string value for use in a C# verbatim string literal.
        /// Uses span-based check to avoid allocation when no escaping needed.
        /// </summary>
        /// <param name="value">The value to escape.</param>
        /// <returns>The escaped string for verbatim literal.</returns>
        internal static string EscapeVerbatim(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // Quick check if any escaping is needed
            if (value.AsSpan().IndexOf('"') < 0)
            {
                return value;
            }

            // For verbatim strings, only double-quotes need escaping
            var sb = new StringBuilder(value.Length + 8);
            foreach (var c in value.AsSpan())
            {
                if (c == '"')
                {
                    sb.Append("\"\"");
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Escapes a string value for use in XML documentation.
        /// Uses single-pass span iteration to avoid intermediate string allocations.
        /// </summary>
        /// <param name="value">The value to escape.</param>
        /// <returns>The XML-escaped string.</returns>
        internal static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // Quick check if any escaping is needed
            var span = value.AsSpan();
            var needsEscaping = false;
            foreach (var c in span)
            {
                if (c is '&' or '<' or '>')
                {
                    needsEscaping = true;
                    break;
                }
            }

            if (!needsEscaping)
            {
                return value;
            }

            // Single pass to build escaped string
            var sb = new StringBuilder(value.Length + 16);
            foreach (var c in span)
            {
                switch (c)
                {
                    case '&':
                        sb.Append("&amp;");
                        break;
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates assertion code based on configuration.
        /// </summary>
        /// <param name="sb">The string builder to append to.</param>
        /// <param name="cfg">The generator configuration.</param>
        internal static void GenerateAssertions(StringBuilder sb, DotHttpConfig cfg)
        {
            // Generate call to DotHttpAssertions.AssertValidResponseAsync with config values
            sb.AppendLine($"            await DotHttpAssertions.AssertValidResponseAsync(response,");
            sb.AppendLine($"                checkStatusCode: {cfg.CheckStatusCode.ToString().ToLowerInvariant()},");
            sb.AppendLine($"                checkContentType: {cfg.CheckContentType.ToString().ToLowerInvariant()},");
            sb.AppendLine($"                checkBodyForErrors: {cfg.CheckBodyForErrors.ToString().ToLowerInvariant()},");
            sb.AppendLine($"                logResponseOnFailure: {cfg.LogResponseOnFailure.ToString().ToLowerInvariant()});");
        }

        /// <summary>
        /// Generates a complete test class from a parsed .http file.
        /// </summary>
        /// <param name="file">The parsed .http file.</param>
        /// <param name="cfg">The generator configuration.</param>
        /// <returns>The generated C# source code.</returns>
        internal static string GenerateTestClass(DotHttpFile file, DotHttpConfig cfg)
        {
            var sb = new StringBuilder();
            var className = SanitizeClassName(Path.GetFileNameWithoutExtension(file.FilePath));
            var isXUnit = string.Equals(cfg.TestFramework, "XUnit", StringComparison.OrdinalIgnoreCase);

            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using CloudNimble.Breakdance.DotHttp;");

            // Framework-specific usings
            if (isXUnit)
            {
                sb.AppendLine("using Xunit;");
            }
            else
            {
                sb.AppendLine("using Microsoft.VisualStudio.TestTools.UnitTesting;");
            }

            if (cfg.UseFluentAssertions)
            {
                sb.AppendLine("using FluentAssertions;");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {cfg.Namespace}");
            sb.AppendLine("{");

            // Class attribute based on framework
            if (!isXUnit)
            {
                sb.AppendLine("    [TestClass]");
            }

            sb.AppendLine($"    public partial class {className}Tests : DotHttpTestBase");
            sb.AppendLine("    {");

            // Generate file-level variables as static initialization
            if (file.Variables.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("        #region File Variables");
                sb.AppendLine();
                sb.AppendLine("        private void InitializeFileVariables()");
                sb.AppendLine("        {");
                foreach (var variable in file.Variables.OrderBy(v => v.Key))
                {
                    sb.AppendLine($"            SetVariable(\"{Escape(variable.Key)}\", \"{Escape(variable.Value)}\");");
                }
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        #endregion");
            }

            // Generate tests for each request
            sb.AppendLine();
            sb.AppendLine("        #region Tests");

            foreach (var request in file.Requests)
            {
                GenerateTestMethod(sb, request, cfg, isXUnit, file.Variables.Count > 0);
            }

            sb.AppendLine();
            sb.AppendLine("        #endregion");

            // Generate partial methods
            sb.AppendLine();
            sb.AppendLine("        #region Partial Methods");
            sb.AppendLine();

            foreach (var request in file.Requests)
            {
                var methodName = GetTestMethodName(request);
                sb.AppendLine($"        partial void On{methodName}Setup();");
                sb.AppendLine($"        partial void On{methodName}Assert(HttpResponseMessage response);");
            }

            sb.AppendLine();
            sb.AppendLine("        #endregion");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates a test method for a single HTTP request.
        /// </summary>
        /// <param name="sb">The string builder to append to.</param>
        /// <param name="request">The HTTP request to generate a test for.</param>
        /// <param name="cfg">The generator configuration.</param>
        /// <param name="isXUnit">Whether to generate XUnit-style tests.</param>
        /// <param name="hasFileVariables">Whether the file has variables to initialize.</param>
        internal static void GenerateTestMethod(StringBuilder sb, DotHttpRequest request, DotHttpConfig cfg, bool isXUnit, bool hasFileVariables)
        {
            var methodName = GetTestMethodName(request);

            sb.AppendLine();

            // Add comments from the request as test summary
            if (request.Comments.Count > 0)
            {
                sb.AppendLine("        /// <summary>");
                foreach (var comment in request.Comments)
                {
                    sb.AppendLine($"        /// {EscapeXml(comment)}");
                }
                sb.AppendLine("        /// </summary>");
            }

            // Test method attribute
            sb.AppendLine(isXUnit ? "        [Fact]" : "        [TestMethod]");
            sb.AppendLine($"        public async Task {methodName}()");
            sb.AppendLine("        {");

            // Initialize file variables if present
            if (hasFileVariables)
            {
                sb.AppendLine("            InitializeFileVariables();");
            }

            // Initialize request-level variables if present
            if (request.Variables.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("            // Request-level variable overrides");
                foreach (var variable in request.Variables.OrderBy(v => v.Key))
                {
                    sb.AppendLine($"            SetVariable(\"{Escape(variable.Key)}\", \"{Escape(variable.Value)}\");");
                }
            }

            // Call partial setup method
            sb.AppendLine($"            On{methodName}Setup();");
            sb.AppendLine();

            // Build the request
            sb.AppendLine($"            var request = new HttpRequestMessage(");
            sb.AppendLine($"                HttpMethod.{GetHttpMethodProperty(request.Method)},");
            sb.AppendLine($"                ResolveVariables(\"{Escape(request.Url)}\"));");

            // Add HTTP version if specified
            if (!string.IsNullOrEmpty(request.HttpVersion))
            {
                sb.AppendLine($"            request.Version = new Version(\"{GetHttpVersion(request.HttpVersion)}\");");
            }

            // Add headers
            if (request.Headers.Count > 0)
            {
                sb.AppendLine();
                foreach (var header in request.Headers.OrderBy(h => h.Key))
                {
                    sb.AppendLine($"            request.Headers.TryAddWithoutValidation(\"{Escape(header.Key)}\", ResolveVariables(\"{Escape(header.Value)}\"));");
                }
            }

            // Add body if present
            if (request.IsFileBody)
            {
                // File-based body
                sb.AppendLine();
                sb.AppendLine($"            var bodyFilePath = ResolveVariables(\"{Escape(request.BodyFilePath)}\");");
                sb.AppendLine($"            var bodyContent = await File.ReadAllBytesAsync(bodyFilePath);");

                var contentType = request.Headers.TryGetValue("Content-Type", out var ct) ? ct : "application/octet-stream";
                sb.AppendLine($"            request.Content = new ByteArrayContent(bodyContent);");
                sb.AppendLine($"            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(\"{Escape(contentType)}\");");
            }
            else if (!string.IsNullOrEmpty(request.Body))
            {
                sb.AppendLine();
                var contentType = request.Headers.TryGetValue("Content-Type", out var ct) ? ct : "application/json";
                sb.AppendLine($"            request.Content = new StringContent(");
                sb.AppendLine($"                ResolveVariables(@\"{EscapeVerbatim(request.Body)}\"),");
                sb.AppendLine($"                Encoding.UTF8,");
                sb.AppendLine($"                \"{Escape(contentType)}\");");
            }

            // Send request
            sb.AppendLine();
            sb.AppendLine("            var response = await HttpClient.SendAsync(request);");

            // Capture response if named (for chaining)
            if (!string.IsNullOrEmpty(request.Name))
            {
                sb.AppendLine($"            await CaptureResponseAsync(\"{Escape(request.Name)}\", response);");
            }

            // Generate assertions
            sb.AppendLine();
            GenerateAssertions(sb, cfg);

            // Call partial assert method
            sb.AppendLine();
            sb.AppendLine($"            On{methodName}Assert(response);");

            sb.AppendLine("        }");
        }

        /// <summary>
        /// Gets the configuration from analyzer config options.
        /// </summary>
        /// <param name="globalOptions">The global analyzer config options.</param>
        /// <example>
        /// <code>
        /// // Configuration can come from nested XML (preferred):
        /// // &lt;BreakdanceDotHttpConfig&gt;
        /// //   &lt;DotHttp&gt;
        /// //     &lt;TestFramework&gt;MSTest&lt;/TestFramework&gt;
        /// //   &lt;/DotHttp&gt;
        /// // &lt;/BreakdanceDotHttpConfig&gt;
        /// //
        /// // Or individual properties (for backwards compatibility):
        /// // &lt;BreakdanceDotHttp_TestFramework&gt;MSTest&lt;/BreakdanceDotHttp_TestFramework&gt;
        /// </code>
        /// </example>
        /// <returns>The parsed configuration.</returns>
        /// <remarks>
        /// Nested XML configuration is parsed first. Individual properties can override
        /// values from the nested XML for fine-grained control.
        /// </remarks>
        internal static DotHttpConfig GetConfiguration(AnalyzerConfigOptions globalOptions)
        {
            var config = new DotHttpConfig();

            // First, try to parse nested XML configuration (single parse, no intermediate files)
            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttpConfig", out var xmlConfig) &&
                !string.IsNullOrWhiteSpace(xmlConfig))
            {
                DotHttpConfig.ParseFromXml(xmlConfig, config);
            }

            // Individual properties can override nested XML values (backwards compatibility)
            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_TestFramework", out var framework) &&
                !string.IsNullOrWhiteSpace(framework))
            {
                config.TestFramework = framework;
            }

            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_Namespace", out var ns) &&
                !string.IsNullOrWhiteSpace(ns))
            {
                config.Namespace = ns;
            }

            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_BasePath", out var basePath) &&
                !string.IsNullOrWhiteSpace(basePath))
            {
                config.BasePath = basePath;
            }

            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_UseFluentAssertions", out var fluentAssertions) &&
                !string.IsNullOrWhiteSpace(fluentAssertions))
            {
                config.UseFluentAssertions = string.Equals(fluentAssertions, "true", StringComparison.OrdinalIgnoreCase);
            }

            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_CheckStatusCode", out var checkStatus) &&
                !string.IsNullOrWhiteSpace(checkStatus))
            {
                config.CheckStatusCode = !string.Equals(checkStatus, "false", StringComparison.OrdinalIgnoreCase);
            }

            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_CheckContentType", out var checkContentType) &&
                !string.IsNullOrWhiteSpace(checkContentType))
            {
                config.CheckContentType = !string.Equals(checkContentType, "false", StringComparison.OrdinalIgnoreCase);
            }

            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_CheckBodyForErrors", out var checkBody) &&
                !string.IsNullOrWhiteSpace(checkBody))
            {
                config.CheckBodyForErrors = !string.Equals(checkBody, "false", StringComparison.OrdinalIgnoreCase);
            }

            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_LogResponseOnFailure", out var logResponse) &&
                !string.IsNullOrWhiteSpace(logResponse))
            {
                config.LogResponseOnFailure = !string.Equals(logResponse, "false", StringComparison.OrdinalIgnoreCase);
            }

            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_Environment", out var env) &&
                !string.IsNullOrWhiteSpace(env))
            {
                config.Environment = env;
            }

            if (globalOptions.TryGetValue("build_property.BreakdanceDotHttp_HttpClientType", out var httpClientType) &&
                !string.IsNullOrWhiteSpace(httpClientType))
            {
                config.HttpClientType = httpClientType;
            }

            // Fall back to RootNamespace if Namespace not set
            if (string.IsNullOrWhiteSpace(config.Namespace) &&
                globalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace) &&
                !string.IsNullOrWhiteSpace(rootNamespace))
            {
                config.Namespace = rootNamespace;
            }

            // Default namespace fallback
            if (string.IsNullOrWhiteSpace(config.Namespace))
            {
                config.Namespace = "GeneratedTests";
            }

            return config;
        }

        /// <summary>
        /// Gets the HttpMethod property name for a given HTTP method.
        /// </summary>
        /// <param name="method">The HTTP method (e.g., "GET", "POST").</param>
        /// <returns>The HttpMethod property name (e.g., "Get", "Post").</returns>
        internal static string GetHttpMethodProperty(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => "Get",
                "POST" => "Post",
                "PUT" => "Put",
                "DELETE" => "Delete",
                "PATCH" => "Patch",
                "HEAD" => "Head",
                "OPTIONS" => "Options",
                "TRACE" => "Trace",
                "CONNECT" => "new HttpMethod(\"CONNECT\")",
                _ => "Get"
            };
        }

        /// <summary>
        /// Parses an HTTP version string to a version number.
        /// Uses span-based parsing to avoid regex overhead.
        /// </summary>
        /// <param name="version">The HTTP version string (e.g., "HTTP/1.1", "HTTP/2").</param>
        /// <returns>The version number as a string (e.g., "1.1", "2.0").</returns>
        internal static string GetHttpVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return "1.1";
            }

            // Parse "HTTP/1.1" or "HTTP/2" or "HTTP/3" format using span
            var span = version.AsSpan();
            var httpPrefix = "HTTP/".AsSpan();

            var prefixIndex = span.IndexOf(httpPrefix, StringComparison.OrdinalIgnoreCase);
            if (prefixIndex < 0)
            {
                return "1.1";
            }

            var versionStart = prefixIndex + httpPrefix.Length;
            if (versionStart >= span.Length)
            {
                return "1.1";
            }

            // Find the end of the version number (digits and dots only)
            var versionSpan = span[versionStart..];
            var versionEnd = 0;
            foreach (var c in versionSpan)
            {
                if (char.IsDigit(c) || c == '.')
                {
                    versionEnd++;
                }
                else
                {
                    break;
                }
            }

            if (versionEnd == 0)
            {
                return "1.1";
            }

            var v = versionSpan[..versionEnd];

            // Ensure we have major.minor format
            if (v.IndexOf('.') < 0)
            {
                return $"{v.ToString()}.0";
            }

            return v.ToString();
        }

        /// <summary>
        /// Gets the test method name for a request.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>A valid C# method name.</returns>
        /// <remarks>
        /// Name is determined in this priority order:
        /// 1. The @name directive value if specified
        /// 2. The text after ### separator (dehumanized) with _{HttpMethod} suffix
        /// 3. The URL path (dehumanized) with _{HttpMethod} suffix
        /// </remarks>
        internal static string GetTestMethodName(DotHttpRequest request)
        {
            // Priority 1: Use @name if specified
            if (!string.IsNullOrEmpty(request.Name))
            {
                return SanitizeMethodName(request.Name);
            }

            // Priority 2: Use separator title if present
            if (!string.IsNullOrEmpty(request.SeparatorTitle))
            {
                var titleDehumanized = CleanupAfterDehumanize(request.SeparatorTitle.Dehumanize());
                return $"{titleDehumanized}_{request.Method}";
            }

            // Priority 3: Use URL path
            var urlPart = request.Url;

            // Remove variable references for naming
            urlPart = VariableReferenceRegex.Replace(urlPart, "");

            // Remove protocol
            urlPart = ProtocolPrefixRegex.Replace(urlPart, "");

            // Remove host/domain portion (everything before first /)
            var pathStart = urlPart.IndexOf('/');
            if (pathStart >= 0)
            {
                urlPart = urlPart[pathStart..];
            }

            // Remove query string
            var queryStart = urlPart.IndexOf('?');
            if (queryStart >= 0)
            {
                urlPart = urlPart[..queryStart];
            }

            var dehumanized = CleanupAfterDehumanize(urlPart.Dehumanize());

            // If cleanup resulted in empty string, use a default
            if (string.IsNullOrEmpty(dehumanized))
            {
                dehumanized = "Request";
            }

            return $"{dehumanized}_{request.Method}";
        }

        /// <summary>
        /// Cleans up a dehumanized string by removing characters that are invalid for C# identifiers.
        /// This runs AFTER Dehumanize, requiring fewer character checks than pre-sanitization.
        /// </summary>
        /// <param name="input">The dehumanized string.</param>
        /// <returns>A string with only valid C# identifier characters.</returns>
        internal static string CleanupAfterDehumanize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Remove any characters that aren't valid in C# identifiers (letters, digits, underscores)
            var result = InvalidIdentifierCharRegex.Replace(input, "");

            // Ensure result starts with a letter (valid C# identifier requirement)
            if (!string.IsNullOrEmpty(result) && !char.IsLetter(result[0]))
            {
                result = "N" + result;
            }

            return result;
        }

        /// <summary>
        /// Reports parse diagnostics from the parsed file.
        /// </summary>
        /// <param name="context">The source production context.</param>
        /// <param name="file">The parsed file with potential diagnostics.</param>
        internal static void ReportDiagnostics(SourceProductionContext context, DotHttpFile file)
        {
            foreach (var diagnostic in file.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Sanitizes a string to be a valid C# class name.
        /// </summary>
        /// <param name="name">The name to sanitize.</param>
        /// <returns>A valid C# class name.</returns>
        internal static string SanitizeClassName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "HttpTests";
            }

            // Remove invalid characters and convert to PascalCase
            var result = InvalidIdentifierCharRegex.Replace(name, "_");

            // Ensure it starts with a letter
            if (!char.IsLetter(result[0]))
            {
                result = "Http" + result;
            }

            return ToPascalCase(result);
        }

        /// <summary>
        /// Sanitizes a string to be a valid C# method name.
        /// </summary>
        /// <param name="name">The name to sanitize.</param>
        /// <returns>A valid C# method name.</returns>
        internal static string SanitizeMethodName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Test";
            }

            // Remove invalid characters
            var result = InvalidIdentifierCharRegex.Replace(name, "_");

            // Remove consecutive underscores
            result = ConsecutiveUnderscoreRegex.Replace(result, "_");

            // Remove leading/trailing underscores
            result = result.Trim('_');

            // Ensure it starts with a letter
            if (string.IsNullOrEmpty(result) || !char.IsLetter(result[0]))
            {
                result = "Test" + result;
            }

            return ToPascalCase(result);
        }

        /// <summary>
        /// Converts a string to PascalCase.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The PascalCase string.</returns>
        internal static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var span = input.AsSpan();
            var result = new StringBuilder(input.Length);
            var capitalizeNext = true;

            foreach (var c in span)
            {
                if (c is '_' or '-' or ' ')
                {
                    // Skip delimiter, next character should be capitalized
                    capitalizeNext = true;
                }
                else if (capitalizeNext)
                {
                    result.Append(char.ToUpperInvariant(c));
                    capitalizeNext = false;
                }
                else
                {
                    result.Append(char.ToLowerInvariant(c));
                }
            }

            return result.ToString();
        }

        #endregion

    }

}
