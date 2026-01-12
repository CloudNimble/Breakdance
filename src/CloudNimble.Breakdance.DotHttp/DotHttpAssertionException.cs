using System;

namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Exception thrown when a DotHttp assertion fails.
    /// </summary>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     await DotHttpAssertions.AssertValidResponseAsync(response);
    /// }
    /// catch (DotHttpAssertionException ex)
    /// {
    ///     Console.WriteLine($"Assertion failed: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// This exception is thrown by <see cref="DotHttpAssertions"/> methods when
    /// HTTP response validation fails.
    /// </remarks>
    public class DotHttpAssertionException : Exception
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DotHttpAssertionException"/> class.
        /// </summary>
        public DotHttpAssertionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotHttpAssertionException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DotHttpAssertionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotHttpAssertionException"/> class
        /// with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DotHttpAssertionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion

    }

}
