using System.Threading.Tasks;
using CloudNimble.Breakdance.WebApi;
using CloudNimble.EasyAF.Core;
using Newtonsoft.Json;

namespace System.Net.Http
{

    /// <summary>
    /// Extension methods for making <see cref="HttpClient"/> a little more test-friendly.
    /// </summary>
    public static class HttpClientExtensions
    {

        /// <summary>
        /// Creates an <see cref="HttpRequestMessage"/> for the given configuration and executes it asynchronously through the HttpClient.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance to use.</param>
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
        /// <returns>An <see cref="HttpResponseMessage"/> containing the results of the attempted request.</returns>
        /// <example> 
        /// This sample shows the simplest way to create a testable <see cref="HttpClient"/> and execute a test request, using MSTest and FluentAssertions.
        /// <code>
        /// [TestClass]
        /// public class ApiTests
        /// {
        ///     [TestMethod]
        ///     public async Task TestApi_Companies_ReturnsResults()
        ///     {
        ///         var httpClient = WebApiTestHelpers.GetTestableHttpClient();
        ///         var result = await httpClient.ExecuteTestRequest(HttpMethods.Get, resource = "/Companies");
        ///         result.Should().NotBeNull();
        ///         result.StatusCode.Should().Be(HttpStatusCode.OK);
        ///         var content = await result.Content.ReadAsStringAsync();
        ///         content.Should().NotBeNullOrWhiteSpace();
        ///     }
        /// }
        /// </code>
        /// </example>
        public static async Task<HttpResponseMessage> ExecuteTestRequest(this HttpClient httpClient, HttpMethod httpMethod, string host = WebApiConstants.Localhost, 
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null, string acceptHeader = WebApiConstants.DefaultAcceptHeader, object payload = null, 
            JsonSerializerSettings jsonSerializerSettings = null)
        {
            Ensure.ArgumentNotNull(httpClient, nameof(httpClient));

            var request = HttpClientHelpers.GetTestableHttpRequestMessage(httpMethod, host, routePrefix, resource, acceptHeader, payload, jsonSerializerSettings);
            return await httpClient.SendAsync(request).ConfigureAwait(false);
        }

    }

}