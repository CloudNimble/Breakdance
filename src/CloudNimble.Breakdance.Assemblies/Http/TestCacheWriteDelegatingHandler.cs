using System;

namespace CloudNimble.Breakdance.Assemblies.Http
{

    /// <summary>
    /// Handler for capturing HTTP responses and writing them to files.
    /// </summary>
    /// <remarks>
    /// This class is deprecated. Use <see cref="ResponseSnapshotCaptureHandler"/> instead.
    /// </remarks>
    [Obsolete("Use ResponseSnapshotCaptureHandler instead. This class will be removed in a future major version.")]
    public class TestCacheWriteDelegatingHandler : ResponseSnapshotCaptureHandler
    {

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="TestCacheWriteDelegatingHandler"/> with the specified root folder path.
        /// </summary>
        /// <param name="responseFilesPath">Root folder path for storing static response files.</param>
        /// <remarks>
        /// This constructor is deprecated. Use <see cref="ResponseSnapshotCaptureHandler"/> instead.
        /// </remarks>
        public TestCacheWriteDelegatingHandler(string responseFilesPath) : base(responseFilesPath)
        {
        }

        #endregion

    }

}
