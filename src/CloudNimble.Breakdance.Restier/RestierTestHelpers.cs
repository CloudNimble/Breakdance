using CloudNimble.Breakdance.OData;
using CloudNimble.Breakdance.WebApi;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml.Linq;

namespace CloudNimble.Breakdance.Restier
{

    /// <summary>
    /// A set of methods that make it easier to pull out Restier runtime components for unit testing.
    /// </summary>
    /// <remarks>See RestierTestHelperTests.cs for more examples of how to use these methods.</remarks>
    public static class RestierTestHelpers
    {

        #region Private Members

        private static readonly DefaultQuerySettings QueryDefaults = new DefaultQuerySettings
        {
            EnableCount = true,
            EnableExpand = true,
            EnableFilter = true,
            EnableOrderBy = true,
            EnableSelect = true,
            MaxTop = 10
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Configures the Restier pipeline in-memory and executes a test request against a given service, returning an <see cref="HttpResponseMessage"/> for inspection.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> to use for the request.</param>
        /// <param name="host">
        /// The protocol and host to connect to in order to run the tests. Must end with a forward-slash. Defaults to "http://localhost/", and should not normally be changed. NOTE: This should
        /// NOT be the same as any of your actual running environments, and does not require a port assignment in order to function.
        /// </param>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">
        /// The string that will be appended in between the Host and the Resource when constructing a URL. NOTE: DO NOT set this to the same URL as your deployment environments. 
        /// The prefix is irrelevant, is only for internal testing, and should ONLY be changed if you are testing more than one API in a test method (which is not recommended).
        /// </param>
        /// <param name="resource">The specific resource on the endpoint that will be called. Must start with a forward-slash.</param>
        /// <param name="acceptHeader">The "Accept" header that should be added to the request. Defaults to "application/json;odata.metadata=full".</param>
        /// <param name="defaultQuerySettings">A <see cref="DefaultQuerySettings"/> instabce that defines how OData operations should work. Defaults to everything enabled with a <see cref="DefaultQuerySettings.MaxTop"/> of 10.</param>
        /// <param name="timeZoneInfo">A <see cref="TimeZoneInfo"/> instenace specifying what time zone should be used to translate time payloads into. Defaults to <see cref="TimeZoneInfo.Utc"/>.</param>
        /// <param name="payload">When the <paramref name="httpMethod"/> is <see cref="HttpMethod.Post"/> or <see cref="HttpMethod.Put"/>, this object is serialized to JSON and inserted into the <see cref="HttpRequestMessage.Content"/>.</param>
        /// <param name="jsonSerializerSettings">A <see cref="JsonSerializerSettings"/> instance defining how the payload should be serialized into the request body. Defaults to using Zulu time and will include all properties in the payload, even null ones.</param>
        /// <returns>An <see cref="HttpResponseMessage"/> that contains the managed response for the request for inspection.</returns>
        public static async Task<HttpResponseMessage> ExecuteTestRequest<TApi>(HttpMethod httpMethod, string host = WebApiConstants.Localhost, string routeName = WebApiConstants.RouteName,
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null, string acceptHeader = ODataConstants.DefaultAcceptHeader,
            DefaultQuerySettings defaultQuerySettings = null, TimeZoneInfo timeZoneInfo = null, object payload = null, JsonSerializerSettings jsonSerializerSettings = null)
            where TApi : ApiBase
        {
            var config = await GetTestableRestierConfiguration<TApi>(routeName, routePrefix, defaultQuerySettings, timeZoneInfo);
            var client = config.GetTestableHttpClient();
            return await client.ExecuteTestRequest(httpMethod, host, routePrefix, resource, acceptHeader, payload, jsonSerializerSettings);
        }

        /// <summary>
        /// Retrieves the instance of the Restier API (inheriting from <see cref="ApiBase"/> from the Dependency Injection container.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <returns></returns>
        public static async Task<TApi> GetTestableApiInstance<TApi>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix)
            where TApi : ApiBase => await GetTestableInjectedService<TApi, ApiBase>(routeName, routePrefix) as TApi;

        /// <summary>
        /// Retrieves class instance of type <typeparamref name="TService"/> from the Dependency Injection container.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <typeparam name="TService">The type whose instance should be retrieved from the DI container.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <returns></returns>
        public static async Task<TService> GetTestableInjectedService<TApi, TService>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix)
             where TApi : ApiBase
             where TService : class => (await GetTestableInjectionContainer<TApi>(routeName, routePrefix)).GetService<TService>();

