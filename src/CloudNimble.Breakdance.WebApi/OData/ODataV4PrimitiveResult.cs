using CloudNimble.Breakdance.WebApi.OData;
using Newtonsoft.Json;

namespace CloudNimble.Breakdance.WebApi.OData
{

    /// <summary>
    /// A container that allows you to capture metadata from an OData V4 response.
    /// </summary>
    /// <typeparam name="T">The type that will be deserialized from the OData V4 "value" property.</typeparam>
    public class ODataV4PrimitiveResult<T> : ODataV4SingleEntityResponseBase
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("Id@odata.type")]
        public string ODataIdType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("value")]
        public T Value { get; set; }

    }

}