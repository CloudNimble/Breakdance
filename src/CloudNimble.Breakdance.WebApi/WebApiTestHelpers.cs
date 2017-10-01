using System.Net.Http;
using System.Web.Http;

namespace CloudNimble.Breakdance.WebApi
{

    /// <summary>
    /// A set of methods that make it easier to pull out WebApi runtime components for unit testing.
    /// </summary>
    /// <remarks>See WebApiTestHelperTests.cs for more examples of how to use these methods.</remarks>
    public static class WebApiTestHelpers
    {

        /// <summary>
        /// Gets a new <see cref="HttpConfiguration" /> using the default AttributeRouting mapping engine, suitable for use in unit tests.
        /// </summary>
        /// <returns>A new <see cref="HttpConfiguration" /> instance.</returns>
        public static HttpConfiguration GetTestableConfiguration()
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            return config;
        }

        /// <summary>
        /// Gets a new <see cref="HttpServer" /> using the default AttributeRouting mapping engine, suitable for use in unit tests.
        /// </summary>
        /// <returns>A new <see cref="HttpServer" /> instance.</returns>
        public static HttpServer GetTestableHttpServer()
        {
            return GetTestableConfiguration().GetTestableHttpServer();
        }

        /// <summary>
        /// Gets a new <see cref="HttpClient" /> instance using the default AttributeRouting mapping engine, suitable for use in unit tests
        /// </summary>
        /// <returns>a new <see cref="HttpClient" /> instance.</returns>
        public static HttpClient GetTestableHttpClient()
        {
            return GetTestableConfiguration().GetTestableHttpClient();
        }

    }

}