        /// <summary>
        /// Retrieves the Dependency Injection container that was created as a part of the request pipeline.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <returns></returns>
        public static async Task<IServiceProvider> GetTestableInjectionContainer<TApi>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix)
             where TApi : ApiBase
        {
            var config = await GetTestableRestierConfiguration<TApi>(routeName, routePrefix);
            var request = HttpClientHelpers.GetTestableHttpRequestMessage(HttpMethod.Get, WebApiConstants.Localhost, routePrefix);
            request.SetConfiguration(config);
            return request.CreateRequestContainer(routeName);
        }

        /// <summary>
        /// Retrieves an <see cref="HttpConfiguration"> instance that has been configured to execute a given Restier API, along with settings suitable for easy troubleshooting.</see>
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <param name="defaultQuerySettings">A <see cref="DefaultQuerySettings"/> instabce that defines how OData operations should work. Defaults to everything enabled with a <see cref="DefaultQuerySettings.MaxTop"/> of 10.</param>
        /// <param name="timeZoneInfo">A <see cref="TimeZoneInfo"/> instenace specifying what time zone should be used to translate time payloads into. Defaults to <see cref="TimeZoneInfo.Utc"/>.</param>
        /// <returns>An <see cref="HttpConfiguration"/> instance</returns>
        public static async Task<HttpConfiguration> GetTestableRestierConfiguration<TApi>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix, 
            DefaultQuerySettings defaultQuerySettings = null, TimeZoneInfo timeZoneInfo = null)
            where TApi : ApiBase
        {
            var config = new HttpConfiguration();
            config.SetDefaultQuerySettings(defaultQuerySettings ?? QueryDefaults);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.SetTimeZoneInfo(timeZoneInfo ?? TimeZoneInfo.Utc);
            await config.MapRestierRoute<TApi>(routeName, routePrefix);
            return config;
        }

        /// <summary>
        /// Returns a properly configured <see cref="HttpClient"/> that can make reqests to the in-memory Restier context.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <param name="routeName">The name that will be assigned to the route in the route configuration dictionary.</param>
        /// <param name="routePrefix">The string that will be appendedin between the Host and the Resource when constructing a URL.</param>
        /// <returns>A properly configured <see cref="HttpClient"/> that can make reqests to the in-memory Restier context.</returns>
        public static async Task<HttpClient> GetTestableHttpClient<TApi>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix)
            where TApi : ApiBase
        {
            var config = await GetTestableRestierConfiguration<TApi>(routeName, routePrefix);
            return new HttpClient(new HttpServer(config));
        }

        /// <summary>
        /// Retrieves the <see cref="IEdmModel"/> instance for a given API, whether it used a custom ModelBuilder or the RestierModelBuilder.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <returns>An <see cref="IEdmModel"/> instance containing the model used to configure both OData and Restier processing.</returns>
        public static async Task<IEdmModel> GetTestableModelAsync<TApi>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix)
            where TApi : ApiBase
        {
            var api = await GetTestableApiInstance<TApi>(routeName, routePrefix);
            return await api.GetModelAsync();
        }

        /// <summary>
        /// Executes a test request against the configured API endpoint and retrieves the content from the /$metadata endpoint.
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <returns>An <see cref="XDocument"/> containing the results of the metadata request.</returns>
        public static async Task<XDocument> GetApiMetadata<TApi>(string host = WebApiConstants.Localhost, string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix) 
            where TApi : ApiBase
        {
            var response = await ExecuteTestRequest<TApi>(HttpMethod.Get, host, routeName, routePrefix, "/$metadata");
            var result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Trace.WriteLine(result);
                return null;
            }
            return XDocument.Parse(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TApi">The class inheriting from <see cref="ApiBase"/> that implements the Restier API to test.</typeparam>
        /// <param name="sourceDirectory"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static async Task WriteCurrentApiMetadata<TApi>(string sourceDirectory = "", string suffix = "ApiMetadata")
            where TApi : ApiBase
        {
            var filePath = $"{sourceDirectory}{typeof(TApi).Name}-{suffix}.txt";
            var result = await GetApiMetadata<TApi>();
            System.IO.File.WriteAllText(filePath, result.ToString());
        }

        #endregion

    }

}