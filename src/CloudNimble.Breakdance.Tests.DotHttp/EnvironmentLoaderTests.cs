using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CloudNimble.Breakdance.DotHttp;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="EnvironmentLoader"/> class.
    /// </summary>
    [TestClass]
    public class EnvironmentLoaderTests
    {

        #region Parse Tests

        [TestMethod]
        public void Parse_NullContent_ReturnsEmptyEnvironment()
        {
            var loader = new EnvironmentLoader();

            var result = loader.Parse(null);

            result.Should().NotBeNull();
            result.Shared.Should().BeEmpty();
            result.Environments.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_EmptyContent_ReturnsEmptyEnvironment()
        {
            var loader = new EnvironmentLoader();

            var result = loader.Parse(string.Empty);

            result.Should().NotBeNull();
            result.Shared.Should().BeEmpty();
            result.Environments.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_WhitespaceContent_ReturnsEmptyEnvironment()
        {
            var loader = new EnvironmentLoader();

            var result = loader.Parse("   ");

            result.Should().NotBeNull();
            result.Shared.Should().BeEmpty();
            result.Environments.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_EmptyJsonObject_ReturnsEmptyEnvironment()
        {
            var loader = new EnvironmentLoader();

            var result = loader.Parse("{}");

            result.Should().NotBeNull();
            result.Shared.Should().BeEmpty();
            result.Environments.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_SharedVariablesOnly_ParsesCorrectly()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""$shared"": {
                    ""ApiVersion"": ""v2"",
                    ""Timeout"": ""30""
                }
            }";

            var result = loader.Parse(json);

            result.Shared.Should().HaveCount(2);
            result.Shared["ApiVersion"].Value.Should().Be("v2");
            result.Shared["Timeout"].Value.Should().Be("30");
            result.Environments.Should().BeEmpty();
        }

        [TestMethod]
        public void Parse_SingleEnvironment_ParsesCorrectly()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""dev"": {
                    ""HostAddress"": ""https://localhost:5001"",
                    ""ApiKey"": ""dev-key-123""
                }
            }";

            var result = loader.Parse(json);

            result.Environments.Should().HaveCount(1);
            result.Environments["dev"].Should().HaveCount(2);
            result.Environments["dev"]["HostAddress"].Value.Should().Be("https://localhost:5001");
            result.Environments["dev"]["ApiKey"].Value.Should().Be("dev-key-123");
        }

        [TestMethod]
        public void Parse_MultipleEnvironments_ParsesCorrectly()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""dev"": {
                    ""HostAddress"": ""https://localhost:5001""
                },
                ""staging"": {
                    ""HostAddress"": ""https://staging.api.example.com""
                },
                ""prod"": {
                    ""HostAddress"": ""https://api.example.com""
                }
            }";

            var result = loader.Parse(json);

            result.Environments.Should().HaveCount(3);
            result.Environments.Should().ContainKeys("dev", "staging", "prod");
        }

        [TestMethod]
        public void Parse_SharedAndEnvironments_ParsesBoth()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""$shared"": {
                    ""ApiVersion"": ""v2""
                },
                ""dev"": {
                    ""HostAddress"": ""https://localhost:5001""
                }
            }";

            var result = loader.Parse(json);

            result.Shared.Should().HaveCount(1);
            result.Environments.Should().HaveCount(1);
            result.Shared["ApiVersion"].Value.Should().Be("v2");
            result.Environments["dev"]["HostAddress"].Value.Should().Be("https://localhost:5001");
        }

        [TestMethod]
        public void Parse_ProviderBasedSecret_ParsesSecretProperties()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""staging"": {
                    ""ApiKey"": {
                        ""provider"": ""AspnetUserSecrets"",
                        ""secretName"": ""StagingApiKey""
                    }
                }
            }";

            var result = loader.Parse(json);

            result.Environments["staging"]["ApiKey"].Provider.Should().Be("AspnetUserSecrets");
            result.Environments["staging"]["ApiKey"].SecretName.Should().Be("StagingApiKey");
            result.Environments["staging"]["ApiKey"].IsSecret.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_AzureKeyVaultSecret_ParsesAllProperties()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""prod"": {
                    ""ApiKey"": {
                        ""provider"": ""AzureKeyVault"",
                        ""secretName"": ""ProdApiKey"",
                        ""resourceId"": ""/subscriptions/123/vaults/my-vault""
                    }
                }
            }";

            var result = loader.Parse(json);

            var value = result.Environments["prod"]["ApiKey"];
            value.Provider.Should().Be("AzureKeyVault");
            value.SecretName.Should().Be("ProdApiKey");
            value.ResourceId.Should().Be("/subscriptions/123/vaults/my-vault");
            value.IsSecret.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_NumericValue_ConvertsToString()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""dev"": {
                    ""Timeout"": 30,
                    ""MaxRetries"": 3
                }
            }";

            var result = loader.Parse(json);

            result.Environments["dev"]["Timeout"].Value.Should().Be("30");
            result.Environments["dev"]["MaxRetries"].Value.Should().Be("3");
        }

        [TestMethod]
        public void Parse_BooleanTrue_ConvertsToString()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""dev"": {
                    ""Enabled"": true
                }
            }";

            var result = loader.Parse(json);

            result.Environments["dev"]["Enabled"].Value.Should().Be("true");
        }

        [TestMethod]
        public void Parse_BooleanFalse_ConvertsToString()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""dev"": {
                    ""Enabled"": false
                }
            }";

            var result = loader.Parse(json);

            result.Environments["dev"]["Enabled"].Value.Should().Be("false");
        }

        [TestMethod]
        public void Parse_NullValue_PreservesNull()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""dev"": {
                    ""OptionalValue"": null
                }
            }";

            var result = loader.Parse(json);

            result.Environments["dev"]["OptionalValue"].Value.Should().BeNull();
        }

        [TestMethod]
        public void Parse_InvalidJson_ThrowsJsonException()
        {
            var loader = new EnvironmentLoader();
            var invalidJson = "{ invalid json }";

            Action act = () => loader.Parse(invalidJson);

            act.Should().Throw<JsonException>();
        }

        #endregion

        #region GetResolvedVariables Tests

        [TestMethod]
        public void GetResolvedVariables_EmptyEnvironment_ReturnsEmpty()
        {
            var loader = new EnvironmentLoader();
            var environment = new DotHttpEnvironment();

            var result = loader.GetResolvedVariables(environment, "dev");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetResolvedVariables_SharedOnly_ReturnsShared()
        {
            var loader = new EnvironmentLoader();
            var environment = new DotHttpEnvironment
            {
                Shared = new Dictionary<string, EnvironmentValue>
                {
                    ["ApiVersion"] = EnvironmentValue.FromString("v2")
                }
            };

            var result = loader.GetResolvedVariables(environment, "dev");

            result.Should().HaveCount(1);
            result["ApiVersion"].Should().Be("v2");
        }

        [TestMethod]
        public void GetResolvedVariables_EnvironmentOnly_ReturnsEnvironmentVars()
        {
            var loader = new EnvironmentLoader();
            var environment = new DotHttpEnvironment
            {
                Environments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
                {
                    ["dev"] = new Dictionary<string, EnvironmentValue>
                    {
                        ["HostAddress"] = EnvironmentValue.FromString("https://localhost:5001")
                    }
                }
            };

            var result = loader.GetResolvedVariables(environment, "dev");

            result.Should().HaveCount(1);
            result["HostAddress"].Should().Be("https://localhost:5001");
        }

        [TestMethod]
        public void GetResolvedVariables_SharedAndEnvironment_MergesBoth()
        {
            var loader = new EnvironmentLoader();
            var environment = new DotHttpEnvironment
            {
                Shared = new Dictionary<string, EnvironmentValue>
                {
                    ["ApiVersion"] = EnvironmentValue.FromString("v2")
                },
                Environments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
                {
                    ["dev"] = new Dictionary<string, EnvironmentValue>
                    {
                        ["HostAddress"] = EnvironmentValue.FromString("https://localhost:5001")
                    }
                }
            };

            var result = loader.GetResolvedVariables(environment, "dev");

            result.Should().HaveCount(2);
            result["ApiVersion"].Should().Be("v2");
            result["HostAddress"].Should().Be("https://localhost:5001");
        }

        [TestMethod]
        public void GetResolvedVariables_EnvironmentOverridesShared()
        {
            var loader = new EnvironmentLoader();
            var environment = new DotHttpEnvironment
            {
                Shared = new Dictionary<string, EnvironmentValue>
                {
                    ["ApiKey"] = EnvironmentValue.FromString("shared-key")
                },
                Environments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
                {
                    ["dev"] = new Dictionary<string, EnvironmentValue>
                    {
                        ["ApiKey"] = EnvironmentValue.FromString("dev-key")
                    }
                }
            };

            var result = loader.GetResolvedVariables(environment, "dev");

            result["ApiKey"].Should().Be("dev-key");
        }

        [TestMethod]
        public void GetResolvedVariables_NonExistentEnvironment_ReturnsOnlyShared()
        {
            var loader = new EnvironmentLoader();
            var environment = new DotHttpEnvironment
            {
                Shared = new Dictionary<string, EnvironmentValue>
                {
                    ["ApiVersion"] = EnvironmentValue.FromString("v2")
                },
                Environments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
                {
                    ["dev"] = new Dictionary<string, EnvironmentValue>
                    {
                        ["HostAddress"] = EnvironmentValue.FromString("https://localhost:5001")
                    }
                }
            };

            var result = loader.GetResolvedVariables(environment, "staging");

            result.Should().HaveCount(1);
            result["ApiVersion"].Should().Be("v2");
        }

        [TestMethod]
        public void GetResolvedVariables_SecretValue_ReturnsPlaceholder()
        {
            var loader = new EnvironmentLoader();
            var environment = new DotHttpEnvironment
            {
                Environments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
                {
                    ["prod"] = new Dictionary<string, EnvironmentValue>
                    {
                        ["ApiKey"] = new EnvironmentValue
                        {
                            Provider = "AzureKeyVault",
                            SecretName = "ProdApiKey"
                        }
                    }
                }
            };

            var result = loader.GetResolvedVariables(environment, "prod");

            result["ApiKey"].Should().Be("{{secret:AzureKeyVault:ProdApiKey}}");
        }

        [TestMethod]
        public void GetResolvedVariables_NullValue_ReturnsNull()
        {
            var loader = new EnvironmentLoader();
            var environment = new DotHttpEnvironment
            {
                Environments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
                {
                    ["dev"] = new Dictionary<string, EnvironmentValue>
                    {
                        ["OptionalValue"] = null
                    }
                }
            };

            var result = loader.GetResolvedVariables(environment, "dev");

            result["OptionalValue"].Should().BeNull();
        }

        #endregion

        #region LoadFromFile Tests

        [TestMethod]
        public void LoadFromFile_FileDoesNotExist_ReturnsEmptyEnvironment()
        {
            var loader = new EnvironmentLoader();
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.json");

            var result = loader.LoadFromFile(nonExistentPath);

            result.Should().NotBeNull();
            result.Shared.Should().BeEmpty();
            result.Environments.Should().BeEmpty();
        }

        [TestMethod]
        public void LoadFromFile_ValidFile_ParsesContent()
        {
            var loader = new EnvironmentLoader();
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-env-{Guid.NewGuid()}.json");
            try
            {
                var json = @"{
                    ""$shared"": { ""ApiVersion"": ""v2"" },
                    ""dev"": { ""HostAddress"": ""https://localhost:5001"" }
                }";
                File.WriteAllText(tempPath, json);

                var result = loader.LoadFromFile(tempPath);

                result.Shared.Should().HaveCount(1);
                result.Environments.Should().HaveCount(1);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [TestMethod]
        public void LoadFromFile_EmptyFile_ReturnsEmptyEnvironment()
        {
            var loader = new EnvironmentLoader();
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-env-{Guid.NewGuid()}.json");
            try
            {
                File.WriteAllText(tempPath, "{}");

                var result = loader.LoadFromFile(tempPath);

                result.Shared.Should().BeEmpty();
                result.Environments.Should().BeEmpty();
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        #endregion

        #region MergeWithUserOverrides Tests

        [TestMethod]
        public void MergeWithUserOverrides_UserFileDoesNotExist_ReturnsBaseEnvironment()
        {
            var loader = new EnvironmentLoader();
            var baseEnv = new DotHttpEnvironment
            {
                Shared = new Dictionary<string, EnvironmentValue>
                {
                    ["ApiVersion"] = EnvironmentValue.FromString("v2")
                }
            };
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.user.json");

            var result = loader.MergeWithUserOverrides(baseEnv, nonExistentPath);

            result.Should().BeSameAs(baseEnv);
            result.Shared["ApiVersion"].Value.Should().Be("v2");
        }

        [TestMethod]
        public void MergeWithUserOverrides_UserFileOverridesShared()
        {
            var loader = new EnvironmentLoader();
            var baseEnv = new DotHttpEnvironment
            {
                Shared = new Dictionary<string, EnvironmentValue>
                {
                    ["ApiKey"] = EnvironmentValue.FromString("base-key")
                }
            };
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-user-{Guid.NewGuid()}.json");
            try
            {
                var userJson = @"{ ""$shared"": { ""ApiKey"": ""user-key"" } }";
                File.WriteAllText(tempPath, userJson);

                var result = loader.MergeWithUserOverrides(baseEnv, tempPath);

                result.Shared["ApiKey"].Value.Should().Be("user-key");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [TestMethod]
        public void MergeWithUserOverrides_UserFileAddsNewSharedVariable()
        {
            var loader = new EnvironmentLoader();
            var baseEnv = new DotHttpEnvironment
            {
                Shared = new Dictionary<string, EnvironmentValue>
                {
                    ["ApiVersion"] = EnvironmentValue.FromString("v2")
                }
            };
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-user-{Guid.NewGuid()}.json");
            try
            {
                var userJson = @"{ ""$shared"": { ""DebugMode"": ""true"" } }";
                File.WriteAllText(tempPath, userJson);

                var result = loader.MergeWithUserOverrides(baseEnv, tempPath);

                result.Shared.Should().HaveCount(2);
                result.Shared["ApiVersion"].Value.Should().Be("v2");
                result.Shared["DebugMode"].Value.Should().Be("true");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [TestMethod]
        public void MergeWithUserOverrides_UserFileOverridesEnvironmentVariable()
        {
            var loader = new EnvironmentLoader();
            var baseEnv = new DotHttpEnvironment
            {
                Environments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
                {
                    ["dev"] = new Dictionary<string, EnvironmentValue>
                    {
                        ["ApiKey"] = EnvironmentValue.FromString("base-dev-key")
                    }
                }
            };
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-user-{Guid.NewGuid()}.json");
            try
            {
                var userJson = @"{ ""dev"": { ""ApiKey"": ""user-dev-key"" } }";
                File.WriteAllText(tempPath, userJson);

                var result = loader.MergeWithUserOverrides(baseEnv, tempPath);

                result.Environments["dev"]["ApiKey"].Value.Should().Be("user-dev-key");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [TestMethod]
        public void MergeWithUserOverrides_UserFileAddsNewEnvironment()
        {
            var loader = new EnvironmentLoader();
            var baseEnv = new DotHttpEnvironment
            {
                Environments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
                {
                    ["dev"] = new Dictionary<string, EnvironmentValue>
                    {
                        ["HostAddress"] = EnvironmentValue.FromString("https://localhost:5001")
                    }
                }
            };
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-user-{Guid.NewGuid()}.json");
            try
            {
                var userJson = @"{ ""local"": { ""HostAddress"": ""https://localhost:3000"" } }";
                File.WriteAllText(tempPath, userJson);

                var result = loader.MergeWithUserOverrides(baseEnv, tempPath);

                result.Environments.Should().HaveCount(2);
                result.Environments.Should().ContainKeys("dev", "local");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        [TestMethod]
        public void MergeWithUserOverrides_UserFileAddsVariableToExistingEnvironment()
        {
            var loader = new EnvironmentLoader();
            var baseEnv = new DotHttpEnvironment
            {
                Environments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
                {
                    ["dev"] = new Dictionary<string, EnvironmentValue>
                    {
                        ["HostAddress"] = EnvironmentValue.FromString("https://localhost:5001")
                    }
                }
            };
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-user-{Guid.NewGuid()}.json");
            try
            {
                var userJson = @"{ ""dev"": { ""DebugMode"": ""true"" } }";
                File.WriteAllText(tempPath, userJson);

                var result = loader.MergeWithUserOverrides(baseEnv, tempPath);

                result.Environments["dev"].Should().HaveCount(2);
                result.Environments["dev"]["HostAddress"].Value.Should().Be("https://localhost:5001");
                result.Environments["dev"]["DebugMode"].Value.Should().Be("true");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        #endregion

        #region ParseEnvironmentValues Tests

        [TestMethod]
        public void ParseEnvironmentValues_EmptyObject_ReturnsEmptyDictionary()
        {
            var loader = new EnvironmentLoader();
            var json = "{}";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseEnvironmentValues(doc.RootElement);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ParseEnvironmentValues_MultipleValues_ParsesAll()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""Var1"": ""value1"",
                ""Var2"": ""value2"",
                ""Var3"": ""value3""
            }";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseEnvironmentValues(doc.RootElement);

            result.Should().HaveCount(3);
            result["Var1"].Value.Should().Be("value1");
            result["Var2"].Value.Should().Be("value2");
            result["Var3"].Value.Should().Be("value3");
        }

        #endregion

        #region ParseValue Tests

        [TestMethod]
        public void ParseValue_StringValue_ReturnsEnvironmentValue()
        {
            var loader = new EnvironmentLoader();
            var json = @"""hello world""";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Value.Should().Be("hello world");
            result.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void ParseValue_IntegerValue_ReturnsStringRepresentation()
        {
            var loader = new EnvironmentLoader();
            var json = "42";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Value.Should().Be("42");
        }

        [TestMethod]
        public void ParseValue_DecimalValue_ReturnsStringRepresentation()
        {
            var loader = new EnvironmentLoader();
            var json = "3.14";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Value.Should().Be("3.14");
        }

        [TestMethod]
        public void ParseValue_TrueValue_ReturnsTrue()
        {
            var loader = new EnvironmentLoader();
            var json = "true";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Value.Should().Be("true");
        }

        [TestMethod]
        public void ParseValue_FalseValue_ReturnsFalse()
        {
            var loader = new EnvironmentLoader();
            var json = "false";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Value.Should().Be("false");
        }

        [TestMethod]
        public void ParseValue_NullValue_ReturnsNull()
        {
            var loader = new EnvironmentLoader();
            var json = "null";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Value.Should().BeNull();
        }

        [TestMethod]
        public void ParseValue_ObjectWithProvider_ParsesAsSecret()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""provider"": ""AspnetUserSecrets"",
                ""secretName"": ""MySecret""
            }";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Provider.Should().Be("AspnetUserSecrets");
            result.SecretName.Should().Be("MySecret");
            result.IsSecret.Should().BeTrue();
        }

        [TestMethod]
        public void ParseValue_ObjectWithResourceId_ParsesResourceId()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""provider"": ""AzureKeyVault"",
                ""secretName"": ""MySecret"",
                ""resourceId"": ""/subscriptions/xxx/vaults/my-vault""
            }";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.ResourceId.Should().Be("/subscriptions/xxx/vaults/my-vault");
        }

        [TestMethod]
        public void ParseValue_ObjectWithValue_ParsesValue()
        {
            var loader = new EnvironmentLoader();
            var json = @"{ ""value"": ""explicit-value"" }";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Value.Should().Be("explicit-value");
        }

        [TestMethod]
        public void ParseValue_EmptyObject_ReturnsEmptyEnvironmentValue()
        {
            var loader = new EnvironmentLoader();
            var json = "{}";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Provider.Should().BeNull();
            result.SecretName.Should().BeNull();
            result.Value.Should().BeNull();
        }

        [TestMethod]
        public void ParseValue_ArrayValue_ReturnsRawText()
        {
            var loader = new EnvironmentLoader();
            var json = @"[1, 2, 3]";
            using var doc = JsonDocument.Parse(json);

            var result = loader.ParseValue(doc.RootElement);

            result.Value.Should().Be("[1, 2, 3]");
        }

        #endregion

        #region ResolveValue Tests

        [TestMethod]
        public void ResolveValue_NullValue_ReturnsNull()
        {
            var loader = new EnvironmentLoader();

            var result = loader.ResolveValue(null);

            result.Should().BeNull();
        }

        [TestMethod]
        public void ResolveValue_SimpleValue_ReturnsValue()
        {
            var loader = new EnvironmentLoader();
            var value = EnvironmentValue.FromString("hello");

            var result = loader.ResolveValue(value);

            result.Should().Be("hello");
        }

        [TestMethod]
        public void ResolveValue_SecretValue_ReturnsPlaceholder()
        {
            var loader = new EnvironmentLoader();
            var value = new EnvironmentValue
            {
                Provider = "AzureKeyVault",
                SecretName = "MyApiKey"
            };

            var result = loader.ResolveValue(value);

            result.Should().Be("{{secret:AzureKeyVault:MyApiKey}}");
        }

        [TestMethod]
        public void ResolveValue_AspNetUserSecrets_ReturnsPlaceholder()
        {
            var loader = new EnvironmentLoader();
            var value = new EnvironmentValue
            {
                Provider = "AspnetUserSecrets",
                SecretName = "DatabasePassword"
            };

            var result = loader.ResolveValue(value);

            result.Should().Be("{{secret:AspnetUserSecrets:DatabasePassword}}");
        }

        [TestMethod]
        public void ResolveValue_EmptyStringValue_ReturnsEmptyString()
        {
            var loader = new EnvironmentLoader();
            var value = EnvironmentValue.FromString(string.Empty);

            var result = loader.ResolveValue(value);

            result.Should().BeEmpty();
        }

        #endregion

        #region Real-World Scenario Tests

        [TestMethod]
        public void FullScenario_ParseAndResolve_WorksCorrectly()
        {
            var loader = new EnvironmentLoader();
            var json = @"{
                ""$shared"": {
                    ""ApiVersion"": ""v2"",
                    ""Timeout"": ""30""
                },
                ""dev"": {
                    ""HostAddress"": ""https://localhost:5001"",
                    ""ApiKey"": ""dev-key-123""
                },
                ""staging"": {
                    ""HostAddress"": ""https://staging.api.example.com"",
                    ""ApiKey"": {
                        ""provider"": ""AspnetUserSecrets"",
                        ""secretName"": ""StagingApiKey""
                    }
                },
                ""prod"": {
                    ""HostAddress"": ""https://api.example.com"",
                    ""ApiKey"": {
                        ""provider"": ""AzureKeyVault"",
                        ""secretName"": ""ProdApiKey"",
                        ""resourceId"": ""/subscriptions/123/vaults/my-vault""
                    }
                }
            }";

            var environment = loader.Parse(json);

            var devVars = loader.GetResolvedVariables(environment, "dev");
            devVars["ApiVersion"].Should().Be("v2");
            devVars["Timeout"].Should().Be("30");
            devVars["HostAddress"].Should().Be("https://localhost:5001");
            devVars["ApiKey"].Should().Be("dev-key-123");

            var stagingVars = loader.GetResolvedVariables(environment, "staging");
            stagingVars["ApiVersion"].Should().Be("v2");
            stagingVars["HostAddress"].Should().Be("https://staging.api.example.com");
            stagingVars["ApiKey"].Should().Be("{{secret:AspnetUserSecrets:StagingApiKey}}");

            var prodVars = loader.GetResolvedVariables(environment, "prod");
            prodVars["HostAddress"].Should().Be("https://api.example.com");
            prodVars["ApiKey"].Should().Be("{{secret:AzureKeyVault:ProdApiKey}}");
        }

        #endregion

    }

}
