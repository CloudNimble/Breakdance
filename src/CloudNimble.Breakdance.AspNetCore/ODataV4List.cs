using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.WebApi
{

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ODataV4List<T>
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string ODataContext { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("value")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<T> Items { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    }

}