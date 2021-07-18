using Newtonsoft.Json;

namespace CloudNimble.Breakdance.WebApi.OData
{

    /// <summary>
    /// 
    /// </summary>
    public class ODataV4ResponseBase
    {

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("@odata.context")]
        public string ODataContext { get; set; }

    }

}
