using System.Collections.Generic;
using CloudNimble.Breakdance.DotHttp;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for DotHttp model classes.
    /// </summary>
    [TestClass]
    public class ModelTests
    {

        #region DotHttpEnvironment Tests

        [TestMethod]
        public void DotHttpEnvironment_DefaultConstructor_InitializesEmptyCollections()
        {
            var environment = new DotHttpEnvironment();

            environment.Environments.Should().NotBeNull();
            environment.Environments.Should().BeEmpty();
            environment.Shared.Should().NotBeNull();
            environment.Shared.Should().BeEmpty();
        }

        [TestMethod]
        public void DotHttpEnvironment_Environments_CanAddEnvironments()
        {
            var environment = new DotHttpEnvironment();

            environment.Environments["dev"] = new Dictionary<string, EnvironmentValue>
            {
                { "baseUrl", EnvironmentValue.FromString("https://localhost:5001") }
            };

            environment.Environments.Should().ContainKey("dev");
            environment.Environments["dev"].Should().ContainKey("baseUrl");
            environment.Environments["dev"]["baseUrl"].Value.Should().Be("https://localhost:5001");
        }

        [TestMethod]
        public void DotHttpEnvironment_Shared_CanAddSharedVariables()
        {
            var environment = new DotHttpEnvironment();

            environment.Shared["apiVersion"] = EnvironmentValue.FromString("v2");

            environment.Shared.Should().ContainKey("apiVersion");
            environment.Shared["apiVersion"].Value.Should().Be("v2");
        }

        [TestMethod]
        public void DotHttpEnvironment_Environments_CanSetToNewDictionary()
        {
            var environment = new DotHttpEnvironment();
            var newEnvironments = new Dictionary<string, Dictionary<string, EnvironmentValue>>
            {
                { "prod", new Dictionary<string, EnvironmentValue>() }
            };

            environment.Environments = newEnvironments;

            environment.Environments.Should().BeSameAs(newEnvironments);
        }

        [TestMethod]
        public void DotHttpEnvironment_Shared_CanSetToNewDictionary()
        {
            var environment = new DotHttpEnvironment();
            var newShared = new Dictionary<string, EnvironmentValue>
            {
                { "key", EnvironmentValue.FromString("value") }
            };

            environment.Shared = newShared;

            environment.Shared.Should().BeSameAs(newShared);
        }

        #endregion

        #region EnvironmentValue Tests

        [TestMethod]
        public void EnvironmentValue_DefaultConstructor_HasNullProperties()
        {
            var value = new EnvironmentValue();

            value.Value.Should().BeNull();
            value.Provider.Should().BeNull();
            value.SecretName.Should().BeNull();
            value.ResourceId.Should().BeNull();
        }

        [TestMethod]
        public void EnvironmentValue_IsSecret_ReturnsFalse_WhenProviderIsNull()
        {
            var value = new EnvironmentValue { Value = "test" };

            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void EnvironmentValue_IsSecret_ReturnsFalse_WhenProviderIsEmpty()
        {
            var value = new EnvironmentValue { Provider = "" };

            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void EnvironmentValue_IsSecret_ReturnsFalse_WhenProviderIsWhitespace()
        {
            var value = new EnvironmentValue { Provider = "   " };

            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void EnvironmentValue_IsSecret_ReturnsTrue_WhenProviderIsSet()
        {
            var value = new EnvironmentValue { Provider = "AzureKeyVault" };

            value.IsSecret.Should().BeTrue();
        }

        [TestMethod]
        public void EnvironmentValue_FromString_CreatesValueWithString()
        {
            var value = EnvironmentValue.FromString("test-value");

            value.Value.Should().Be("test-value");
            value.Provider.Should().BeNull();
            value.SecretName.Should().BeNull();
            value.ResourceId.Should().BeNull();
            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void EnvironmentValue_FromString_WithNull_CreatesValueWithNull()
        {
            var value = EnvironmentValue.FromString(null);

            value.Value.Should().BeNull();
            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void EnvironmentValue_FromString_WithEmpty_CreatesValueWithEmpty()
        {
            var value = EnvironmentValue.FromString("");

            value.Value.Should().BeEmpty();
            value.IsSecret.Should().BeFalse();
        }

        [TestMethod]
        public void EnvironmentValue_AllProperties_CanBeSet()
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

        [TestMethod]
        public void EnvironmentValue_Provider_AspnetUserSecrets_IsSecret()
        {
            var value = new EnvironmentValue { Provider = "AspnetUserSecrets", SecretName = "ApiKey" };

            value.IsSecret.Should().BeTrue();
        }

        [TestMethod]
        public void EnvironmentValue_Provider_Encrypted_IsSecret()
        {
            var value = new EnvironmentValue { Provider = "Encrypted", SecretName = "ApiKey" };

            value.IsSecret.Should().BeTrue();
        }

        #endregion

        #region DotHttpRequest Tests

        [TestMethod]
        public void DotHttpRequest_DefaultConstructor_InitializesCollections()
        {
            var request = new DotHttpRequest();

            request.Comments.Should().NotBeNull();
            request.Comments.Should().BeEmpty();
            request.DependsOn.Should().NotBeNull();
            request.DependsOn.Should().BeEmpty();
            request.Headers.Should().NotBeNull();
            request.Headers.Should().BeEmpty();
            request.Variables.Should().NotBeNull();
            request.Variables.Should().BeEmpty();
        }

        [TestMethod]
        public void DotHttpRequest_DefaultConstructor_HasNullStringProperties()
        {
            var request = new DotHttpRequest();

            request.Body.Should().BeNull();
            request.BodyFilePath.Should().BeNull();
            request.HttpVersion.Should().BeNull();
            request.Method.Should().BeNull();
            request.Name.Should().BeNull();
            request.Url.Should().BeNull();
        }

        [TestMethod]
        public void DotHttpRequest_DefaultConstructor_HasDefaultLineNumber()
        {
            var request = new DotHttpRequest();

            request.LineNumber.Should().Be(0);
        }

        [TestMethod]
        public void DotHttpRequest_DefaultConstructor_HasNoResponseReferences()
        {
            var request = new DotHttpRequest();

            request.HasResponseReferences.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpRequest_IsFileBody_ReturnsFalse_WhenBodyFilePathIsNull()
        {
            var request = new DotHttpRequest { BodyFilePath = null };

            request.IsFileBody.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpRequest_IsFileBody_ReturnsFalse_WhenBodyFilePathIsEmpty()
        {
            var request = new DotHttpRequest { BodyFilePath = "" };

            request.IsFileBody.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpRequest_IsFileBody_ReturnsTrue_WhenBodyFilePathIsSet()
        {
            var request = new DotHttpRequest { BodyFilePath = "./data.json" };

            request.IsFileBody.Should().BeTrue();
        }

        [TestMethod]
        public void DotHttpRequest_Headers_AreCaseInsensitive()
        {
            var request = new DotHttpRequest();

            request.Headers["Content-Type"] = "application/json";
            request.Headers["content-type"].Should().Be("application/json");
            request.Headers["CONTENT-TYPE"].Should().Be("application/json");
        }

        [TestMethod]
        public void DotHttpRequest_Variables_AreCaseInsensitive()
        {
            var request = new DotHttpRequest();

            request.Variables["BaseUrl"] = "https://example.com";
            request.Variables["baseurl"].Should().Be("https://example.com");
            request.Variables["BASEURL"].Should().Be("https://example.com");
        }

        [TestMethod]
        public void DotHttpRequest_Comments_CanAddMultiple()
        {
            var request = new DotHttpRequest();

            request.Comments.Add("First comment");
            request.Comments.Add("Second comment");

            request.Comments.Should().HaveCount(2);
            request.Comments[0].Should().Be("First comment");
            request.Comments[1].Should().Be("Second comment");
        }

        [TestMethod]
        public void DotHttpRequest_DependsOn_CanAddDependencies()
        {
            var request = new DotHttpRequest();

            request.DependsOn.Add("login");
            request.DependsOn.Add("getUser");

            request.DependsOn.Should().HaveCount(2);
            request.DependsOn.Should().Contain("login");
            request.DependsOn.Should().Contain("getUser");
        }

        [TestMethod]
        public void DotHttpRequest_AllProperties_CanBeSet()
        {
            var request = new DotHttpRequest
            {
                Body = "{\"name\": \"test\"}",
                BodyFilePath = null,
                HttpVersion = "HTTP/1.1",
                LineNumber = 10,
                Method = "POST",
                Name = "CreateUser",
                Url = "{{baseUrl}}/users",
                HasResponseReferences = true
            };

            request.Body.Should().Be("{\"name\": \"test\"}");
            request.BodyFilePath.Should().BeNull();
            request.HttpVersion.Should().Be("HTTP/1.1");
            request.LineNumber.Should().Be(10);
            request.Method.Should().Be("POST");
            request.Name.Should().Be("CreateUser");
            request.Url.Should().Be("{{baseUrl}}/users");
            request.HasResponseReferences.Should().BeTrue();
            request.IsFileBody.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpRequest_CanReplaceCollections()
        {
            var request = new DotHttpRequest();
            var newComments = new List<string> { "New comment" };
            var newDependsOn = new List<string> { "dependency" };
            var newHeaders = new Dictionary<string, string> { { "Accept", "application/json" } };
            var newVariables = new Dictionary<string, string> { { "key", "value" } };

            request.Comments = newComments;
            request.DependsOn = newDependsOn;
            request.Headers = newHeaders;
            request.Variables = newVariables;

            request.Comments.Should().BeSameAs(newComments);
            request.DependsOn.Should().BeSameAs(newDependsOn);
            request.Headers.Should().BeSameAs(newHeaders);
            request.Variables.Should().BeSameAs(newVariables);
        }

        #endregion

        #region DotHttpFile Tests

        [TestMethod]
        public void DotHttpFile_DefaultConstructor_InitializesCollections()
        {
            var file = new DotHttpFile();

            file.Diagnostics.Should().NotBeNull();
            file.Diagnostics.Should().BeEmpty();
            file.Requests.Should().NotBeNull();
            file.Requests.Should().BeEmpty();
            file.Variables.Should().NotBeNull();
            file.Variables.Should().BeEmpty();
        }

        [TestMethod]
        public void DotHttpFile_DefaultConstructor_HasNullFilePath()
        {
            var file = new DotHttpFile();

            file.FilePath.Should().BeNull();
        }

        [TestMethod]
        public void DotHttpFile_HasChainedRequests_ReturnsFalse_WhenNoRequests()
        {
            var file = new DotHttpFile();

            file.HasChainedRequests.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpFile_HasChainedRequests_ReturnsFalse_WhenNoRequestsHaveReferences()
        {
            var file = new DotHttpFile();
            file.Requests.Add(new DotHttpRequest { Name = "request1", HasResponseReferences = false });
            file.Requests.Add(new DotHttpRequest { Name = "request2", HasResponseReferences = false });

            file.HasChainedRequests.Should().BeFalse();
        }

        [TestMethod]
        public void DotHttpFile_HasChainedRequests_ReturnsTrue_WhenAnyRequestHasReferences()
        {
            var file = new DotHttpFile();
            file.Requests.Add(new DotHttpRequest { Name = "request1", HasResponseReferences = false });
            file.Requests.Add(new DotHttpRequest { Name = "request2", HasResponseReferences = true });

            file.HasChainedRequests.Should().BeTrue();
        }

        [TestMethod]
        public void DotHttpFile_HasChainedRequests_ReturnsTrue_WhenAllRequestsHaveReferences()
        {
            var file = new DotHttpFile();
            file.Requests.Add(new DotHttpRequest { Name = "request1", HasResponseReferences = true });
            file.Requests.Add(new DotHttpRequest { Name = "request2", HasResponseReferences = true });

            file.HasChainedRequests.Should().BeTrue();
        }

        [TestMethod]
        public void DotHttpFile_Variables_AreCaseSensitive()
        {
            var file = new DotHttpFile();

            file.Variables["baseUrl"] = "https://lower.com";
            file.Variables["BaseUrl"] = "https://upper.com";

            file.Variables.Should().HaveCount(2);
            file.Variables["baseUrl"].Should().Be("https://lower.com");
            file.Variables["BaseUrl"].Should().Be("https://upper.com");
        }

        [TestMethod]
        public void DotHttpFile_CanAddDiagnostics()
        {
            var file = new DotHttpFile { FilePath = "test.http" };

            var diagnostic = Diagnostic.Create(
                DotHttpFileParser.RequestLineErrorDescriptor,
                Location.Create(
                    "test.http",
                    new TextSpan(0, 0),
                    new LinePositionSpan(new LinePosition(4, 0), new LinePosition(4, 1))),
                "Test error");

            file.Diagnostics.Add(diagnostic);

            file.Diagnostics.Should().HaveCount(1);
            file.Diagnostics[0].Id.Should().Be("DOTHTTP001");
        }

        [TestMethod]
        public void DotHttpFile_CanAddRequests()
        {
            var file = new DotHttpFile();

            file.Requests.Add(new DotHttpRequest { Method = "GET", Url = "/users" });
            file.Requests.Add(new DotHttpRequest { Method = "POST", Url = "/users" });

            file.Requests.Should().HaveCount(2);
        }

        [TestMethod]
        public void DotHttpFile_AllProperties_CanBeSet()
        {
            var file = new DotHttpFile
            {
                FilePath = "api.http"
            };
            file.Variables["baseUrl"] = "https://api.example.com";
            file.Requests.Add(new DotHttpRequest { Method = "GET", Url = "{{baseUrl}}/users" });

            var diagnostic = Diagnostic.Create(
                DotHttpFileParser.BodyWarningDescriptor,
                Location.None,
                "Warning");
            file.Diagnostics.Add(diagnostic);

            file.FilePath.Should().Be("api.http");
            file.Variables.Should().ContainKey("baseUrl");
            file.Requests.Should().HaveCount(1);
            file.Diagnostics.Should().HaveCount(1);
        }

        [TestMethod]
        public void DotHttpFile_CanReplaceCollections()
        {
            var file = new DotHttpFile();
            var newDiagnostics = new List<Diagnostic>();
            var newRequests = new List<DotHttpRequest>();
            var newVariables = new Dictionary<string, string>();

            file.Diagnostics = newDiagnostics;
            file.Requests = newRequests;
            file.Variables = newVariables;

            file.Diagnostics.Should().BeSameAs(newDiagnostics);
            file.Requests.Should().BeSameAs(newRequests);
            file.Variables.Should().BeSameAs(newVariables);
        }

        #endregion

    }

}
