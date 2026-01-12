namespace CloudNimble.Breakdance.DotHttp.Models
{

    /// <summary>
    /// Represents a value in the environment configuration.
    /// </summary>
    /// <example>
    /// <code>
    /// // Simple string value
    /// { "ApiKey": "dev-key-123" }
    ///
    /// // Provider-based secret
    /// {
    ///   "ApiKey": {
    ///     "provider": "AzureKeyVault",
    ///     "secretName": "ProdApiKey",
    ///     "resourceId": "/subscriptions/.../vaults/my-vault"
    ///   }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Can be a simple string or a provider-based secret reference (AspnetUserSecrets, AzureKeyVault, etc.).
    /// </remarks>
    public class EnvironmentValue
    {

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this is a provider-based secret.
        /// </summary>
        /// <remarks>
        /// Returns true when <see cref="Provider"/> is not null or empty.
        /// </remarks>
        public bool IsSecret => !string.IsNullOrWhiteSpace(Provider);

        /// <summary>
        /// Gets or sets the provider type for secret resolution.
        /// </summary>
        /// <remarks>
        /// Supported providers include "AspnetUserSecrets", "AzureKeyVault", and "Encrypted".
        /// </remarks>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the Azure resource ID for the AzureKeyVault provider.
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the secret name for provider-based values.
        /// </summary>
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets the simple string value when not using a provider.
        /// </summary>
        public string Value { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates an <see cref="EnvironmentValue"/> from a simple string.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>A new <see cref="EnvironmentValue"/> with the specified value.</returns>
        public static EnvironmentValue FromString(string value)
        {
            return new EnvironmentValue { Value = value };
        }

        #endregion

    }

}
