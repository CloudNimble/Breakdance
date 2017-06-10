using System.Net.Http;
using System.Web.Http;

namespace AdvancedREI.Breakdance.WebApi
{
    public static class WebApiTestHelpers
    {

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <returns></returns>
        public static HttpConfiguration GetTestableConfiguration()
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            return config;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static HttpServer GetTestableServer()
        {
            return GetTestableConfiguration().GetTestableServer();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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