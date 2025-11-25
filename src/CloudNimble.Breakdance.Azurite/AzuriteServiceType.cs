using System;

namespace CloudNimble.Breakdance.Azurite
{

    /// <summary>
    /// Specifies which Azurite services to start.
    /// </summary>
    [Flags]
    public enum AzuriteServiceType
    {
        /// <summary>
        /// No services (invalid).
        /// </summary>
        None = 0,

        /// <summary>
        /// Azure Blob Storage emulator.
        /// </summary>
        Blob = 1,

        /// <summary>
        /// Azure Queue Storage emulator.
        /// </summary>
        Queue = 2,

        /// <summary>
        /// Azure Table Storage emulator.
        /// </summary>
        Table = 4,

        /// <summary>
        /// All services (Blob, Queue, and Table).
        /// </summary>
        All = Blob | Queue | Table
    }

}
