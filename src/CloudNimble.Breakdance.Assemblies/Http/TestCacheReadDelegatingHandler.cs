using System;

namespace CloudNimble.Breakdance.Assemblies.Http
{

    /// <summary>
    /// Handler for returning HTTP responses from cached files.
    /// </summary>
    /// <remarks>
    /// This class is deprecated. Use <see cref="ResponseSnapshotReplayHandler"/> instead.
    /// </remarks>
    [Obsolete("Use ResponseSnapshotReplayHandler instead. This class will be removed in a future major version.")]
    public class TestCacheReadDelegatingHandler : ResponseSnapshotReplayHandler
    {

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="TestCacheReadDelegatingHandler"/> with the specified root folder path.
        /// </summary>
        /// <param name="responseFilesPath">Root folder path for storing static response files.</param>
        /// <remarks>
        /// This constructor is deprecated. Use <see cref="ResponseSnapshotReplayHandler"/> instead.
        /// </remarks>
        public TestCacheReadDelegatingHandler(string responseFilesPath) : base(responseFilesPath)
        {
        }

        #endregion

    }

}
