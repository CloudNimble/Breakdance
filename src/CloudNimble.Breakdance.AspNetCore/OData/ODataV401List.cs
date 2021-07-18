using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.AspNetCore.OData
{

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ODataV401List<T> : ODataV401ResponseBase
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@count")]
        public long ODataCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@nextLink")]
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