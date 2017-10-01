using System.Web.Http;

namespace System.Net.Http
{

    /// <summary>
    /// 
    /// </summary>
    public static class HttpConfigurationExtensions
    {

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

    }
}
