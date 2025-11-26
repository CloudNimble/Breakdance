using CloudNimble.Breakdance.Assemblies;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Azurite
{

    /// <summary>
    /// Base class for tests that require an Azurite instance.
    /// Each derived class must declare its own static <see cref="AzuriteInstance"/> field
    /// and override the <see cref="Azurite"/> property to return it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MSTest requires [ClassInitialize] and [ClassCleanup] to be static methods.
    /// To avoid cross-class pollution (static fields on a base class are shared by ALL derived classes),
    /// each test class must own its own static instance.
    /// </para>
    /// <example>
    /// <code>
    /// [TestClass]
    /// public class MyTests : AzuriteTestBase
    /// {
    ///     private static AzuriteInstance _azurite;
    ///
    ///     protected override AzuriteInstance Azurite => _azurite;
    ///
    ///     [ClassInitialize]
    ///     public static async Task ClassInit(TestContext ctx)
    ///     {
    ///         _azurite = await CreateAndStartInstanceAsync(new AzuriteConfiguration
    ///         {
    ///             Services = AzuriteServiceType.All,
    ///             InMemoryPersistence = true,
    ///             Silent = true
    ///         });
    ///     }
    ///
    ///     [ClassCleanup]
    ///     public static async Task ClassCleanup()
    ///     {
    ///         if (_azurite != null)
    ///         {
    ///             await _azurite.DisposeAsync();
    ///             _azurite = null;
    ///         }
    ///     }
    ///
    ///     [TestMethod]
    ///     public void MyTest()
    ///     {
    ///         Assert.IsNotNull(BlobEndpoint);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public abstract class AzuriteBreakdanceTestBase : BreakdanceTestBase
    {

        #region Properties

        /// <summary>
        /// Gets the Azurite instance for this test class.
        /// Each derived class must override this to return its own static instance.
        /// </summary>
        protected abstract AzuriteInstance Azurite { get; }

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

        #region Static Factory Methods

        /// <summary>
        /// Creates and starts an Azurite instance with the specified configuration.
        /// Call this from your [ClassInitialize] or [AssemblyInitialize] method.
        /// </summary>
        /// <param name="config">The configuration for the Azurite instance. If null, uses defaults.</param>
        /// <returns>A running <see cref="AzuriteInstance"/>.</returns>
        protected static async Task<AzuriteInstance> CreateAndStartInstanceAsync(AzuriteConfiguration config = null)
        {
            config ??= new AzuriteConfiguration();

            // Auto-populate instance name from caller's class name if not set
            if (string.IsNullOrWhiteSpace(config.InstanceName))
            {
                var callerType = new StackFrame(1).GetMethod()?.DeclaringType;
                config.InstanceName = callerType?.Name ?? "Unknown";
            }

            var instance = new AzuriteInstance(config);
            await instance.StartAsync();
            return instance;
        }

        /// <summary>
        /// Stops and disposes an Azurite instance.
        /// Call this from your [ClassCleanup] or [AssemblyCleanup] method.
        /// </summary>
        /// <param name="instance">The instance to stop and dispose.</param>
        protected static async Task StopAndDisposeAsync(AzuriteInstance instance)
        {
            if (instance is not null)
            {
                await instance.DisposeAsync();
            }
        }

        #endregion

    }

}
