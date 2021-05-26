using CloudNimble.Breakdance.Blazor;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.Blazor
{

    /// <summary>
    /// Tests the functionality of <see cref="TestableNavigationManager"/>.
    /// </summary>
    [TestClass]
    public class TestableNavigationManagerTests
    {

        /// <summary>
        /// Tests <see cref="TestableNavigationManager(string)" />
        /// </summary>
        [TestMethod]
        public void TestableNavigationManager_Constructor_BaseUrl()
        {
            var manager = new TestableNavigationManager("https://localhost:3389/");
            manager.Uri.Should().Be("https://localhost:3389/");
            manager.NavigateTo("test");
            manager.Uri.Should().Be("https://localhost:3389/test");
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void TestableNavigationManager_Constructor_NoParameters()
        {
            var manager = new TestableNavigationManager();
            manager.Uri.Should().Be("https://localhost/");
            manager.NavigateTo("test");
            manager.Uri.Should().Be("https://localhost/test");
        }

    }

}
