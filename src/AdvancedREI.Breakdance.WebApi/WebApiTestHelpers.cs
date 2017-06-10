using System.Net.Http;
using System.Web.Http;

namespace AdvancedREI.Breakdance.WebApi
{

    /// <summary>
    /// Helper methods for generic WebAPI testing.
    /// </summary>
    public static class WebApiTestHelpers
    {

        /// <summary>
        /// Returns a new <see cref="HttpConfiguration" /> using the default Attribute Routing mappings.
        /// </summary>
        /// <returns>A new <see cref="HttpConfiguration" /> instance.</returns>
        public static HttpConfiguration GetTestableConfiguration()
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            return config;
        }

        /// <summary>
        /// Returns a new <see cref="HttpServer" /> that uses the default Attribute Routing mappings, suitable for use in unit tests.
        /// </summary>
        /// <returns>A new <see cref="HttpServer" /> instance.</returns>
        public static HttpServer GetTestableServer()
        {
            return GetTestableConfiguration().GetTestableServer();
        }

        /// <summary>
        /// Returns a new <see cref="HttpServer" /> instance for a given <see cref="HttpConfiguration" />, suitable for use in unit tests.
        /// </summary>
        /// <returns>A new <see cref="HttpServer" /> instance.</returns>
        public static HttpServer GetTestableServer(this HttpConfiguration config)
        {
            return new HttpServer(config)
            {
                InnerHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = true
                }
            };
        }

    }

}