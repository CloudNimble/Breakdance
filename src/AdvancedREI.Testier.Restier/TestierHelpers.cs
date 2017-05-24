using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Publishers.OData;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Extensions;

namespace AdvancedREI.Testier.Restier
{

    /// <summary>
    /// A set of methods that make it easier to pull out Restier runtime components for unit testing.
    /// </summary>
    /// <remarks>See TestierGeneratorTests.cs for more examples of how to use these methods.</remarks>
    public static class TestierHelpers
    {

        #region Constants

        private const string localhost = "http://localhost/";
        private const string routeName = "test";
        private const string routePrefix = "api/test";
        private const string acceptHeader = "application/json;odata.metadata=full";

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpMethod"></param>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ExecuteTestRequest<T>(HttpMethod httpMethod, string routeName = routeName, string routePrefix = routePrefix,
            string resource = null) where T : ApiBase
        {
            var config = await GetTestableConfiguration<T>(routeName, routePrefix);
            var client = config.GetTestableHttpClient();
            return await client.ExecuteTestRequest(httpMethod, routePrefix, resource);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="httpMethod"></param>
        /// <param name="routePrefix"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ExecuteTestRequest(this HttpClient httpClient, HttpMethod httpMethod, string routePrefix = routePrefix,
            string resource = null)
        {
            var request = GetTestableHttpRequestMessage(httpMethod, routePrefix, resource);
            return await httpClient.SendAsync(request);
        }

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
        public static async Task<HttpResponseMessage> ExecuteTestRequest<T>(this HttpConfiguration config, HttpMethod httpMethod, string routeName = routeName, 
            string routePrefix = routePrefix, string resource = null) where T : ApiBase
        {
            var client = config.GetTestableHttpClient();
            var request = GetTestableHttpRequestMessage(httpMethod, routePrefix, resource);
            return await client.SendAsync(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <returns></returns>
        public static async Task<ApiBase> GetTestableApiInstance<T>(string routeName = routeName, string routePrefix = routePrefix) where T : ApiBase
        {
            return await GetTestableApiService<T, ApiBase>(routeName, routePrefix);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TApi"></typeparam>
        /// <typeparam name="TService"></typeparam>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <returns></returns>
        public static async Task<TService> GetTestableApiService<TApi, TService>(string routeName = routeName, string routePrefix = routePrefix)
             where TApi : ApiBase
             where TService : class
        {
            var config = await GetTestableConfiguration<TApi>(routeName, routePrefix);
            var request = GetTestableHttpRequestMessage(HttpMethod.Get, routePrefix);
            request.SetConfiguration(config);
            return request.CreateRequestContainer(routeName).GetService<TService>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <returns></returns>
        public static async Task<HttpConfiguration> GetTestableConfiguration<T>(string routeName = routeName, string routePrefix = routePrefix) where T : ApiBase
        {
            var config = new HttpConfiguration();
            await config.MapRestierRoute<T>(routeName, routePrefix);
            return config;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static HttpClient GetTestableHttpClient(this HttpConfiguration config)
        {
            //RWM: You may be compelled to think that we should track a static instance of HttpClient. And that would be normal. but because someone could
            //     test different APIs in the same test run, we can't.
            //     Maybe at some point we get smart and put them in a static dictionary and do lookups. But today is not that day.
            return new HttpClient(new HttpServer(config));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <returns></returns>
        public static async Task<HttpClient> GetTestableHttpClient<T>(string routeName = routeName, string routePrefix = routePrefix) where T : ApiBase
        {
            var config = await GetTestableConfiguration<T>(routeName, routePrefix);
            return new HttpClient(new HttpServer(config));
        }

        /// <summary>
        /// Gets the <see cref="IEdmModel"/> instance for a given API, whether it used a custom ModelBuilder or the <see cref="RestierModelBuilder"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<IEdmModel> GetTestableModelAsync<T>(string routeName = routeName, string routePrefix = routePrefix) where T : ApiBase
        {
            var api = await GetTestableApiInstance<T>(routeName, routePrefix);
            return await api.GetModelAsync();
        }

        /// <summary>
        /// Gets an <see cref="HttpRequestMessage"/> instance properly configured to be used to make test requests.
        /// </summary>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to use for the request.</param>
        /// <param name="routePrefix">The routePrefix corresponding to the route already mapped in MapRestierRoute or GetTestableConfiguration.</param>
        /// <param name="resource">The resource on the API to be requested.</param>
        /// <returns>An <see cref="HttpRequestMessage"/> that is ready to be sent through an HttpClient instance configured for the test.</returns>
        public static HttpRequestMessage GetTestableHttpRequestMessage(HttpMethod httpMethod, string routePrefix = routePrefix, string resource = null)
        {
            var request = new HttpRequestMessage(httpMethod, localhost + routePrefix + resource);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<string> GetApiMetadata<T>(string routeName = routeName, string routePrefix = routePrefix) where T : ApiBase
        {
            var response = await ExecuteTestRequest<T>(HttpMethod.Get, routeName, routePrefix, "/$metadata");
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceDirectory"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static async Task WriteCurrentApiMetadata<T>(string sourceDirectory = "", string suffix = "ApiMetadata") where T : ApiBase
        {
            var filePath = $"{sourceDirectory}{typeof(T).Name}-{suffix}.txt";
            var result = await GetApiMetadata<T>();
            System.IO.File.WriteAllText(filePath, result);
        }

        #endregion

    }

}