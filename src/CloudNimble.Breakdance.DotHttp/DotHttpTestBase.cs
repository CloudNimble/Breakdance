using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.DotHttp.Models;

namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Base class for generated .http file tests. Provides HTTP client management,
    /// variable resolution, and response capture for request chaining.
    /// </summary>
    /// <example>
    /// <code>
    /// public partial class ApiTests : DotHttpTestBase
    /// {
    ///     protected override HttpMessageHandler CreateHttpMessageHandler()
    ///     {
    ///         // Use cached responses for deterministic tests
    ///         return new TestCacheReadDelegatingHandler("ResponseFiles");
    ///     }
    ///
    ///     partial void OnLoginSetup()
    ///     {
    ///         SetVariable("baseUrl", "https://api.example.com");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Inherits from BreakdanceTestBase for integration with the Breakdance testing framework.
    /// Generated test classes are partial, allowing customization of setup and assertions.
    /// </remarks>
    public abstract class DotHttpTestBase : BreakdanceTestBase
    {

        #region Fields

        private string _currentEnvironmentName = "dev";
        private DotHttpEnvironment _environment;
        private readonly EnvironmentLoader _environmentLoader = new EnvironmentLoader();
        private HttpClient _httpClient;
        private HttpMessageHandler _httpMessageHandler;
        private readonly ResponseCapture _responseCapture = new ResponseCapture();
        private readonly VariableResolver _variableResolver = new VariableResolver();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current environment name.
        /// </summary>
        protected string CurrentEnvironmentName => _currentEnvironmentName;

        /// <summary>
        /// Gets the HTTP client used for making requests.
        /// </summary>
        protected HttpClient HttpClient => _httpClient ??= CreateHttpClient();

        /// <summary>
        /// Gets the response capture for request chaining.
        /// </summary>
        protected ResponseCapture ResponseCapture => _responseCapture;

        /// <summary>
        /// Gets the variable resolver for {{variable}} substitution.
        /// </summary>
        protected VariableResolver VariableResolver => _variableResolver;

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads environment configuration from an http-client.env.json file.
        /// </summary>
        /// <param name="filePath">The path to the environment file.</param>
        /// <param name="environmentName">The environment to use (e.g., "dev", "staging").</param>
        /// <example>
        /// <code>
        /// LoadEnvironment("http-client.env.json", "dev");
        /// // Now variables from the dev environment are available
        /// </code>
        /// </example>
        public void LoadEnvironment(string filePath, string environmentName = null)
        {
            _environment = _environmentLoader.LoadFromFile(filePath);
            _currentEnvironmentName = environmentName ?? _currentEnvironmentName;

            ApplyEnvironmentVariables();
        }

        /// <summary>
        /// Loads environment configuration with user overrides.
        /// </summary>
        /// <param name="baseFilePath">The path to the http-client.env.json file.</param>
        /// <param name="userFilePath">The path to the http-client.env.json.user file.</param>
        /// <param name="environmentName">The environment to use.</param>
        /// <example>
        /// <code>
        /// LoadEnvironmentWithOverrides(
        ///     "http-client.env.json",
        ///     "http-client.env.json.user",
        ///     "dev");
        /// </code>
        /// </example>
        public void LoadEnvironmentWithOverrides(string baseFilePath, string userFilePath, string environmentName = null)
        {
            _environment = _environmentLoader.LoadFromFile(baseFilePath);
            _environment = _environmentLoader.MergeWithUserOverrides(_environment, userFilePath);
            _currentEnvironmentName = environmentName ?? _currentEnvironmentName;

            ApplyEnvironmentVariables();
        }

        /// <summary>
        /// Sets a variable for use in request resolution.
        /// </summary>
        /// <param name="name">The variable name (without @ prefix or {{}} wrapper).</param>
        /// <param name="value">The variable value.</param>
        /// <example>
        /// <code>
        /// SetVariable("baseUrl", "https://api.example.com");
        /// SetVariable("apiKey", "my-secret-key");
        /// </code>
        /// </example>
        public void SetVariable(string name, string value)
        {
            _variableResolver.SetVariable(name, value);
        }

        /// <summary>
        /// Switches to a different environment from the loaded configuration.
        /// </summary>
        /// <param name="environmentName">The environment name to switch to.</param>
        /// <example>
        /// <code>
        /// LoadEnvironment("http-client.env.json", "dev");
        /// // Run dev tests...
        /// SwitchEnvironment("staging");
        /// // Now running with staging variables
        /// </code>
        /// </example>
        public void SwitchEnvironment(string environmentName)
        {
            _currentEnvironmentName = environmentName;
            ApplyEnvironmentVariables();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Captures a response for use in subsequent chained requests.
        /// </summary>
        /// <param name="name">The request name (from # @name directive).</param>
        /// <param name="response">The HTTP response to capture.</param>
        /// <param name="requestBody">The original request body.</param>
        /// <example>
        /// <code>
        /// var response = await HttpClient.SendAsync(request);
        /// await CaptureResponseAsync("login", response, requestBody);
        /// // Now {{login.response.body.$.token}} can be resolved
        /// </code>
        /// </example>
        protected async Task CaptureResponseAsync(string name, HttpResponseMessage response, string requestBody = null)
        {
            await _responseCapture.CaptureAsync(name, response, requestBody).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the HTTP client for requests. Override to provide custom configuration.
        /// </summary>
        /// <returns>A configured HttpClient instance.</returns>
        /// <example>
        /// <code>
        /// protected override HttpClient CreateHttpClient()
        /// {
        ///     var handler = CreateHttpMessageHandler();
        ///     var client = new HttpClient(handler);
        ///     client.Timeout = TimeSpan.FromSeconds(30);
        ///     return client;
        /// }
        /// </code>
        /// </example>
        protected virtual HttpClient CreateHttpClient()
        {
            _httpMessageHandler = CreateHttpMessageHandler();

            if (_httpMessageHandler != null)
            {
                return new HttpClient(_httpMessageHandler);
            }

            return new HttpClient();
        }

        /// <summary>
        /// Creates the HTTP message handler. Override to provide custom handlers
        /// like TestCacheReadDelegatingHandler for cached responses.
        /// </summary>
        /// <returns>An HttpMessageHandler, or null for default behavior.</returns>
        /// <example>
        /// <code>
        /// protected override HttpMessageHandler CreateHttpMessageHandler()
        /// {
        ///     return new TestCacheReadDelegatingHandler("ResponseFiles");
        /// }
        /// </code>
        /// </example>
        protected virtual HttpMessageHandler CreateHttpMessageHandler()
        {
            return null;
        }

        /// <summary>
        /// Resolves a URL with variable substitution.
        /// </summary>
        /// <param name="url">The URL containing {{variable}} placeholders.</param>
        /// <returns>The resolved URL.</returns>
        protected string ResolveUrl(string url)
        {
            return ResolveVariables(url);
        }

        /// <summary>
        /// Resolves all variables in the input string.
        /// </summary>
        /// <param name="input">The input containing {{variable}} placeholders.</param>
        /// <returns>The resolved string.</returns>
        /// <example>
        /// <code>
        /// SetVariable("baseUrl", "https://api.example.com");
        /// var url = ResolveVariables("{{baseUrl}}/users");
        /// // url = "https://api.example.com/users"
        /// </code>
        /// </example>
        protected string ResolveVariables(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // First resolve response references
            var result = _responseCapture.ResolveAllReferences(input);

            // Then resolve regular variables
            result = _variableResolver.Resolve(result);

            return result;
        }

        /// <summary>
        /// Sends an HTTP request with variable resolution.
        /// </summary>
        /// <param name="request">The request definition.</param>
        /// <returns>The HTTP response.</returns>
        /// <example>
        /// <code>
        /// var request = new DotHttpRequest
        /// {
        ///     Method = "GET",
        ///     Url = "{{baseUrl}}/users",
        ///     Name = "getUsers"
        /// };
        /// var response = await SendRequestAsync(request);
        /// </code>
        /// </example>
        protected async Task<HttpResponseMessage> SendRequestAsync(DotHttpRequest request)
        {
            var httpRequest = new HttpRequestMessage(
                new HttpMethod(request.Method),
                ResolveUrl(request.Url));

            // Add headers
            foreach (var header in request.Headers)
            {
                var resolvedValue = ResolveVariables(header.Value);
                httpRequest.Headers.TryAddWithoutValidation(header.Key, resolvedValue);
            }

            // Add body if present
            if (!string.IsNullOrEmpty(request.Body))
            {
                var resolvedBody = ResolveVariables(request.Body);
                var contentType = request.Headers.ContainsKey("Content-Type")
                    ? request.Headers["Content-Type"]
                    : "application/json";
                httpRequest.Content = new StringContent(resolvedBody, Encoding.UTF8, contentType);
            }

            var response = await HttpClient.SendAsync(httpRequest).ConfigureAwait(false);

            // Capture response if request has a name
            if (!string.IsNullOrEmpty(request.Name))
            {
                await CaptureResponseAsync(request.Name, response, request.Body).ConfigureAwait(false);
            }

            return response;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Disposes of managed resources.
        /// </summary>
        /// <param name="disposing">Whether disposing is occurring.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
                _httpMessageHandler?.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Sets up the test environment.
        /// </summary>
        public override Task TestSetupAsync()
        {
            _responseCapture.Clear();
            return base.TestSetupAsync();
        }

        /// <summary>
        /// Cleans up resources after each test.
        /// </summary>
        public override Task TestTearDownAsync()
        {
            _responseCapture.Clear();
            return base.TestTearDownAsync();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Applies the current environment's variables to the resolver.
        /// </summary>
        internal void ApplyEnvironmentVariables()
        {
            if (_environment == null)
            {
                return;
            }

            var resolved = _environmentLoader.GetResolvedVariables(_environment, _currentEnvironmentName);
            _variableResolver.SetVariables(resolved);
        }

        #endregion

    }

}
