using Newtonsoft.Json;

namespace CloudNimble.Breakdance.WebApi.OData
{

    /// <summary>
    /// 
    /// </summary>
    public class ODataV4SingleEntityResponseBase : ODataV4ResponseBase
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("@odata.type")]
        public string ODataType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("@odata.id")]
        public string ODataId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("@odata.editLink")]
        public string ODataEditLink { get; set; }

    }

}
