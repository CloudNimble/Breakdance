using CloudNimble.Breakdance.Restier;
using CloudNimble.Breakdance.WebApi;
using Microsoft.Restier.Core;
using System.Net.Http;
using System.Threading.Tasks;

namespace System.Web.Http
{

    /// <summary>
    /// 
    /// </summary>
    public static class HttpConfigurationExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="config"></param>
        /// <param name="httpMethod"></param>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ExecuteTestRequest<T>(this HttpConfiguration config, HttpMethod httpMethod, string routeName = RestierTestHelpers.RouteName,
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null) where T : ApiBase
        {
            var client = config.GetTestableHttpClient();
            var request = HttpClientHelpers.GetTestableHttpRequestMessage(httpMethod, WebApiConstants.Localhost, routePrefix, resource);
            return await client.SendAsync(request);
        }

    }
}
