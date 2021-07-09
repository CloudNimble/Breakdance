using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

#if NETCOREAPP3_1 || NET5_0_OR_GREATER
namespace CloudNimble.Breakdance.AspNetCore
#else
namespace CloudNimble.Breakdance.WebApi
#endif
{

    /// <summary>
    /// 
    /// </summary>
    public static class HttpClientHelpers
    {

        #region Private Properties

        private static readonly JsonSerializerSettings JsonSerializerDefaults = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "yyyy-MM-ddTHH:mm:ssZ",
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets an <see cref="HttpRequestMessage"/> instance properly configured to be used to make test requests.
        /// </summary>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to use for the request.</param>
        /// <param name="host">
        /// The hostname to use for this request. Defaults to "http://localhost", only change it if that collides with other services running on the local machine.
        /// </param>
        /// <param name="routePrefix">
        /// The routePrefix corresponding to the route already mapped in MapRestierRoute or GetTestableConfiguration. Defaults to "api/test", only change it if absolutely necessary.
        /// </param>
        /// <param name="resource">The resource on the API to be requested.</param>
        /// <param name="acceptHeader">The inbound MIME types to accept. Defaults to "application/json".</param>
        /// <param name="payload"></param>
        /// <param name="jsonSerializerSettings"></param>
        /// <returns>An <see cref="HttpRequestMessage"/> that is ready to be sent through an HttpClient instance configured for the test.</returns>
        public static HttpRequestMessage GetTestableHttpRequestMessage(HttpMethod httpMethod, string host = WebApiConstants.Localhost, string routePrefix = WebApiConstants.RoutePrefix, 
            string resource = null, string acceptHeader = WebApiConstants.DefaultAcceptHeader, object payload = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            if (httpMethod == null)
            {
                throw new ArgumentNullException(nameof(httpMethod));
            }

            var request = new HttpRequestMessage(httpMethod, $"{host}{routePrefix}{resource}");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
            if (httpMethod.Method.StartsWith("P") && payload != null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(payload, jsonSerializerSettings ?? JsonSerializerDefaults), Encoding.UTF8, acceptHeader);
            }

            return request;
        }

        #endregion

    }

}