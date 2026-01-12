// This file contains runtime code, not analyzer code. File I/O is allowed at runtime.
#pragma warning disable RS1035

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CloudNimble.Breakdance.DotHttp.Models;

namespace CloudNimble.Breakdance.DotHttp
{

    /// <summary>
    /// Loads and parses http-client.env.json environment files.
    /// </summary>
    /// <example>
    /// <code>
    /// var loader = new EnvironmentLoader();
    /// var environment = loader.LoadFromFile("http-client.env.json");
    /// var variables = loader.GetResolvedVariables(environment, "dev");
    /// </code>
    /// </example>
    /// <remarks>
    /// Supports $shared variables, environment-specific values, provider-based secrets, and .user file overrides.
    /// </remarks>
    public class EnvironmentLoader
    {

        #region Public Methods

        /// <summary>
        /// Gets the resolved variables for a specific environment, including $shared values.
        /// </summary>
        /// <param name="environment">The environment configuration.</param>
        /// <param name="environmentName">The name of the environment (e.g., "dev", "staging").</param>
        /// <returns>A dictionary of resolved variable names and values.</returns>
        /// <example>
        /// <code>
        /// var loader = new EnvironmentLoader();
        /// var env = loader.LoadFromFile("http-client.env.json");
        /// var devVars = loader.GetResolvedVariables(env, "dev");
        /// // devVars contains merged $shared and dev-specific variables
        /// </code>
        /// </example>
        public Dictionary<string, string> GetResolvedVariables(DotHttpEnvironment environment, string environmentName)
        {
            var result = new Dictionary<string, string>();

            // Start with shared variables
            foreach (var kvp in environment.Shared)
            {
                result[kvp.Key] = ResolveValue(kvp.Value);
            }

            // Override with environment-specific variables
            if (environment.Environments.TryGetValue(environmentName, out var envVars))
            {
                foreach (var kvp in envVars)
                {
                    result[kvp.Key] = ResolveValue(kvp.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Loads an environment configuration from a JSON file path.
        /// </summary>
        /// <param name="filePath">The path to the http-client.env.json file.</param>
        /// <returns>A <see cref="DotHttpEnvironment"/> containing the parsed configuration.</returns>
        /// <example>
        /// <code>
        /// var loader = new EnvironmentLoader();
        /// var environment = loader.LoadFromFile("http-client.env.json");
        /// foreach (var envName in environment.Environments.Keys)
        /// {
        ///     Console.WriteLine($"Environment: {envName}");
        /// }
        /// </code>
        /// </example>
        public DotHttpEnvironment LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new DotHttpEnvironment();
            }

            var content = File.ReadAllText(filePath);
            return Parse(content);
        }

        /// <summary>
        /// Merges the user override file (.user) with the base environment.
        /// </summary>
        /// <param name="baseEnvironment">The base environment from http-client.env.json.</param>
        /// <param name="userFilePath">The path to the http-client.env.json.user file.</param>
        /// <returns>The merged environment configuration.</returns>
        /// <example>
        /// <code>
        /// var loader = new EnvironmentLoader();
        /// var baseEnv = loader.LoadFromFile("http-client.env.json");
        /// var mergedEnv = loader.MergeWithUserOverrides(baseEnv, "http-client.env.json.user");
        /// </code>
        /// </example>
        /// <remarks>
        /// User override values take precedence over base environment values.
        /// </remarks>
        public DotHttpEnvironment MergeWithUserOverrides(DotHttpEnvironment baseEnvironment, string userFilePath)
        {
            if (!File.Exists(userFilePath))
            {
                return baseEnvironment;
            }

            var userEnvironment = LoadFromFile(userFilePath);

            // Merge shared variables
            foreach (var kvp in userEnvironment.Shared)
            {
                baseEnvironment.Shared[kvp.Key] = kvp.Value;
            }

            // Merge environment-specific variables
            foreach (var env in userEnvironment.Environments)
            {
                if (!baseEnvironment.Environments.ContainsKey(env.Key))
                {
                    baseEnvironment.Environments[env.Key] = new Dictionary<string, EnvironmentValue>();
                }

                foreach (var kvp in env.Value)
                {
                    baseEnvironment.Environments[env.Key][kvp.Key] = kvp.Value;
                }
            }

            return baseEnvironment;
        }

        /// <summary>
        /// Parses environment configuration from JSON content.
        /// </summary>
        /// <param name="jsonContent">The JSON content of the environment file.</param>
        /// <returns>A <see cref="DotHttpEnvironment"/> containing the parsed configuration.</returns>
        /// <example>
        /// <code>
        /// var loader = new EnvironmentLoader();
        /// var json = @"{
        ///   ""$shared"": { ""ApiVersion"": ""v2"" },
        ///   ""dev"": { ""HostAddress"": ""https://localhost:5001"" }
        /// }";
        /// var environment = loader.Parse(json);
        /// </code>
        /// </example>
        public DotHttpEnvironment Parse(string jsonContent)
        {
            var environment = new DotHttpEnvironment();

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return environment;
            }

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            foreach (var property in root.EnumerateObject())
            {
                if (property.Name == "$shared")
                {
                    environment.Shared = ParseEnvironmentValues(property.Value);
                }
                else
                {
                    environment.Environments[property.Name] = ParseEnvironmentValues(property.Value);
                }
            }

            return environment;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Parses a JSON element containing environment variable definitions.
        /// </summary>
        /// <param name="element">The JSON element to parse.</param>
        /// <returns>A dictionary of variable names to <see cref="EnvironmentValue"/> objects.</returns>
        /// <remarks>
        /// Handles both simple string values and provider-based secret objects.
        /// </remarks>
        internal Dictionary<string, EnvironmentValue> ParseEnvironmentValues(JsonElement element)
        {
            var result = new Dictionary<string, EnvironmentValue>();

            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ParseValue(property.Value);
            }

            return result;
        }

        /// <summary>
        /// Parses a single JSON value into an <see cref="EnvironmentValue"/>.
        /// </summary>
        /// <param name="element">The JSON element representing the value.</param>
        /// <returns>The parsed <see cref="EnvironmentValue"/>.</returns>
        /// <remarks>
        /// Supports string values, objects with provider/secretName/resourceId properties,
        /// numbers, booleans, and null values.
        /// </remarks>
        internal EnvironmentValue ParseValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return EnvironmentValue.FromString(element.GetString());

                case JsonValueKind.Object:
                    // Provider-based secret
                    var envValue = new EnvironmentValue();

                    if (element.TryGetProperty("provider", out var providerElement))
                    {
                        envValue.Provider = providerElement.GetString();
                    }

                    if (element.TryGetProperty("secretName", out var secretNameElement))
                    {
                        envValue.SecretName = secretNameElement.GetString();
                    }

                    if (element.TryGetProperty("resourceId", out var resourceIdElement))
                    {
                        envValue.ResourceId = resourceIdElement.GetString();
                    }

                    if (element.TryGetProperty("value", out var valueElement))
                    {
                        envValue.Value = valueElement.GetString();
                    }

                    return envValue;

                case JsonValueKind.Number:
                    return EnvironmentValue.FromString(element.GetRawText());

                case JsonValueKind.True:
                    return EnvironmentValue.FromString("true");

                case JsonValueKind.False:
                    return EnvironmentValue.FromString("false");

                case JsonValueKind.Null:
                    return EnvironmentValue.FromString(null);

                default:
                    return EnvironmentValue.FromString(element.GetRawText());
            }
        }

        /// <summary>
        /// Resolves an <see cref="EnvironmentValue"/> to its string representation.
        /// </summary>
        /// <param name="value">The environment value to resolve.</param>
        /// <returns>The resolved string value, or a placeholder for secret providers.</returns>
        /// <remarks>
        /// For secret providers (AspnetUserSecrets, AzureKeyVault, etc.), returns a placeholder
        /// in the format {{secret:provider:secretName}}. Actual resolution happens at runtime.
        /// </remarks>
        internal string ResolveValue(EnvironmentValue value)
        {
            if (value == null)
            {
                return null;
            }

            if (!value.IsSecret)
            {
                return value.Value;
            }

            // For secret providers, we return a placeholder.
            // The actual resolution happens at runtime when the tests execute.
            // This allows the generator to work without secrets access.
            return $"{{{{secret:{value.Provider}:{value.SecretName}}}}}";
        }

        #endregion

    }

}
