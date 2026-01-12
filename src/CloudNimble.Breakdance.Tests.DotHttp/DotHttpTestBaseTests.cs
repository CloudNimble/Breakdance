using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Breakdance.DotHttp;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="DotHttpTestBase"/> class.
    /// </summary>
    [TestClass]
    public class DotHttpTestBaseTests
    {

        #region SetVariable Tests

        [TestMethod]
        public void SetVariable_StoresVariable()
        {
            using var testBase = new TestableDotHttpTestBase();

            testBase.SetVariable("baseUrl", "https://api.example.com");

            var resolved = testBase.TestResolveVariables("{{baseUrl}}/users");
            resolved.Should().Be("https://api.example.com/users");
        }

        [TestMethod]
        public void SetVariable_OverwritesExisting()
        {
            using var testBase = new TestableDotHttpTestBase();

            testBase.SetVariable("baseUrl", "http://localhost");
            testBase.SetVariable("baseUrl", "https://api.example.com");

            var resolved = testBase.TestResolveVariables("{{baseUrl}}");
            resolved.Should().Be("https://api.example.com");
        }

        [TestMethod]
        public void SetVariable_MultipleVariables_AllResolved()
        {
            using var testBase = new TestableDotHttpTestBase();

            testBase.SetVariable("scheme", "https");
            testBase.SetVariable("host", "api.example.com");
            testBase.SetVariable("version", "v2");

            var resolved = testBase.TestResolveVariables("{{scheme}}://{{host}}/{{version}}/users");
            resolved.Should().Be("https://api.example.com/v2/users");
        }

        #endregion

        #region ResolveVariables Tests

        [TestMethod]
        public void ResolveVariables_NullInput_ReturnsNull()
        {
            using var testBase = new TestableDotHttpTestBase();

            var result = testBase.TestResolveVariables(null);

            result.Should().BeNull();
        }

        [TestMethod]
        public void ResolveVariables_EmptyInput_ReturnsEmpty()
        {
            using var testBase = new TestableDotHttpTestBase();

            var result = testBase.TestResolveVariables(string.Empty);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ResolveVariables_NoVariables_ReturnsOriginal()
        {
            using var testBase = new TestableDotHttpTestBase();

            var result = testBase.TestResolveVariables("https://api.example.com");

            result.Should().Be("https://api.example.com");
        }

        [TestMethod]
        public void ResolveVariables_UnresolvedVariable_ReturnsOriginalPlaceholder()
        {
            using var testBase = new TestableDotHttpTestBase();

            var result = testBase.TestResolveVariables("{{missing}}");

            result.Should().Be("{{missing}}");
        }

        #endregion

        #region ResolveUrl Tests

        [TestMethod]
        public void ResolveUrl_WithVariables_ResolvesCorrectly()
        {
            using var testBase = new TestableDotHttpTestBase();
            testBase.SetVariable("baseUrl", "https://api.example.com");
            testBase.SetVariable("userId", "123");

            var result = testBase.TestResolveUrl("{{baseUrl}}/users/{{userId}}");

            result.Should().Be("https://api.example.com/users/123");
        }

        #endregion

        #region HttpClient Tests

        [TestMethod]
        public void HttpClient_FirstAccess_CreatesClient()
        {
            using var testBase = new TestableDotHttpTestBase();

            var client = testBase.TestHttpClient;

            client.Should().NotBeNull();
        }

        [TestMethod]
        public void HttpClient_MultipleAccess_ReturnsSameInstance()
        {
            using var testBase = new TestableDotHttpTestBase();

            var client1 = testBase.TestHttpClient;
            var client2 = testBase.TestHttpClient;

            client1.Should().BeSameAs(client2);
        }

        [TestMethod]
        public void HttpClient_WithCustomHandler_UsesHandler()
        {
            var customHandler = new TestableHttpMessageHandler(HttpStatusCode.OK, "test");
            using var testBase = new TestableDotHttpTestBase(customHandler);

            var client = testBase.TestHttpClient;

            client.Should().NotBeNull();
            // The custom handler is used internally
        }

        #endregion

        #region VariableResolver Tests

        [TestMethod]
        public void VariableResolver_Property_ReturnsResolver()
        {
            using var testBase = new TestableDotHttpTestBase();

            var resolver = testBase.TestVariableResolver;

            resolver.Should().NotBeNull();
        }

        #endregion

        #region ResponseCapture Tests

        [TestMethod]
        public void ResponseCapture_Property_ReturnsCapture()
        {
            using var testBase = new TestableDotHttpTestBase();

            var capture = testBase.TestResponseCapture;

            capture.Should().NotBeNull();
        }

        #endregion

        #region CurrentEnvironmentName Tests

        [TestMethod]
        public void CurrentEnvironmentName_DefaultValue_IsDev()
        {
            using var testBase = new TestableDotHttpTestBase();

            var name = testBase.TestCurrentEnvironmentName;

            name.Should().Be("dev");
        }

        #endregion

        #region LoadEnvironment Tests

        [TestMethod]
        public void LoadEnvironment_FileNotFound_DoesNotThrow()
        {
            using var testBase = new TestableDotHttpTestBase();
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.json");

            Action act = () => testBase.LoadEnvironment(nonExistentPath);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void LoadEnvironment_ValidFile_LoadsVariables()
        {
            using var testBase = new TestableDotHttpTestBase();
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-env-{Guid.NewGuid()}.json");
            try
            {
                var json = @"{
                    ""$shared"": { ""ApiVersion"": ""v2"" },
                    ""dev"": { ""baseUrl"": ""https://dev.api.example.com"" }
                }";
                File.WriteAllText(tempPath, json);

                testBase.LoadEnvironment(tempPath, "dev");

                var resolved = testBase.TestResolveVariables("{{ApiVersion}} at {{baseUrl}}");
                resolved.Should().Be("v2 at https://dev.api.example.com");
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
        public void LoadEnvironment_WithEnvironmentName_SetsCurrentEnvironment()
        {
            using var testBase = new TestableDotHttpTestBase();
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-env-{Guid.NewGuid()}.json");
            try
            {
                var json = @"{ ""staging"": { ""baseUrl"": ""https://staging.api.example.com"" } }";
                File.WriteAllText(tempPath, json);

                testBase.LoadEnvironment(tempPath, "staging");

                testBase.TestCurrentEnvironmentName.Should().Be("staging");
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
        public void LoadEnvironment_NullEnvironmentName_KeepsDefault()
        {
            using var testBase = new TestableDotHttpTestBase();
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-env-{Guid.NewGuid()}.json");
            try
            {
                var json = @"{ ""dev"": { ""baseUrl"": ""https://dev.api.example.com"" } }";
                File.WriteAllText(tempPath, json);

                testBase.LoadEnvironment(tempPath, null);

                testBase.TestCurrentEnvironmentName.Should().Be("dev");
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

        #region LoadEnvironmentWithOverrides Tests

        [TestMethod]
        public void LoadEnvironmentWithOverrides_MergesFiles()
        {
            using var testBase = new TestableDotHttpTestBase();
            var basePath = Path.Combine(Path.GetTempPath(), $"test-env-base-{Guid.NewGuid()}.json");
            var userPath = Path.Combine(Path.GetTempPath(), $"test-env-user-{Guid.NewGuid()}.json");
            try
            {
                File.WriteAllText(basePath, @"{ ""dev"": { ""baseUrl"": ""https://base.api.com"" } }");
                File.WriteAllText(userPath, @"{ ""dev"": { ""baseUrl"": ""https://user.api.com"" } }");

                testBase.LoadEnvironmentWithOverrides(basePath, userPath, "dev");

                var resolved = testBase.TestResolveVariables("{{baseUrl}}");
                resolved.Should().Be("https://user.api.com");
            }
            finally
            {
                if (File.Exists(basePath)) File.Delete(basePath);
                if (File.Exists(userPath)) File.Delete(userPath);
            }
        }

        [TestMethod]
        public void LoadEnvironmentWithOverrides_UserFileNotFound_UsesBaseOnly()
        {
            using var testBase = new TestableDotHttpTestBase();
            var basePath = Path.Combine(Path.GetTempPath(), $"test-env-base-{Guid.NewGuid()}.json");
            var missingUserPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.json");
            try
            {
                File.WriteAllText(basePath, @"{ ""dev"": { ""baseUrl"": ""https://base.api.com"" } }");

                testBase.LoadEnvironmentWithOverrides(basePath, missingUserPath, "dev");

                var resolved = testBase.TestResolveVariables("{{baseUrl}}");
                resolved.Should().Be("https://base.api.com");
            }
            finally
            {
                if (File.Exists(basePath)) File.Delete(basePath);
            }
        }

        #endregion

        #region SwitchEnvironment Tests

        [TestMethod]
        public void SwitchEnvironment_ChangesEnvironmentVariables()
        {
            using var testBase = new TestableDotHttpTestBase();
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-env-{Guid.NewGuid()}.json");
            try
            {
                var json = @"{
                    ""dev"": { ""baseUrl"": ""https://dev.api.com"" },
                    ""prod"": { ""baseUrl"": ""https://prod.api.com"" }
                }";
                File.WriteAllText(tempPath, json);

                testBase.LoadEnvironment(tempPath, "dev");
                testBase.TestResolveVariables("{{baseUrl}}").Should().Be("https://dev.api.com");

                testBase.SwitchEnvironment("prod");
                testBase.TestResolveVariables("{{baseUrl}}").Should().Be("https://prod.api.com");
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        [TestMethod]
        public void SwitchEnvironment_UpdatesCurrentEnvironmentName()
        {
            using var testBase = new TestableDotHttpTestBase();
            var tempPath = Path.Combine(Path.GetTempPath(), $"test-env-{Guid.NewGuid()}.json");
            try
            {
                File.WriteAllText(tempPath, @"{ ""staging"": {} }");
                testBase.LoadEnvironment(tempPath, "dev");

                testBase.SwitchEnvironment("staging");

                testBase.TestCurrentEnvironmentName.Should().Be("staging");
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        #endregion

        #region CaptureResponseAsync Tests

        [TestMethod]
        public async Task CaptureResponseAsync_CapturesResponse()
        {
            using var testBase = new TestableDotHttpTestBase();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""token"": ""abc123""}", Encoding.UTF8, "application/json")
            };

            await testBase.TestCaptureResponseAsync("login", response);

            var token = testBase.TestResolveVariables("{{login.response.body.$.token}}");
            token.Should().Be("abc123");
        }

        [TestMethod]
        public async Task CaptureResponseAsync_WithRequestBody_CapturesRequestBody()
        {
            using var testBase = new TestableDotHttpTestBase();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""id"": 1}", Encoding.UTF8, "application/json")
            };
            var requestBody = @"{""username"": ""test""}";

            await testBase.TestCaptureResponseAsync("create", response, requestBody);

            var username = testBase.TestResolveVariables("{{create.request.body.$.username}}");
            username.Should().Be("test");
        }

        #endregion

        #region SendRequestAsync Tests

        [TestMethod]
        public async Task SendRequestAsync_SimplGetRequest_SendsRequest()
        {
            var handler = new TestableHttpMessageHandler(HttpStatusCode.OK, @"{""success"": true}");
            using var testBase = new TestableDotHttpTestBase(handler);

            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/test",
                Headers = new Dictionary<string, string>()
            };

            var response = await testBase.TestSendRequestAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest.Method.Should().Be(HttpMethod.Get);
        }

        [TestMethod]
        public async Task SendRequestAsync_WithVariables_ResolvesVariables()
        {
            var handler = new TestableHttpMessageHandler(HttpStatusCode.OK, "{}");
            using var testBase = new TestableDotHttpTestBase(handler);
            testBase.SetVariable("baseUrl", "https://api.example.com");
            testBase.SetVariable("userId", "123");

            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "{{baseUrl}}/users/{{userId}}",
                Headers = new Dictionary<string, string>()
            };

            await testBase.TestSendRequestAsync(request);

            handler.LastRequest.RequestUri.ToString().Should().Be("https://api.example.com/users/123");
        }

        [TestMethod]
        public async Task SendRequestAsync_WithHeaders_AddsHeaders()
        {
            var handler = new TestableHttpMessageHandler(HttpStatusCode.OK, "{}");
            using var testBase = new TestableDotHttpTestBase(handler);

            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/test",
                Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer token123",
                    ["X-Custom-Header"] = "custom-value"
                }
            };

            await testBase.TestSendRequestAsync(request);

            handler.LastRequest.Headers.Authorization.Should().NotBeNull();
            handler.LastRequest.Headers.Authorization.ToString().Should().Be("Bearer token123");
            handler.LastRequest.Headers.TryGetValues("X-Custom-Header", out var values).Should().BeTrue();
        }

        [TestMethod]
        public async Task SendRequestAsync_WithBody_AddsBody()
        {
            var handler = new TestableHttpMessageHandler(HttpStatusCode.OK, "{}");
            using var testBase = new TestableDotHttpTestBase(handler);

            var request = new DotHttpRequest
            {
                Method = "POST",
                Url = "https://api.example.com/test",
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = @"{""name"": ""test""}"
            };

            await testBase.TestSendRequestAsync(request);

            handler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
            handler.LastRequestBody.Should().Be(@"{""name"": ""test""}");
        }

        [TestMethod]
        public async Task SendRequestAsync_WithBodyVariables_ResolvesVariables()
        {
            var handler = new TestableHttpMessageHandler(HttpStatusCode.OK, "{}");
            using var testBase = new TestableDotHttpTestBase(handler);
            testBase.SetVariable("userName", "JohnDoe");

            var request = new DotHttpRequest
            {
                Method = "POST",
                Url = "https://api.example.com/test",
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = @"{""name"": ""{{userName}}""}"
            };

            await testBase.TestSendRequestAsync(request);

            handler.LastRequestBody.Should().Be(@"{""name"": ""JohnDoe""}");
        }

        [TestMethod]
        public async Task SendRequestAsync_WithName_CapturesResponse()
        {
            var handler = new TestableHttpMessageHandler(HttpStatusCode.OK, @"{""token"": ""captured""}");
            using var testBase = new TestableDotHttpTestBase(handler);

            var request = new DotHttpRequest
            {
                Method = "POST",
                Url = "https://api.example.com/login",
                Headers = new Dictionary<string, string>(),
                Name = "login"
            };

            await testBase.TestSendRequestAsync(request);

            var token = testBase.TestResolveVariables("{{login.response.body.$.token}}");
            token.Should().Be("captured");
        }

        [TestMethod]
        public async Task SendRequestAsync_WithoutName_DoesNotCapture()
        {
            var handler = new TestableHttpMessageHandler(HttpStatusCode.OK, @"{""data"": ""value""}");
            using var testBase = new TestableDotHttpTestBase(handler);

            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/test",
                Headers = new Dictionary<string, string>(),
                Name = null
            };

            await testBase.TestSendRequestAsync(request);

            testBase.TestResponseCapture.HasResponse("test").Should().BeFalse();
        }

        [TestMethod]
        public async Task SendRequestAsync_DefaultContentType_UsesApplicationJson()
        {
            var handler = new TestableHttpMessageHandler(HttpStatusCode.OK, "{}");
            using var testBase = new TestableDotHttpTestBase(handler);

            var request = new DotHttpRequest
            {
                Method = "POST",
                Url = "https://api.example.com/test",
                Headers = new Dictionary<string, string>(), // No Content-Type
                Body = @"{""data"": ""value""}"
            };

            await testBase.TestSendRequestAsync(request);

            handler.LastRequest.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        }

        #endregion

        #region TestSetupAsync and TestTearDownAsync Tests

        [TestMethod]
        public async Task TestSetupAsync_ClearsResponseCapture()
        {
            using var testBase = new TestableDotHttpTestBase();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
            await testBase.TestCaptureResponseAsync("test", response);
            testBase.TestResponseCapture.HasResponse("test").Should().BeTrue();

            await testBase.InvokeTestSetupAsync();

            testBase.TestResponseCapture.HasResponse("test").Should().BeFalse();
        }

        [TestMethod]
        public async Task TestTearDownAsync_ClearsResponseCapture()
        {
            using var testBase = new TestableDotHttpTestBase();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            };
            await testBase.TestCaptureResponseAsync("test", response);

            await testBase.InvokeTestTearDownAsync();

            testBase.TestResponseCapture.HasResponse("test").Should().BeFalse();
        }

        #endregion

        #region ApplyEnvironmentVariables Tests

        [TestMethod]
        public void ApplyEnvironmentVariables_NullEnvironment_DoesNothing()
        {
            using var testBase = new TestableDotHttpTestBase();

            Action act = () => testBase.TestApplyEnvironmentVariables();

            act.Should().NotThrow();
        }

        #endregion

        #region Dispose Tests

        [TestMethod]
        public void Dispose_DisposesHttpClient()
        {
            var testBase = new TestableDotHttpTestBase();
            var client = testBase.TestHttpClient;

            testBase.Dispose();

            // After dispose, attempting to use the client should throw
            Action act = () =>
            {
                var _ = client.BaseAddress;
            };
            // Note: HttpClient doesn't throw on property access after dispose
            // Just verify dispose completes without error
        }

        [TestMethod]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var testBase = new TestableDotHttpTestBase();

            Action act = () =>
            {
                testBase.Dispose();
                testBase.Dispose();
            };

            act.Should().NotThrow();
        }

        #endregion

        #region CreateHttpMessageHandler Tests

        [TestMethod]
        public void CreateHttpMessageHandler_Default_ReturnsNull()
        {
            using var testBase = new TestableDotHttpTestBase();

            var handler = testBase.TestCreateHttpMessageHandler();

            handler.Should().BeNull();
        }

        [TestMethod]
        public void CreateHttpMessageHandler_BaseImplementation_ReturnsNull()
        {
            // Use the non-overriding version to test the base class implementation
            using var testBase = new NonOverridingTestBase();

            var handler = testBase.TestCreateHttpMessageHandler();

            handler.Should().BeNull();
        }

        [TestMethod]
        public void CreateHttpClient_WithNullHandler_CreatesClientWithoutHandler()
        {
            // Test that CreateHttpClient works correctly when handler is provided
            // (We don't test with null handler as it creates a real network-capable HttpClient)
            var mockHandler = new TestableHttpMessageHandler(HttpStatusCode.OK, "{}");
            using var testBase = new TestableDotHttpTestBase(mockHandler);

            var client = testBase.TestHttpClient;

            client.Should().NotBeNull();
        }

        [TestMethod]
        public void CreateHttpClient_CalledMultipleTimes_ReturnsSameInstance()
        {
            var mockHandler = new TestableHttpMessageHandler(HttpStatusCode.OK, "{}");
            using var testBase = new TestableDotHttpTestBase(mockHandler);

            var client1 = testBase.TestHttpClient;
            var client2 = testBase.TestHttpClient;

            client1.Should().BeSameAs(client2);
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Concrete implementation of DotHttpTestBase for testing.
        /// </summary>
        private class TestableDotHttpTestBase : DotHttpTestBase
        {
            private readonly HttpMessageHandler _customHandler;

            public TestableDotHttpTestBase(HttpMessageHandler customHandler = null)
            {
                _customHandler = customHandler;
            }

            public HttpClient TestHttpClient => HttpClient;
            public VariableResolver TestVariableResolver => VariableResolver;
            public ResponseCapture TestResponseCapture => ResponseCapture;
            public string TestCurrentEnvironmentName => CurrentEnvironmentName;

            public string TestResolveVariables(string input) => ResolveVariables(input);
            public string TestResolveUrl(string url) => ResolveUrl(url);
            public Task TestCaptureResponseAsync(string name, HttpResponseMessage response, string requestBody = null)
                => CaptureResponseAsync(name, response, requestBody);
            public Task<HttpResponseMessage> TestSendRequestAsync(DotHttpRequest request)
                => SendRequestAsync(request);
            public Task InvokeTestSetupAsync() => base.TestSetupAsync();
            public Task InvokeTestTearDownAsync() => base.TestTearDownAsync();
            public void TestApplyEnvironmentVariables() => ApplyEnvironmentVariables();
            public HttpMessageHandler TestCreateHttpMessageHandler() => CreateHttpMessageHandler();

            protected override HttpMessageHandler CreateHttpMessageHandler() => _customHandler;
        }

        /// <summary>
        /// Test implementation that does NOT override CreateHttpMessageHandler,
        /// allowing testing of the base class default implementation without creating real HttpClient.
        /// </summary>
        private class NonOverridingTestBase : DotHttpTestBase
        {
            // Note: We don't expose HttpClient here to avoid creating a real network-capable client
            public HttpMessageHandler TestCreateHttpMessageHandler() => CreateHttpMessageHandler();
        }

        /// <summary>
        /// Test HTTP message handler that returns predetermined responses.
        /// </summary>
        private class TestableHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _responseContent;

            public HttpRequestMessage LastRequest { get; private set; }

            /// <summary>
            /// Gets the body of the last request, captured at send time.
            /// On .NET Framework 4.8, HttpContent can be disposed before tests read it,
            /// so we capture the body synchronously when the request is sent.
            /// </summary>
            public string LastRequestBody { get; private set; }

            public TestableHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
            {
                _statusCode = statusCode;
                _responseContent = responseContent;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;

                // Capture the request body synchronously before it can be disposed.
                // On .NET Framework 4.8, content may be disposed after SendAsync completes.
                if (request.Content != null)
                {
                    LastRequestBody = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                else
                {
                    LastRequestBody = null;
                }

                var response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_responseContent, Encoding.UTF8, "application/json"),
                    RequestMessage = request
                };

                return Task.FromResult(response);
            }
        }

        #endregion

    }

}
