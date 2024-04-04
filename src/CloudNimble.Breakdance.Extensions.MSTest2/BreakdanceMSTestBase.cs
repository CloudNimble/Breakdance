using CloudNimble.Breakdance.Assemblies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Extensions.MSTest2
{

    /// <summary>
    /// A base class for testing that provides an MSTest <see cref="TestContext" />.
    /// </summary>
    public class BreakdanceMSTestBase : BreakdanceTestBase
    {

        /// <summary>
        /// The <see cref="TestContext" /> populated by MSTest during test execution.
        /// </summary>
        public TestContext TestContext { get; set; }

    }

}
