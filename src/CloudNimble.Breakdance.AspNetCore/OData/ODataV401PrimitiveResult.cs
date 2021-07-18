using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.AspNetCore.OData
{

    /// <summary>
    /// A container that allows you to capture metadata from an OData V4 response.
    /// </summary>
    /// <typeparam name="T">The type that will be deserialized from the OData V4 "value" property.</typeparam>
    public class ODataV401PrimitiveResult<T> : ODataV401SingleEntityResponseBase
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("value")]
        public T Value { get; set; }

    }

}