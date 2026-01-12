namespace CloudNimble.Breakdance.Azurite
{

    /// <summary>
    /// Configuration options for an Azurite instance.
    /// </summary>
    public class AzuriteConfiguration
    {

        #region Properties

        /// <summary>
        /// Gets or sets which services to start. Defaults to <see cref="AzuriteServiceType.All"/>.
        /// </summary>
        public AzuriteServiceType Services { get; set; } = AzuriteServiceType.All;

        /// <summary>
        /// Gets or sets whether to use in-memory persistence (no disk storage). Defaults to true.
        /// </summary>
        public bool InMemoryPersistence { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to run in silent mode (no access logs). Defaults to true.
        /// </summary>
        public bool Silent { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum memory limit in MB for in-memory storage.
        /// Null means unlimited. Only applies when <see cref="InMemoryPersistence"/> is true.
        /// </summary>
        public int? ExtentMemoryLimitMB { get; set; }

        /// <summary>
        /// Gets or sets whether to skip API version checking. Defaults to true.
        /// </summary>
        public bool SkipApiVersionCheck { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to disable telemetry. Defaults to true.
        /// </summary>
        public bool DisableTelemetry { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable loose mode (ignore unsupported headers/parameters). Defaults to false.
        /// </summary>
        public bool LooseMode { get; set; } = false;

        /// <summary>
        /// Gets or sets the optional blob service port.
        /// If null, Azurite will use its default port and we'll parse the actual port from output.
        /// </summary>
        public int? BlobPort { get; set; }

        /// <summary>
        /// Gets or sets the optional queue service port.
        /// If null, Azurite will use its default port and we'll parse the actual port from output.
        /// </summary>
        public int? QueuePort { get; set; }

        /// <summary>
        /// Gets or sets the optional table service port.
        /// If null, Azurite will use its default port and we'll parse the actual port from output.
        /// </summary>
        public int? TablePort { get; set; }

        /// <summary>
        /// Gets or sets the workspace location for disk persistence.
        /// Only used when <see cref="InMemoryPersistence"/> is false.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the debug log file path. Null means no debug logging.
        /// </summary>
        public string DebugLogPath { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds to wait for Azurite to start. Defaults to 30 seconds.
        /// </summary>
        public int StartupTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether to automatically assign random ports when ports are not specified.
        /// When true and ports are null, random ports in 20000-30000 range will be assigned.
        /// Defaults to true.
        /// </summary>
        public bool AutoAssignPorts { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts when port conflicts occur.
        /// Only applies when <see cref="AutoAssignPorts"/> is true. Defaults to 20.
        /// </summary>
        public int MaxRetries { get; set; } = 20;

        /// <summary>
        /// Gets or sets a name to identify this Azurite instance (e.g., test class name).
        /// Used for process identification and debugging. The full window title will be
        /// "Breakdance.Azurite - {InstanceName}". If not set, defaults to "Unknown".
        /// </summary>
        public string InstanceName { get; set; }

        #endregion

    }

}
