using CloudNimble.Breakdance.Assemblies;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Azurite
{

    /// <summary>
    /// Base class for tests that require an Azurite instance.
    /// By default, uses a shared instance per test assembly for optimal performance.
    /// </summary>
    public abstract class AzuriteTestBase : BreakdanceTestBase
    {

        #region Private Members

        private static AzuriteInstance _sharedInstance;
#if NET9_0_OR_GREATER
        private static readonly Lock _sharedLock = new();
#else
        private static readonly object _sharedLock = new();
#endif

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Azurite instance for this test.
        /// For SharedPerAssembly mode, returns the shared instance.
        /// For PerClass or PerTest modes, returns the instance specific to this test class/method.
        /// </summary>
        protected AzuriteInstance Azurite { get; private set; }

        /// <summary>
        /// Gets the emulator lifecycle mode. Override to customize.
        /// Defaults to <see cref="EmulatorMode.SharedPerAssembly"/>.
        /// </summary>
        protected virtual EmulatorMode Mode => EmulatorMode.SharedPerAssembly;

        /// <summary>
        /// Gets which Azurite services to start. Override in derived class to customize.
        /// Defaults to <see cref="AzuriteServiceType.All"/>.
        /// </summary>
        protected virtual AzuriteServiceType Services => AzuriteServiceType.All;

        /// <summary>
        /// Gets whether to run in silent mode (no access logs). Override in derived class to customize.
        /// Defaults to true.
        /// </summary>
        protected virtual bool SilentMode => true;

        /// <summary>
        /// Gets whether to use in-memory persistence (no disk storage). Override in derived class to customize.
        /// Defaults to true.
        /// </summary>
        protected virtual bool UseInMemoryPersistence => true;

        /// <summary>
        /// Gets the startup timeout in seconds. Override in derived class to customize.
        /// Defaults to 30 seconds.
        /// </summary>
        protected virtual int StartupTimeoutSeconds => 30;

        /// <summary>
        /// Gets the maximum memory limit in MB for in-memory storage. Override in derived class to customize.
        /// Null means unlimited. Defaults to null.
        /// </summary>
        protected virtual int? ExtentMemoryLimitMB => null;

        /// <summary>
        /// Gets the workspace location for disk persistence. Override in derived class to customize.
        /// Only used when <see cref="UseInMemoryPersistence"/> is false.
        /// Defaults to null.
        /// </summary>
        protected virtual string Location => null;

        /// <summary>
        /// Gets the Blob service endpoint URL.
        /// </summary>
        public string BlobEndpoint => Azurite?.BlobEndpoint;

        /// <summary>
        /// Gets the Queue service endpoint URL.
        /// </summary>
        public string QueueEndpoint => Azurite?.QueueEndpoint;

        /// <summary>
        /// Gets the Table service endpoint URL.
        /// </summary>
        public string TableEndpoint => Azurite?.TableEndpoint;

        /// <summary>
        /// Gets the Blob service port number, or null if not started.
        /// </summary>
        public int? BlobPort => Azurite?.BlobPort;

        /// <summary>
        /// Gets the Queue service port number, or null if not started.
        /// </summary>
        public int? QueuePort => Azurite?.QueuePort;

        /// <summary>
        /// Gets the Table service port number, or null if not started.
        /// </summary>
        public int? TablePort => Azurite?.TablePort;

        /// <summary>
        /// Gets a connection string for the Azurite Development Storage account.
        /// </summary>
        public string ConnectionString => Azurite?.GetConnectionString();

        #endregion

        #region Public Methods

        /// <summary>
        /// Method used by test assemblies to setup the environment asynchronously.
        /// Ensures Azurite instance is running if needed.
        /// </summary>
        public async override Task AssemblySetupAsync()
        {
            await base.AssemblySetupAsync();
            await EnsureAzuriteAsync();
        }

        /// <summary>
        /// Method used by test classes to setup the environment asynchronously.
        /// Ensures Azurite instance is running if needed.
        /// </summary>
        public async override Task ClassSetupAsync()
        {
            await base.ClassSetupAsync();
            await EnsureAzuriteAsync();
        }

        /// <summary>
        /// Method used by test methods to setup the environment asynchronously.
        /// Ensures Azurite instance is running if needed.
        /// </summary>
        public async override Task TestSetupAsync()
        {
            await base.TestSetupAsync();
            await EnsureAzuriteAsync();
        }

        /// <summary>
        /// Method used by test methods to clean up the environment asynchronously.
        /// For PerTest mode, this stops the test-specific Azurite instance.
        /// </summary>
        public async override Task TestTearDownAsync()
        {
            if (Mode == EmulatorMode.PerTest)
            {
                await StopAzuriteAsync();
            }
            await base.TestTearDownAsync();
        }

        /// <summary>
        /// Method used by test classes to clean up the environment asynchronously.
        /// For PerClass mode, this stops the class-specific Azurite instance.
        /// </summary>
        public async override Task ClassTearDownAsync()
        {
            if (Mode == EmulatorMode.PerClass)
            {
                await StopAzuriteAsync();
            }
            await base.ClassTearDownAsync();
        }

        /// <summary>
        /// Method used by test assemblies to clean up the environment asynchronously.
        /// For SharedPerAssembly mode, this stops the shared Azurite instance.
        /// </summary>
        public async override Task AssemblyTearDownAsync()
        {
            if (Mode == EmulatorMode.SharedPerAssembly)
            {
                await StopSharedAzuriteAsync();
            }
            await base.AssemblyTearDownAsync();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Ensures an Azurite instance is running based on the configured <see cref="Mode"/>.
        /// This method is idempotent and thread-safe.
        /// </summary>
        protected void EnsureAzurite()
        {
            EnsureAzuriteAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Ensures an Azurite instance is running based on the configured <see cref="Mode"/>.
        /// This method is idempotent and thread-safe.
        /// </summary>
        protected async Task EnsureAzuriteAsync()
        {
            switch (Mode)
            {
                case EmulatorMode.SharedPerAssembly:
                    await EnsureSharedAzuriteAsync();
                    break;

                case EmulatorMode.PerClass:
                case EmulatorMode.PerTest:
                    await EnsureInstanceAzuriteAsync();
                    break;

                case EmulatorMode.Manual:
                    // Do nothing - test author manages lifecycle
                    break;
            }
        }

        /// <summary>
        /// Ensures the shared Azurite instance is running (for SharedPerAssembly mode).
        /// </summary>
        private async Task EnsureSharedAzuriteAsync()
        {
            // Quick check without lock
            if (_sharedInstance?.IsRunning == true)
            {
                Azurite = _sharedInstance;
                return;
            }

            lock (_sharedLock)
            {
                // Double-check inside lock
                if (_sharedInstance?.IsRunning == true)
                {
                    Azurite = _sharedInstance;
                    return;
                }

                _sharedInstance = CreateAndStartInstanceAsync().GetAwaiter().GetResult();
                Azurite = _sharedInstance;
            }
        }

        /// <summary>
        /// Ensures an instance-specific Azurite is running (for PerClass or PerTest modes).
        /// </summary>
        private async Task EnsureInstanceAzuriteAsync()
        {
            if (Azurite?.IsRunning == true)
                return;

            Azurite = await CreateAndStartInstanceAsync();
        }

        /// <summary>
        /// Creates and starts an Azurite instance with retry logic for port conflicts.
        /// Uses random port allocation to avoid conflicts in parallel test execution.
        /// </summary>
        /// <returns>A running AzuriteInstance.</returns>
        private async Task<AzuriteInstance> CreateAndStartInstanceAsync()
        {
            const int maxRetries = 20;
            var random = new Random();

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var config = CreateConfiguration();

                    // Use random port allocation to avoid conflicts in parallel execution
                    // Start from a random port in the 20000-30000 range for better isolation
                    var currentPort = attempt == 0 
                        ? random.Next(20000, 25000)  // First attempt: random port
                        : random.Next(20000, 30000);  // Subsequent attempts: also random

                    // Only set ports for services that are actually requested
                    if (Services.HasFlag(AzuriteServiceType.Blob))
                        config.BlobPort = currentPort;
                    if (Services.HasFlag(AzuriteServiceType.Queue))
                        config.QueuePort = currentPort + 1;
                    if (Services.HasFlag(AzuriteServiceType.Table))
                        config.TablePort = currentPort + 2;

                    // Log attempt for debugging
                    System.Diagnostics.Debug.WriteLine($"[AzuriteTestBase] Attempt {attempt + 1}/{maxRetries}: Starting Azurite with ports " +
                        $"Blob={config.BlobPort}, Queue={config.QueuePort}, Table={config.TablePort}");

                    var instance = new AzuriteInstance(config);
                    await instance.StartAsync();
                    
                    // Log success
                    System.Diagnostics.Debug.WriteLine($"[AzuriteTestBase] Successfully started Azurite with ports " +
                        $"Blob={instance.BlobPort}, Queue={instance.QueuePort}, Table={instance.TablePort}");
                    
                    return instance;
                }
                catch (InvalidOperationException ex) when (
                    ex.Message.Contains("EADDRINUSE") ||
                    ex.Message.Contains("address already in use") ||
                    ex.Message.Contains("Port conflict") ||
                    ex.Message.Contains("port is already in use"))
                {
                    // Log retry
                    System.Diagnostics.Debug.WriteLine($"[AzuriteTestBase] Port conflict on attempt {attempt + 1}: {ex.Message}");
                    
                    // Port conflict - retry with different random port
                    if (attempt == maxRetries - 1)
                    {
                        throw new InvalidOperationException(
                            $"Failed to start Azurite after {maxRetries} attempts. All port ranges tested were in use.\n" +
                            $"Last error: {ex.Message}", ex);
                    }

                    // Brief delay before retry
                    await Task.Delay(50);
                }
                // All other exceptions propagate immediately (no retry)
            }

            throw new InvalidOperationException("Failed to start Azurite - maximum retries exceeded.");
        }

        /// <summary>
        /// Stops the current Azurite instance asynchronously.
        /// </summary>
        protected async Task StopAzuriteAsync()
        {
            if (Azurite != null)
            {
                await Azurite.StopAsync();
                Azurite.Dispose();
                Azurite = null;
            }
        }

        /// <summary>
        /// Stops the shared Azurite instance asynchronously (for SharedPerAssembly mode).
        /// </summary>
        protected async Task StopSharedAzuriteAsync()
        {
            lock (_sharedLock)
            {
                if (_sharedInstance != null)
                {
                    _sharedInstance.StopAsync().GetAwaiter().GetResult();
                    _sharedInstance.Dispose();
                    _sharedInstance = null;
                    Azurite = null;
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Creates the Azurite configuration based on the test's property overrides.
        /// </summary>
        /// <returns>A configured AzuriteConfiguration instance.</returns>
        protected virtual AzuriteConfiguration CreateConfiguration()
        {
            return new AzuriteConfiguration
            {
                Services = Services,
                InMemoryPersistence = UseInMemoryPersistence,
                Silent = SilentMode,
                StartupTimeoutSeconds = StartupTimeoutSeconds,
                ExtentMemoryLimitMB = ExtentMemoryLimitMB,
                Location = Location
            };
        }

        /// <summary>
        /// Disposes the test base and stops Azurite if necessary.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Only dispose instance-specific Azurite, not the shared one
                if (Mode != EmulatorMode.SharedPerAssembly && Azurite != null)
                {
                    StopAzuriteAsync().GetAwaiter().GetResult();
                }
            }

            base.Dispose(disposing);
        }

        #endregion

    }

}
