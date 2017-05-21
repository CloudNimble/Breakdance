using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Publishers.OData;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Extensions;

namespace AdvancedREI.Restier.Testier
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
            var request = GetTestableRequest(HttpMethod.Get, routePrefix);
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
        /// Gets the <see cref="IEdmModel"/> instance for a given API, whether it used a custom ModelBuilder or the <see cref=RestierModelBuilder"/>.
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
        public static HttpRequestMessage GetTestableRequest(HttpMethod httpMethod, string routePrefix = routePrefix, string resource = null)
        {
            var request = new HttpRequestMessage(httpMethod, localhost + routePrefix + resource);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptHeader));
            return request;
        }

    }

}