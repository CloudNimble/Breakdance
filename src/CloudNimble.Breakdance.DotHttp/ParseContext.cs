using CloudNimble.Breakdance.DotHttp.Models;

namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Context for parsing a .http file, tracking current position and state.
    /// </summary>
    internal sealed class ParseContext
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseContext"/> class.
        /// </summary>
        /// <param name="file">The file being built.</param>
        /// <param name="lines">The lines to parse.</param>
        public ParseContext(DotHttpFile file, string[] lines)
        {
            File = file;
            Lines = lines;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the file being built.
        /// </summary>
        public DotHttpFile File { get; }

        /// <summary>
        /// Gets or sets the current line index (0-based).
        /// </summary>
        public int LineIndex { get; set; }

        /// <summary>
        /// Gets the current line number (1-based) for error reporting.
        /// </summary>
        public int LineNumber => LineIndex + 1;

        /// <summary>
        /// Gets the lines being parsed.
        /// </summary>
        public string[] Lines { get; }

        #endregion

    }

}
