using Bunit.TestDoubles;
using CloudNimble.Breakdance.Blazor;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Breakdance.Tests.Blazor
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
            RegisterServices = services => {
                services.AddScoped<TestableNavigationManager>();
            };
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
            RegisterServices.Should().NotBeNull();
            BUnitTestContext.Services.Should().HaveCount(14);
            GetService<NavigationManager>().Should().NotBeNull().And.BeOfType(typeof(FakeNavigationManager));
            GetServices<NavigationManager>().Should().HaveCount(1);
            GetService<TestableNavigationManager>().Should().NotBeNull();
        }

    }

}
