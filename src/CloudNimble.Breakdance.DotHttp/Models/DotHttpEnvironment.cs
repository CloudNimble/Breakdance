using System.Collections.Generic;

namespace CloudNimble.Breakdance.DotHttp.Models
{

    /// <summary>
    /// Represents the configuration from an http-client.env.json file.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///   "$shared": {
    ///     "ApiVersion": "v2"
    ///   },
    ///   "dev": {
    ///     "HostAddress": "https://localhost:5001"
    ///   },
    ///   "prod": {
    ///     "HostAddress": "https://api.example.com"
    ///   }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Supports $shared variables, environment-specific values, and provider-based secrets.
    /// </remarks>
    public class DotHttpEnvironment
    {

        #region Properties

        /// <summary>
        /// Gets or sets the environment-specific variable sets, keyed by environment name.
        /// </summary>
        /// <remarks>
        /// Common environment names include "dev", "staging", and "prod".
        /// </remarks>
        public Dictionary<string, Dictionary<string, EnvironmentValue>> Environments { get; set; }
            = new Dictionary<string, Dictionary<string, EnvironmentValue>>();

        /// <summary>
        /// Gets or sets the shared variables that apply to all environments.
        /// </summary>
        /// <remarks>
        /// Parsed from the "$shared" section in http-client.env.json.
        /// </remarks>
        public Dictionary<string, EnvironmentValue> Shared { get; set; } = new Dictionary<string, EnvironmentValue>();

        #endregion

    }

}
