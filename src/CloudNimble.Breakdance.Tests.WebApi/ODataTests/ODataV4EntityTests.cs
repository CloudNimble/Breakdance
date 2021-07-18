using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Dynamic;

#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore.OData;
using System.Text.Json;

namespace CloudNimble.Breakdance.Tests.AspNetCore
#else
using CloudNimble.Breakdance.WebApi.OData;
using Newtonsoft.Json;

namespace CloudNimble.Breakdance.Tests.WebApi
#endif

{

    [TestClass]
    public class ODataV4EntityTests
    {

        #region Private Members

        string booleanPayload = " {\"@odata.context\":\"http://localhost/api/tests/$metadata#Edm.Boolean\",\"value\":true}";

        #endregion

        [TestMethod]
        public void Boolean_CanDeserialize()
        {

            var result = Deserialize<ODataV4PrimitiveResult<bool>>(booleanPayload);
            result.Should().NotBeNull();
            result.ODataContext.Should().NotBeNullOrWhiteSpace();
            result.Value.Should().BeTrue();
        }

        [TestMethod]
        public async Task SingleEntity_DeserializesProperly()
        {
            var client = new HttpClient();
            var response = await client.GetAsync("https://services.odata.org/TripPinRESTierService/People('russellwhyte')");
            var (Result, ErrorContent) = await response.DeserializeResponseAsync<ODataV4PrimitiveResult<ExpandoObject>>();
            ErrorContent.Should().BeNullOrEmpty();
            Result.Should().NotBeNull();
            Result.ODataContext.Should().NotBeNullOrWhiteSpace();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        private T Deserialize<T>(string content)
        {
#if NETCOREAPP3_1_OR_GREATER
            return JsonSerializer.Deserialize<T>(content);
#else
            return JsonConvert.DeserializeObject<T>(content);
#endif
        }

    }

}
