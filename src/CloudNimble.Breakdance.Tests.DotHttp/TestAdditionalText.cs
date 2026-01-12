using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Test implementation of <see cref="AdditionalText"/> for providing HTTP file content to source generators.
    /// </summary>
    /// <example>
    /// <code>
    /// var additionalText = new TestAdditionalText("api.http", "GET https://api.example.com/users");
    /// var sourceText = additionalText.GetText();
    /// </code>
    /// </example>
    /// <remarks>
    /// Used in source generator integration tests to simulate .http files being passed to the generator.
    /// </remarks>
    internal class TestAdditionalText : AdditionalText
    {

        #region Fields

        private readonly string _path;
        private readonly string _text;

        #endregion

        #region Properties

        /// <inheritdoc />
        public override string Path => _path;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAdditionalText"/> class.
        /// </summary>
        /// <param name="path">The file path for the additional text.</param>
        /// <param name="text">The text content of the file.</param>
        public TestAdditionalText(string path, string text)
        {
            _path = path;
            _text = text;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(_text);
        }

        #endregion

    }

}
