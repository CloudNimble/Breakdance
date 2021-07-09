using CloudNimble.Breakdance.WebApi;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.WebApi
{

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class HttpResponseMessageExtensionTests
    {

        [TestMethod]
        public async Task DeserializeResponseAsync_SingleEntity()
        {
            var client = new HttpClient();
            var response = await client.GetAsync("https://services.odata.org/TripPinRESTierService/People");
            var (Result, ErrorContent) = await response.DeserializeResponseAsync<ODataV4Entity<ExpandoObject>>();
            ErrorContent.Should().BeNullOrEmpty();
            Result.Should().NotBeNull();
        }

        [TestMethod]
        public async Task DeserializeResponseAsync_WrongUrl()
        {
            var client = new HttpClient();
            var response = await client.GetAsync("https://services.odata.org/TripPinRESTierService/Robert");
            var (Result, ErrorContent) = await response.DeserializeResponseAsync<dynamic>();
            ErrorContent.Should().NotBeNullOrEmpty();
            //Result.Should().BeNull();
        }

    }

}