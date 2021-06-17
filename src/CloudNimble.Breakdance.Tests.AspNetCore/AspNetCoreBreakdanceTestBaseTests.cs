using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Breakdance.Tests.AspNetCore
{

    /// <summary>
    /// Tests the functionality of <see cref="AspNetCoreBreakdanceTestBase"/> the way most end-users will use them.
    /// </summary>
    [TestClass]
    public class AspNetCoreBreakdanceTestBaseTests : AspNetCoreBreakdanceTestBase
    {
        #region Test Lifecycle

        [TestInitialize]
        public void Setup()
        {
            RegisterServices = services => {
                services.AddScoped<DummyService>();
            };
            TestSetup();
        }

        [TestCleanup]
        public void TearDown() => TestTearDown();

        #endregion

        /// <summary>
        /// Tests that DI works property when configuring the <see cref="TestServer"/>.
        /// </summary>
        [TestMethod]
        public void BlazorBreakdanceTestBase_Setup_CreatesTestServer_WithExpectedServices()
        {
            TestServer.Should().NotBeNull();
            RegisterServices.Should().NotBeNull();
            TestServer.Services.GetAllServiceDescriptors().Should().HaveCount(28);
            GetService<IConfiguration>().Should().NotBeNull();
            GetService<DummyService>().Should().NotBeNull();
        }

    }

    public class DummyService
    {
    }
}
