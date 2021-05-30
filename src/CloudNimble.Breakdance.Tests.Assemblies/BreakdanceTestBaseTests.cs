using CloudNimble.Breakdance.Assemblies;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace CloudNimble.Breakdance.Tests.Assemblies
{

    [TestClass]
    public class BreakdanceTestBaseTests
    {

        [TestMethod]
        public void BreakdanceTestBase_Constructor_HasHostBuilder()
        {
            var testBase = new TestBase();
            testBase.Should().NotBeNull();
            testBase.TestHostBuilder.Should().NotBeNull();
            testBase.TestHost.Should().BeNull();
        }

        [TestMethod]
        public void BreakdanceTestBase_AssemblySetup_CreatesTestHost()
        {
            var testBase = new TestBase();
            testBase.AssemblySetup();
            testBase.TestHost.Should().NotBeNull();
        }

        [TestMethod]
        public void BreakdanceTestBase_AssemblySetup_HostIsPopulated()
        {
            var testBase = new TestBase();
            testBase.AssemblySetup();
            testBase.TestHost.Services.Should().NotBeNull();
            var configuration = testBase.TestHost.Services.GetService<IConfiguration>();
            configuration.Should().NotBeNull();
            (configuration as ConfigurationRoot).Providers.Should().HaveCount(4);
            var environment = testBase.TestHost.Services.GetService<IHostEnvironment>();
            environment.Should().NotBeNull();
        }

        [TestMethod]
        public void BreakdanceTestBase_AssemblySetup_EnvironmentFromHostBuilder()
        {
            var testBase = new TestBase();
            testBase.TestHostBuilder.UseEnvironment("Development");
            testBase.AssemblySetup();
            testBase.TestHost.Services.Should().NotBeNull();
            var environment = testBase.TestHost.Services.GetService<IHostEnvironment>();
            environment.Should().NotBeNull();
            environment.EnvironmentName.Should().Be("Development");
        }

        [TestMethod]
        public void BreakdanceTestBase_AssemblySetup_EnvironmentFromRunSettings()
        {
            var testBase = new TestBase();
            testBase.AssemblySetup();
            testBase.TestHost.Services.Should().NotBeNull();
            var environment = testBase.TestHost.Services.GetService<IHostEnvironment>();
            environment.Should().NotBeNull();
            environment.EnvironmentName.Should().Be("Alpha");
            var configuration = testBase.TestHost.Services.GetService<IConfiguration>();
            configuration.Should().NotBeNull();
            configuration.GetValue<string>("ApplicationName").Should().Be("Alpha Tests");
        }

        [TestMethod]
        public void BreakdanceTestBase_TestSetup_CreatesTestHost()
        {
            var testBase = new TestBase();
            testBase.TestSetup();
            testBase.TestHost.Should().NotBeNull();
        }

        [TestMethod]
        public void BreakdanceTestBase_TestSetup_HostIsPopulated()
        {
            var testBase = new TestBase();
            testBase.TestSetup();
            testBase.TestHost.Services.Should().NotBeNull();
            var configuration = testBase.TestHost.Services.GetService<IConfiguration>();
            configuration.Should().NotBeNull();
            (configuration as ConfigurationRoot).Providers.Should().HaveCount(4);
            var environment = testBase.TestHost.Services.GetService<IHostEnvironment>();
            environment.Should().NotBeNull();
        }

        [TestMethod]
        public void BreakdanceTestBase_TestSetup_EnvironmentFromHostBuilder()
        {
            var testBase = new TestBase();
            testBase.TestHostBuilder.UseEnvironment("Development");
            testBase.TestSetup();
            testBase.TestHost.Services.Should().NotBeNull();
            var environment = testBase.TestHost.Services.GetService<IHostEnvironment>();
            environment.Should().NotBeNull();
            environment.EnvironmentName.Should().Be("Development");
        }

        [TestMethod]
        public void BreakdanceTestBase_TestSetup_EnvironmentFromRunSettings()
        {
            var testBase = new TestBase();
            testBase.TestSetup();
            testBase.TestHost.Services.Should().NotBeNull();
            var environment = testBase.TestHost.Services.GetService<IHostEnvironment>();
            environment.Should().NotBeNull();
            environment.EnvironmentName.Should().Be("Alpha");
            var configuration = testBase.TestHost.Services.GetService<IConfiguration>();
            configuration.Should().NotBeNull();
            configuration.GetValue<string>("ApplicationName").Should().Be("Alpha Tests");
        }

    }

    /// <summary>
    /// 
    /// </summary>
    internal class TestBase : BreakdanceTestBase
    {
    }

}
