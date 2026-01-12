using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Test implementation of <see cref="AnalyzerConfigOptions"/> for global options in source generator tests.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["build_property.RootNamespace"] = "MyProject.Tests"
    /// };
    /// var configOptions = new TestGlobalAnalyzerConfigOptions(options);
    /// configOptions.TryGetValue("build_property.RootNamespace", out var value);
    /// </code>
    /// </example>
    /// <remarks>
    /// Wraps a dictionary to provide analyzer config options during testing.
    /// </remarks>
    internal class TestGlobalAnalyzerConfigOptions : AnalyzerConfigOptions
    {

        #region Fields

        private readonly Dictionary<string, string> _options;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestGlobalAnalyzerConfigOptions"/> class.
        /// </summary>
        /// <param name="options">A dictionary of option key-value pairs.</param>
        public TestGlobalAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            _options = options;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value);
        }

        #endregion

    }

}
