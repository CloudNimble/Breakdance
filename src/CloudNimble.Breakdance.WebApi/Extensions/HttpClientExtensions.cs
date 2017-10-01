using System.Threading.Tasks;
using CloudNimble.Breakdance.WebApi;

namespace System.Net.Http
{

    /// <summary>
    /// 
    /// </summary>
    public static class HttpClientExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="httpMethod"></param>
        /// <param name="host"></param>
        /// <param name="routePrefix"></param>
        /// <param name="resource"></param>
        /// <param name="acceptHeader"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ExecuteTestRequest(this HttpClient httpClient, HttpMethod httpMethod, string host = WebApiConstants.Localhost, 
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null, string acceptHeader = WebApiConstants.DefaultAcceptHeader)
        {
            var request = HttpClientHelpers.GetTestableHttpRequestMessage(httpMethod, host, routePrefix, resource, acceptHeader);
            return await httpClient.SendAsync(request);
        }

    }

}