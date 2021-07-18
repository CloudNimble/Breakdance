using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.AspNetCore.OData
{

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ODataV4List<T> : ODataV4ResponseBase
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@odata.count")]
        public long ODataCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@odata.nextLink")]
        public string ODataNextLink { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("value")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<T> Items { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    }

}