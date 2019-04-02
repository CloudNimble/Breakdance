using Newtonsoft.Json;
using System.Collections.Generic;

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
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("value")]
        public List<T> Items { get; set; }

    }

}