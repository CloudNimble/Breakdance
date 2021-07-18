using Newtonsoft.Json;

namespace CloudNimble.Breakdance.WebApi.OData
{

    /// <summary>
    /// 
    /// </summary>
    public class ODataV401SingleEntityResponseBase : ODataV401ResponseBase
    {

        /// <summary>
        /// The ETag of the entity or collection, as appropriate.
        /// </summary>
        [JsonProperty("@etag")]
        public string ODataETag { get; set; }

        /// <summary>
        /// The ID of the entity.
        /// </summary>
        [JsonProperty("@id")]
        public string ODataId { get; set; }

        /// <summary>
        /// The link used to edit/update the entity, if the entity is updatable and the id does not represent a URL 
        /// that can be used to edit the entity.
        /// </summary>
        [JsonProperty("@editLink")]
        public string ODataEditLink { get; set; }

    }

}