namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Represents the current state of the parser state machine.
    /// </summary>
    internal enum ParserState
    {

        /// <summary>
        /// Initial state, looking for variables, comments, or request lines.
        /// </summary>
        Start,

        /// <summary>
        /// Parsing request headers until an empty line is encountered.
        /// </summary>
        InHeaders,

        /// <summary>
        /// Parsing request body until end of file or request separator.
        /// </summary>
        InBody

    }

}
