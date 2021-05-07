using CloudNimble.Breakdance.Blazor;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Breakdance.Tests.Blazor
{

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class TestableNavigationManagerTests
    {

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void TestMethod1()
        {
            var manager = new TestableNavigationManager("https://localhost/");
            manager.Uri.Should().Be("https://localhost/");
            manager.NavigateTo("test");
            manager.Uri.Should().Be("https://localhost/test");
        }

    }

}
