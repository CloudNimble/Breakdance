using Bunit.TestDoubles;
using CloudNimble.Breakdance.Blazor;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.Blazor
{

    /// <summary>
    /// Tests the functionality of <see cref="BlazorBreakdanceTestBase"/> the way most end-users will use them.
    /// </summary>
    [TestClass]
    public class BlazorBreakdanceTestBaseTests : BlazorBreakdanceTestBase
    {

        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            TestSetup();
        }

        [TestCleanup]
        public void TearDown() => TestTearDown();

        #endregion

        /// <summary>
        /// Tests whether or not a <see cref="Bunit.TestContext"/> is created on setup. />
        /// </summary>
        [TestMethod]
        public void BlazorBreakdanceTestBase_Setup_CreatesTestContext_ConflictingServices()
        {
            BUnitTestContext.Should().NotBeNull();
            BUnitTestContext.Services.Should().HaveCount(26);
            GetService<NavigationManager>().Should().NotBeNull().And.BeOfType(typeof(BunitNavigationManager));
            GetServices<NavigationManager>().Should().HaveCount(1);
        }

    }
}
