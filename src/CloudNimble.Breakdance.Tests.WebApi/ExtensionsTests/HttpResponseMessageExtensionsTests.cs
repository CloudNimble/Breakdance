using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore.OData;

namespace CloudNimble.Breakdance.Tests.AspNetCore
#else
using CloudNimble.Breakdance.WebApi.OData;

namespace CloudNimble.Breakdance.Tests.WebApi
#endif
{

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class HttpResponseMessageExtensionsTests
    {

        [TestMethod]
        public async Task DeserializeResponseAsync_SingleEntity()
        {
            var client = new HttpClient();
            var response = await client.GetAsync("https://services.odata.org/TripPinRESTierService/People");
            var (Result, ErrorContent) = await response.DeserializeResponseAsync<ODataV4List<ExpandoObject>>();
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