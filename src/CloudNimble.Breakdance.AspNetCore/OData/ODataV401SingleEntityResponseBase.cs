using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.AspNetCore.OData
{

    /// <summary>
    /// 
    /// </summary>
    public class ODataV401SingleEntityResponseBase : ODataV401ResponseBase
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@type")]
        public string ODataType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@id")]
        public string ODataId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("editLink")]
        public string ODataEditLink { get; set; }

    }

}
