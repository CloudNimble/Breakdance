using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.WebApi
{

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ODataV4Entity<T>
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@odata.type")]
        public string ODataType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@odata.id")]
        public string ODataId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@odata.editLink")]
        public string ODataEditLink { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("Id@odata.type")]
        public string ODataIdType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("value")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<T> Items { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    }

}