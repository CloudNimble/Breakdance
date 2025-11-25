namespace CloudNimble.Breakdance.Azurite
{

    /// <summary>
    /// Defines how the Azurite emulator instance is managed during test execution.
    /// </summary>
    public enum EmulatorMode
    {
        /// <summary>
        /// One shared Azurite instance per test assembly.
        /// The instance starts once and is reused by all tests in the assembly.
        /// This is the default and most efficient mode.
        /// </summary>
        SharedPerAssembly = 0,

        /// <summary>
        /// One Azurite instance per test class.
        /// A new instance starts for each test class and is shared by all tests in that class.
        /// </summary>
        PerClass = 1,

        /// <summary>
        /// One Azurite instance per test method.
        /// A new instance starts and stops for each individual test.
        /// This provides maximum isolation but is the slowest option.
        /// </summary>
        PerTest = 2,

        /// <summary>
        /// Manual lifecycle management.
        /// The test author is responsible for starting and stopping Azurite instances.
        /// </summary>
        Manual = 3
    }

}
