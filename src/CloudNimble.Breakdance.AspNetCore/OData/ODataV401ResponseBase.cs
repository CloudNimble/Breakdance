using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.AspNetCore.OData
{

    /// <summary>
    /// 
    /// </summary>
    public class ODataV401ResponseBase
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonPropertyName("@context")]
        public string ODataContext { get; set; }

    }

}
