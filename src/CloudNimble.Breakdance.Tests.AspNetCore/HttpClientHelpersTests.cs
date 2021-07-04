using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;

namespace Breakdance.Tests.AspNetCore
{

    [TestClass]
    public class HttpClientHelpersTests
    {

        /// <summary>
        /// Tests the shortest path to getting a populated <see cref="HttpRequestMessage"/>.
        /// </summary>
        [TestMethod]
        public void GetTestableHttpRequestMessage_ReturnsHttpRequestMessage()
        {
            var message = HttpClientHelpers.GetTestableHttpRequestMessage(HttpMethod.Get);
            message.Should().NotBeNull();
            message.Should().BeOfType<HttpRequestMessage>();
            message.Content.Should().BeNull();
        }

    }
}
