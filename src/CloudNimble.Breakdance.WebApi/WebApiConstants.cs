namespace CloudNimble.Breakdance.WebApi
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
        /// 
        /// </summary>
        public const string DefaultAcceptHeader = "application/json";

        /// <summary>
        /// 
        /// </summary>
        public const string Localhost = "http://localhost/";

        /// <summary>
        /// 
        /// </summary>
        public const string RoutePrefix = "api/test";

    }

}