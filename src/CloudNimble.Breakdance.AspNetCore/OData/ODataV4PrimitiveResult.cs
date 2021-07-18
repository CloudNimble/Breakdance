using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.AspNetCore.OData
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
        [JsonPropertyName("Id@odata.type")]
        public string ODataIdType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("value")]
        public T Value { get; set; }

    }

}