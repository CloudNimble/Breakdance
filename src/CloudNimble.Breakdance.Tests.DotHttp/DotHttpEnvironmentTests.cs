using System.Collections.Generic;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="DotHttpEnvironment"/> class.
    /// </summary>
    [TestClass]
    public class DotHttpEnvironmentTests
    {

        #region Constructor Tests

        [TestMethod]
        public void DotHttpEnvironment_DefaultConstructor_InitializesEmptyCollections()
        {
            var environment = new DotHttpEnvironment();

            environment.Environments.Should().NotBeNull();
            environment.Environments.Should().BeEmpty();
            environment.Shared.Should().NotBeNull();
            environment.Shared.Should().BeEmpty();
        }

        #endregion

        #region Environments Property Tests

        [TestMethod]
        public void Environments_CanAddEnvironments()
        {
            var environment = new DotHttpEnvironment();

            environment.Environments["dev"] = new Dictionary<string, EnvironmentValue>
            {
                { "baseUrl", EnvironmentValue.FromString("https://localhost:5001") }
            };

            environment.Environments.Should().ContainKey("dev");
            environment.Environments["dev"].Should().ContainKey("baseUrl");
            environment.Environments["dev"]["baseUrl"].Value.Should().Be("https://localhost:5001");
        }

        [TestMethod]
        public void Environments_CanSetToNewDictionary()
        {
            var environment = new DotHttpEnvironment();
            var newEnvironments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
            {
                { "prod", new Dictionary<string, EnvironmentValue>() }
            };

            environment.Environments = newEnvironments;

            environment.Environments.Should().BeSameAs(newEnvironments);
        }

        #endregion

        #region Shared Property Tests

        [TestMethod]
        public void Shared_CanAddSharedVariables()
        {
            var environment = new DotHttpEnvironment();

            environment.Shared["apiVersion"] = EnvironmentValue.FromString("v2");

            environment.Shared.Should().ContainKey("apiVersion");
            environment.Shared["apiVersion"].Value.Should().Be("v2");
        }

        [TestMethod]
        public void Shared_CanSetToNewDictionary()
        {
            var environment = new DotHttpEnvironment();
            var newShared = new Dictionary<string, EnvironmentValue>
            {
                { "key", EnvironmentValue.FromString("value") }
            };

            environment.Shared = newShared;

            environment.Shared.Should().BeSameAs(newShared);
        }

        #endregion

    }

}
