using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.AspNetCore.OData
{

    /// <summary>
    /// 
    /// </summary>
    public class ODataV4SingleEntityResponseBase : ODataV4ResponseBase
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

    }

}
