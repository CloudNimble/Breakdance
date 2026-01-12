using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Test implementation of <see cref="AnalyzerConfigOptionsProvider"/> for source generator tests.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["build_property.BreakdanceDotHttp_TestFramework"] = "MSTest"
    /// };
    /// var provider = new TestAnalyzerConfigOptionsProvider(options);
    /// </code>
    /// </example>
    /// <remarks>
    /// Provides build properties and analyzer options to the source generator during testing.
    /// </remarks>
    internal class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {

        #region Fields

        private readonly TestGlobalAnalyzerConfigOptions _globalOptions;

        #endregion

        #region Properties

        /// <inheritdoc />
        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAnalyzerConfigOptionsProvider"/> class.
        /// </summary>
        /// <param name="options">A dictionary of option key-value pairs.</param>
        public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> options)
        {
            _globalOptions = new TestGlobalAnalyzerConfigOptions(options);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _globalOptions;

        /// <inheritdoc />
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _globalOptions;

        #endregion

    }

}
