using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="EnvironmentValue"/> class.
    /// </summary>
    [TestClass]
    public class EnvironmentValueTests
    {

        #region Constructor Tests

        [TestMethod]
        public void EnvironmentValue_DefaultConstructor_HasNullProperties()
        {
            var value = new EnvironmentValue();

            value.Value.Should().BeNull();
            value.Provider.Should().BeNull();
            value.SecretName.Should().BeNull();
            value.ResourceId.Should().BeNull();
        }

        #endregion

        #region IsSecret Property Tests

        [TestMethod]
        public void IsSecret_ReturnsFalse_WhenProviderIsNull()
        {
            var value = new EnvironmentValue { Value = "test" };

            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void IsSecret_ReturnsFalse_WhenProviderIsEmpty()
        {
            var value = new EnvironmentValue { Provider = "" };

            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void IsSecret_ReturnsFalse_WhenProviderIsWhitespace()
        {
            var value = new EnvironmentValue { Provider = "   " };

            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void IsSecret_ReturnsTrue_WhenProviderIsSet()
        {
            var value = new EnvironmentValue { Provider = "AzureKeyVault" };

            value.IsSecret.Should().BeTrue();
        }

        [TestMethod]
        public void IsSecret_ReturnsTrue_WhenProviderIsAspnetUserSecrets()
        {
            var value = new EnvironmentValue { Provider = "AspnetUserSecrets", SecretName = "ApiKey" };

            value.IsSecret.Should().BeTrue();
        }

        [TestMethod]
        public void IsSecret_ReturnsTrue_WhenProviderIsEncrypted()
        {
            var value = new EnvironmentValue { Provider = "Encrypted", SecretName = "ApiKey" };

            value.IsSecret.Should().BeTrue();
        }

        #endregion

        #region FromString Factory Method Tests

        [TestMethod]
        public void FromString_CreatesValueWithString()
        {
            var value = EnvironmentValue.FromString("test-value");

            value.Value.Should().Be("test-value");
            value.Provider.Should().BeNull();
            value.SecretName.Should().BeNull();
            value.ResourceId.Should().BeNull();
            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void FromString_WithNull_CreatesValueWithNull()
        {
            var value = EnvironmentValue.FromString(null);

            value.Value.Should().BeNull();
            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void FromString_WithEmpty_CreatesValueWithEmpty()
        {
            var value = EnvironmentValue.FromString("");

            value.Value.Should().BeEmpty();
            value.IsSecret.Should().BeFalse();
        }

        #endregion

        #region Full Property Tests

        [TestMethod]
        public void AllProperties_CanBeSet()
        {
            var value = new EnvironmentValue
            {
                Value = "test",
                Provider = "AzureKeyVault",
                SecretName = "MySecret",
                ResourceId = "/subscriptions/xxx/vaults/my-vault"
            };

            value.Value.Should().Be("test");
            value.Provider.Should().Be("AzureKeyVault");
            value.SecretName.Should().Be("MySecret");
            value.ResourceId.Should().Be("/subscriptions/xxx/vaults/my-vault");
            value.IsSecret.Should().BeTrue();
        }

        #endregion

    }

}
