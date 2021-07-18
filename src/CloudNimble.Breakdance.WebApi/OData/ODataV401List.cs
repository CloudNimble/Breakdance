using Newtonsoft.Json;
using System.Collections.Generic;

namespace CloudNimble.Breakdance.WebApi.OData
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
        [JsonProperty("@count")]
        public long ODataCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("@nextLink")]
        public string ODataNextLink { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("value")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<T> Items { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    }

}