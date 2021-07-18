using Newtonsoft.Json;

namespace CloudNimble.Breakdance.WebApi.OData
{

    /// <summary>
    /// 
    /// </summary>
    public class ODataV401ResponseBase
    {

        /// <summary>
        /// The context URL for a collection, entity, primitive value, or service document.
        /// </summary>
        [JsonProperty("@context")]
        public string ODataContext { get; set; }

    }

}