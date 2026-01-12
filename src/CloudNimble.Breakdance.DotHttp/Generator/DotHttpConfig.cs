using System;
using System.Xml.Linq;

namespace CloudNimble.Breakdance.DotHttp.Generator
{

    /// <summary>
    /// Configuration options for the DotHttp source generator.
    /// </summary>
    /// <example>
    /// <code>
    /// // Configuration can be provided as nested XML:
    /// // &lt;BreakdanceDotHttpConfig&gt;
    /// //   &lt;DotHttp&gt;
    /// //     &lt;TestFramework&gt;MSTest&lt;/TestFramework&gt;
    /// //     &lt;UseFluentAssertions&gt;true&lt;/UseFluentAssertions&gt;
    /// //     &lt;Assertions&gt;
    /// //       &lt;CheckStatusCode&gt;true&lt;/CheckStatusCode&gt;
    /// //     &lt;/Assertions&gt;
    /// //   &lt;/DotHttp&gt;
    /// // &lt;/BreakdanceDotHttpConfig&gt;
    /// //
    /// // Or as individual MSBuild properties:
    /// // &lt;BreakdanceDotHttp_TestFramework&gt;MSTest&lt;/BreakdanceDotHttp_TestFramework&gt;
    /// </code>
    /// </example>
    /// <remarks>
    /// Properties are populated from MSBuild build_property values by the source generator.
    /// </remarks>
    internal class DotHttpConfig
    {

        #region Properties

        /// <summary>
        /// Gets or sets the base path for .http files.
        /// </summary>
        public string BasePath { get; set; }

        /// <summary>
        /// Gets or sets whether to check for error patterns in response body.
        /// Default is true.
        /// </summary>
        public bool CheckBodyForErrors { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to check Content-Type headers.
        /// Default is true.
        /// </summary>
        public bool CheckContentType { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to check status codes in generated tests.
        /// Default is true.
        /// </summary>
        public bool CheckStatusCode { get; set; } = true;

        /// <summary>
        /// Gets or sets the environment name to use.
        /// </summary>
        public string Environment { get; set; } = "dev";

        /// <summary>
        /// Gets or sets the custom HttpClient type to use.
        /// </summary>
        public string HttpClientType { get; set; }

        /// <summary>
        /// Gets or sets whether to log response body on test failure.
        /// Default is true.
        /// </summary>
        public bool LogResponseOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets the namespace for generated test classes.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the test framework to generate tests for.
        /// Values: "MSTest" or "XUnit". Default is "MSTest".
        /// </summary>
        public string TestFramework { get; set; } = "MSTest";

        /// <summary>
        /// Gets or sets whether to use FluentAssertions for assertion methods.
        /// Default is true.
        /// </summary>
        public bool UseFluentAssertions { get; set; } = true;

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses configuration from a nested XML string.
        /// </summary>
        /// <param name="xml">The XML configuration string containing a DotHttp element.</param>
        /// <param name="config">The configuration object to populate. If null, a new instance is created.</param>
        /// <example>
        /// <code>
        /// var xml = @"
        ///   &lt;DotHttp&gt;
        ///     &lt;TestFramework&gt;XUnit&lt;/TestFramework&gt;
        ///     &lt;Assertions&gt;
        ///       &lt;CheckStatusCode&gt;false&lt;/CheckStatusCode&gt;
        ///     &lt;/Assertions&gt;
        ///   &lt;/DotHttp&gt;";
        /// var config = new DotHttpConfig();
        /// DotHttpConfig.ParseFromXml(xml, config);
        /// </code>
        /// </example>
        /// <returns>The populated configuration object.</returns>
        /// <remarks>
        /// This method parses the XML once and extracts all configuration values.
        /// Values not present in the XML retain their default values.
        /// </remarks>
        public static DotHttpConfig ParseFromXml(string xml, DotHttpConfig config = null)
        {
            config ??= new DotHttpConfig();

            if (string.IsNullOrWhiteSpace(xml))
            {
                return config;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse($"<root>{xml}</root>");
            }
            catch
            {
                return config;
            }

            var dotHttp = doc.Root?.Element("DotHttp");
            if (dotHttp is null)
            {
                return config;
            }

            // Parse top-level properties
            var testFramework = dotHttp.Element("TestFramework")?.Value;
            if (!string.IsNullOrWhiteSpace(testFramework))
            {
                config.TestFramework = testFramework;
            }

            var basePath = dotHttp.Element("BasePath")?.Value;
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                config.BasePath = basePath;
            }

            var ns = dotHttp.Element("Namespace")?.Value;
            if (!string.IsNullOrWhiteSpace(ns))
            {
                config.Namespace = ns;
            }

            var env = dotHttp.Element("Environment")?.Value;
            if (!string.IsNullOrWhiteSpace(env))
            {
                config.Environment = env;
            }

            var httpClientType = dotHttp.Element("HttpClientType")?.Value;
            if (!string.IsNullOrWhiteSpace(httpClientType))
            {
                config.HttpClientType = httpClientType;
            }

            var useFluentAssertions = dotHttp.Element("UseFluentAssertions")?.Value;
            if (!string.IsNullOrWhiteSpace(useFluentAssertions))
            {
                config.UseFluentAssertions = string.Equals(useFluentAssertions, "true", StringComparison.OrdinalIgnoreCase);
            }

            // Parse Assertions section
            var assertions = dotHttp.Element("Assertions");
            if (assertions is not null)
            {
                var checkStatusCode = assertions.Element("CheckStatusCode")?.Value;
                if (!string.IsNullOrWhiteSpace(checkStatusCode))
                {
                    config.CheckStatusCode = !string.Equals(checkStatusCode, "false", StringComparison.OrdinalIgnoreCase);
                }

                var checkContentType = assertions.Element("CheckContentType")?.Value;
                if (!string.IsNullOrWhiteSpace(checkContentType))
                {
                    config.CheckContentType = !string.Equals(checkContentType, "false", StringComparison.OrdinalIgnoreCase);
                }

                var checkBodyForErrors = assertions.Element("CheckBodyForErrors")?.Value;
                if (!string.IsNullOrWhiteSpace(checkBodyForErrors))
                {
                    config.CheckBodyForErrors = !string.Equals(checkBodyForErrors, "false", StringComparison.OrdinalIgnoreCase);
                }

                var logResponseOnFailure = assertions.Element("LogResponseOnFailure")?.Value;
                if (!string.IsNullOrWhiteSpace(logResponseOnFailure))
                {
                    config.LogResponseOnFailure = !string.Equals(logResponseOnFailure, "false", StringComparison.OrdinalIgnoreCase);
                }
            }

            return config;
        }

        #endregion

    }

}
