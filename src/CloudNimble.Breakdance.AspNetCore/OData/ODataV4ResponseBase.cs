using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.AspNetCore.OData
{

    /// <summary>
    /// 
    /// </summary>
    public class ODataV4ResponseBase
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@odata.context")]
        public string ODataContext { get; set; }

    }

}
