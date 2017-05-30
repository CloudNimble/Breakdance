using AdvancedREI.Breakdance.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Publishers.OData;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Extensions;
using System.Xml.Linq;

namespace AdvancedREI.Breakdance.Restier
{

    /// <summary>
    /// A set of methods that make it easier to pull out Restier runtime components for unit testing.
    /// </summary>
    /// <remarks>See TestierGeneratorTests.cs for more examples of how to use these methods.</remarks>
    public static class RestierTestHelpers
    {

        #region Constants

        private const string routeName = "test";
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
        public static async Task<HttpResponseMessage> ExecuteTestRequest<T>(HttpMethod httpMethod, string host = WebApiConstants.Localhost, string routeName = routeName, 
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null, string acceptHeader = WebApiConstants.DefaultAcceptHeader) where T : ApiBase
        {
            var config = await GetTestableRestierConfiguration<T>(routeName, routePrefix);
            var client = config.GetTestableHttpClient();
            return await client.ExecuteTestRequest(httpMethod, host, routePrefix, resource, acceptHeader);
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
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null) where T : ApiBase
        {
            var client = config.GetTestableHttpClient();
            var request = HttpClientHelpers.GetTestableHttpRequestMessage(httpMethod, WebApiConstants.Localhost, routePrefix, resource);
            return await client.SendAsync(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <returns></returns>
        public static async Task<ApiBase> GetTestableApiInstance<T>(string routeName = routeName, string routePrefix = WebApiConstants.RoutePrefix) where T : ApiBase
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
        public static async Task<TService> GetTestableApiService<TApi, TService>(string routeName = routeName, string routePrefix = WebApiConstants.RoutePrefix)
             where TApi : ApiBase
             where TService : class
        {
            var config = await GetTestableRestierConfiguration<TApi>(routeName, routePrefix);
            var request = HttpClientHelpers.GetTestableHttpRequestMessage(HttpMethod.Get, WebApiConstants.Localhost, routePrefix);
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
        public static async Task<HttpConfiguration> GetTestableRestierConfiguration<T>(string routeName = routeName, string routePrefix = WebApiConstants.RoutePrefix) where T : ApiBase
        {
            var config = new HttpConfiguration();
            await config.MapRestierRoute<T>(routeName, routePrefix);
            return config;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <returns></returns>
        public static async Task<HttpClient> GetTestableHttpClient<T>(string routeName = routeName, string routePrefix = WebApiConstants.RoutePrefix) where T : ApiBase
        {
            var config = await GetTestableRestierConfiguration<T>(routeName, routePrefix);
            return new HttpClient(new HttpServer(config));
        }

        /// <summary>
        /// Gets the <see cref="IEdmModel"/> instance for a given API, whether it used a custom ModelBuilder or the <see cref="RestierModelBuilder"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<IEdmModel> GetTestableModelAsync<T>(string routeName = routeName, string routePrefix = WebApiConstants.RoutePrefix) where T : ApiBase
        {
            var api = await GetTestableApiInstance<T>(routeName, routePrefix);
            return await api.GetModelAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static async Task<string> GetApiMetadata<T>(string host = WebApiConstants.Localhost, string routeName = routeName, string routePrefix = WebApiConstants.RoutePrefix) where T : ApiBase
        {
            var response = await ExecuteTestRequest<T>(HttpMethod.Get, host, routeName, routePrefix, "/$metadata");
            var result = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(result);
            return doc.ToString();
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
            var doc = XDocument.Parse(result);
            result = doc.ToString();
            System.IO.File.WriteAllText(filePath, result);
        }

        #endregion

    }

}