using System.Net.Http;
using System.Net.Http.Headers;

namespace AdvancedREI.Breakdance.WebApi
{

    /// <summary>
    /// 
    /// </summary>
    public static class HttpClientHelpers
    {

        /// <summary>
        /// Gets an <see cref="HttpRequestMessage"/> instance properly configured to be used to make test requests.
        /// </summary>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to use for the request.</param>
        /// <param name="host"></param>
        /// <param name="routePrefix">The routePrefix corresponding to the route already mapped in MapRestierRoute or GetTestableConfiguration.</param>
        /// <param name="resource">The resource on the API to be requested.</param>
        /// <param name="acceptHeader"></param>
        /// <returns>An <see cref="HttpRequestMessage"/> that is ready to be sent through an HttpClient instance configured for the test.</returns>
        public static HttpRequestMessage GetTestableHttpRequestMessage(HttpMethod httpMethod, string host = WebApiConstants.Localhost, string routePrefix = WebApiConstants.RoutePrefix, 
            string resource = null, string acceptHeader = WebApiConstants.DefaultAcceptHeader)
        {
            var request = new HttpRequestMessage(httpMethod, host + routePrefix + resource);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
            return request;
        }

    }

}