using System.Net.Http;

namespace System.Web.Http
{

    /// <summary>
    /// Extension methods for making <see cref="HttpConfiguration"/> a little more test-friendly.
    /// </summary>
    public static class HttpConfigurationExtensions
    {

        /// <summary>
        /// Creates a new <see cref="HttpServer"/> for a given <see cref="HttpConfiguration"/>, and returns a new <see cref="HttpClient"/> that uses said <see cref="HttpServer"/>.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> to use with the internal <see cref="HttpServer"/>.</param>
        /// <returns>
        /// An <see cref="HttpClient"/> whose configuration is bonded to an <see cref="HttpServer"/> so developers don't have to manually configure all of the elements required to
        /// successfully test the API.
        /// </returns>
        public static HttpClient GetTestableHttpClient(this HttpConfiguration config)
        {
            //RWM: You may be compelled to think that we should track a static instance of HttpClient. And that would be normal. but because someone could
            //     test different APIs in the same test run, we can't.
            //     Maybe at some point we get smart and put them in a static dictionary and do lookups. But today is not that day.
            return new HttpClient(config.GetTestableHttpServer());
        }

        /// <summary>
        /// Gets a new <see cref="HttpServer" /> instance for a given <see cref="HttpConfiguration" />, suitable for use in unit tests.
        /// </summary>
        /// <returns>A new <see cref="HttpServer" /> instance whose InnerHandler allows for automatic HTTP redirects.</returns>
        public static HttpServer GetTestableHttpServer(this HttpConfiguration config)
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
