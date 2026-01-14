using System;
using System.Net.Http;

namespace CloudNimble.Breakdance.Assemblies.Http
{

    /// <summary>
    /// Base class for implementation of TestCache handlers for unit testing.
    /// </summary>
    /// <remarks>
    /// This class is deprecated. Use <see cref="ResponseSnapshotHandlerBase"/> instead.
    /// </remarks>
    [Obsolete("Use ResponseSnapshotHandlerBase instead. This class will be removed in a future major version.")]
    public class TestCacheDelegatingHandlerBase : ResponseSnapshotHandlerBase
    {

        #region Properties

        /// <summary>
        /// Gets the root folder path for reading/writing static response files.
        /// </summary>
        /// <remarks>
        /// This property is deprecated. Use <see cref="ResponseSnapshotHandlerBase.ResponseSnapshotsPath"/> instead.
        /// </remarks>
        [Obsolete("Use ResponseSnapshotsPath instead. This property will be removed in a future major version.")]
        public string ResponseFilesPath => ResponseSnapshotsPath;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="TestCacheDelegatingHandlerBase"/> with the specified root folder path.
        /// </summary>
        /// <param name="responseFilesPath">Root folder path for storing static response files.</param>
        /// <remarks>
        /// This constructor is deprecated. Use <see cref="ResponseSnapshotHandlerBase"/> instead.
        /// </remarks>
        public TestCacheDelegatingHandlerBase(string responseFilesPath) : base(responseFilesPath)
        {
        }

        #endregion

    }

}
