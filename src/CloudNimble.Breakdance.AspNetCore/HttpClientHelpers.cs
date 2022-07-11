using Flurl;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudNimble.Breakdance.AspNetCore
{

    /// <summary>
    /// Helper methods for dealing with <see cref="HttpRequestMessage"/>.
    /// </summary>
    public static class HttpClientHelpers
    {

        /// <summary>
        /// Sets up serialization options
        /// </summary>
        /// <remarks>Slightly different implementation between core versions.</remarks>
        private static readonly JsonSerializerOptions JsonSerializerDefaults = new()
        {
            // ignore all null-value properties when serializing or deserializing (different implementation in net3.1 vs net5+)
#if NETCOREAPP3_1
            IgnoreNullValues = true,
#endif
#if NET5_0_OR_GREATER
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#endif
        };

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
            string resource = "", string acceptHeader = WebApiConstants.DefaultAcceptHeader, object payload = null, JsonSerializerOptions jsonSerializerSettings = null)
        {
            if (httpMethod == null)
            {
                throw new ArgumentNullException(nameof(httpMethod));
            }

            var request = new HttpRequestMessage(httpMethod, Url.Combine(host, routePrefix, resource));
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
            if (httpMethod.Method.StartsWith("P") && payload != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(payload, jsonSerializerSettings ?? JsonSerializerDefaults), Encoding.UTF8, acceptHeader);
            }

            return request;
        }

        #endregion

    }

}
