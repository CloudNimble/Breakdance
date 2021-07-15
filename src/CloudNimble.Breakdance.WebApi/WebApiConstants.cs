#if NETCOREAPP3_1_OR_GREATER
namespace CloudNimble.Breakdance.AspNetCore
#else
namespace CloudNimble.Breakdance.WebApi
#endif
{

    /// <summary>
    /// A set of constants used by BreakDance.WebApi to simplify the configuration of test runs.
    /// </summary>
    /// <remarks>
    /// Since unit testing a WebApi should not require knowledge of a *specific* endpoint Url to execute (that's required in *integration* testing),
    /// these constants allow the test to run in a way that abstracts the details of configuring the API away from the developer. That allows the
    /// developer to focus on what is being tested, not on messing with configuration.
    /// </remarks>
    public static class WebApiConstants
    {

        /// <summary>
        /// Specifies the Accept HTTP header required for JSON payloads.
        /// </summary>
        public const string DefaultAcceptHeader = "application/json";

        /// <summary>
        /// Specifies the default testing HTTP host.
        /// </summary>
        public const string Localhost = "http://localhost/";

        /// <summary>
        /// Specifies the default prefix that should be appended to the host to route the request to the API.
        /// </summary>
        public const string RoutePrefix = "api/tests";

        /// <summary>
        /// The default name of the route for the ASP.NET route dictionary.
        /// </summary>
        public const string RouteName = "api/tests";

    }

}