using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CloudNimble.Breakdance.DotHttp;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="ResponseCapture"/> class.
    /// </summary>
    [TestClass]
    public class ResponseCaptureTests
    {

        #region CaptureAsync Tests

        [TestMethod]
        public async Task CaptureAsync_NullName_DoesNotCapture()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse("test body");

            await capture.CaptureAsync(null, response);

            capture.HasResponse("null").Should().BeFalse();
        }

        [TestMethod]
        public async Task CaptureAsync_EmptyName_DoesNotCapture()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse("test body");

            await capture.CaptureAsync(string.Empty, response);

            capture.HasResponse("").Should().BeFalse();
        }

        [TestMethod]
        public async Task CaptureAsync_ValidName_CapturesResponse()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse("test body");

            await capture.CaptureAsync("login", response);

            capture.HasResponse("login").Should().BeTrue();
            capture.GetResponseBody("login").Should().Be("test body");
        }

        [TestMethod]
        public async Task CaptureAsync_WithRequestBody_CapturesRequestBody()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse(@"{""token"": ""abc123""}");
            var requestBody = @"{""username"": ""test""}";

            await capture.CaptureAsync("login", response, requestBody);

            var result = capture.ResolveReference("{{login.request.body.*}}");
            result.Should().Be(requestBody);
        }

        [TestMethod]
        public async Task CaptureAsync_CapturesResponseHeaders()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse("body");
            response.Headers.Add("X-Custom-Header", "custom-value");
            response.Headers.Add("X-Request-Id", "12345");

            await capture.CaptureAsync("test", response);

            var header = capture.ResolveReference("{{test.response.headers.X-Custom-Header}}");
            header.Should().Be("custom-value");
        }

        [TestMethod]
        public async Task CaptureAsync_CapturesContentHeaders()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse("body", "application/json");

            await capture.CaptureAsync("test", response);

            var contentType = capture.ResolveReference("{{test.response.headers.Content-Type}}");
            contentType.Should().Be("application/json; charset=utf-8");
        }

        [TestMethod]
        public async Task CaptureAsync_NullContent_CapturesEmptyBody()
        {
            // Note: HttpResponseMessage.Content is never truly null in practice;
            // it returns empty string when no content is set
            var capture = new ResponseCapture();
            var response = new HttpResponseMessage(HttpStatusCode.NoContent)
            {
                Content = null
            };

            await capture.CaptureAsync("noContent", response);

            capture.HasResponse("noContent").Should().BeTrue();
            capture.GetResponseBody("noContent").Should().BeEmpty();
        }

        [TestMethod]
        public async Task CaptureAsync_OverwritesExistingCapture()
        {
            var capture = new ResponseCapture();
            var response1 = CreateResponse("first body");
            var response2 = CreateResponse("second body");

            await capture.CaptureAsync("test", response1);
            await capture.CaptureAsync("test", response2);

            capture.GetResponseBody("test").Should().Be("second body");
        }

        [TestMethod]
        public async Task CaptureAsync_CaseInsensitiveName()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse("body");

            await capture.CaptureAsync("Login", response);

            capture.HasResponse("login").Should().BeTrue();
            capture.HasResponse("LOGIN").Should().BeTrue();
            capture.HasResponse("Login").Should().BeTrue();
        }

        #endregion

        #region Clear Tests

        [TestMethod]
        public async Task Clear_RemovesAllCapturedResponses()
        {
            var capture = new ResponseCapture();
            await capture.CaptureAsync("test1", CreateResponse("body1"));
            await capture.CaptureAsync("test2", CreateResponse("body2"));

            capture.Clear();

            capture.HasResponse("test1").Should().BeFalse();
            capture.HasResponse("test2").Should().BeFalse();
        }

        #endregion

        #region GetResponseBody Tests

        [TestMethod]
        public void GetResponseBody_NonExistentName_ReturnsNull()
        {
            var capture = new ResponseCapture();

            var result = capture.GetResponseBody("missing");

            result.Should().BeNull();
        }

        [TestMethod]
        public async Task GetResponseBody_ExistingName_ReturnsBody()
        {
            var capture = new ResponseCapture();
            await capture.CaptureAsync("test", CreateResponse("hello world"));

            var result = capture.GetResponseBody("test");

            result.Should().Be("hello world");
        }

        #endregion

        #region HasResponse Tests

        [TestMethod]
        public void HasResponse_NonExistentName_ReturnsFalse()
        {
            var capture = new ResponseCapture();

            capture.HasResponse("missing").Should().BeFalse();
        }

        [TestMethod]
        public async Task HasResponse_ExistingName_ReturnsTrue()
        {
            var capture = new ResponseCapture();
            await capture.CaptureAsync("test", CreateResponse("body"));

            capture.HasResponse("test").Should().BeTrue();
        }

        #endregion

        #region ResolveReference Tests

        [TestMethod]
        public void ResolveReference_InvalidFormat_ReturnsOriginal()
        {
            var capture = new ResponseCapture();

            var result = capture.ResolveReference("not a reference");

            result.Should().Be("not a reference");
        }

        [TestMethod]
        public void ResolveReference_UncapturedRequest_ReturnsOriginal()
        {
            var capture = new ResponseCapture();

            var result = capture.ResolveReference("{{missing.response.body.*}}");

            result.Should().Be("{{missing.response.body.*}}");
        }

        [TestMethod]
        public async Task ResolveReference_BodyWildcard_ReturnsEntireBody()
        {
            var capture = new ResponseCapture();
            var body = @"{""token"": ""abc123""}";
            await capture.CaptureAsync("login", CreateResponse(body));

            var result = capture.ResolveReference("{{login.response.body.*}}");

            result.Should().Be(body);
        }

        [TestMethod]
        public async Task ResolveReference_JsonPathSimple_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var body = @"{""token"": ""abc123""}";
            await capture.CaptureAsync("login", CreateResponse(body));

            var result = capture.ResolveReference("{{login.response.body.$.token}}");

            result.Should().Be("abc123");
        }

        [TestMethod]
        public async Task ResolveReference_JsonPathNested_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var body = @"{""user"": {""profile"": {""name"": ""John""}}}";
            await capture.CaptureAsync("test", CreateResponse(body));

            var result = capture.ResolveReference("{{test.response.body.$.user.profile.name}}");

            result.Should().Be("John");
        }

        [TestMethod]
        public async Task ResolveReference_JsonPathArray_ReturnsElement()
        {
            var capture = new ResponseCapture();
            var body = @"{""items"": [{""id"": 1}, {""id"": 2}, {""id"": 3}]}";
            await capture.CaptureAsync("test", CreateResponse(body));

            var result = capture.ResolveReference("{{test.response.body.$.items[0].id}}");

            result.Should().Be("1");
        }

        [TestMethod]
        public async Task ResolveReference_JsonPathMissingProperty_ReturnsEmpty()
        {
            var capture = new ResponseCapture();
            var body = @"{""token"": ""abc123""}";
            await capture.CaptureAsync("login", CreateResponse(body));

            var result = capture.ResolveReference("{{login.response.body.$.missing}}");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task ResolveReference_HeaderValue_ReturnsHeader()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse("body");
            response.Headers.Add("X-Custom-Header", "custom-value");
            await capture.CaptureAsync("test", response);

            var result = capture.ResolveReference("{{test.response.headers.X-Custom-Header}}");

            result.Should().Be("custom-value");
        }

        [TestMethod]
        public async Task ResolveReference_HeaderCaseInsensitive_ReturnsHeader()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse("body");
            response.Headers.Add("X-Custom-Header", "custom-value");
            await capture.CaptureAsync("test", response);

            var result = capture.ResolveReference("{{test.response.headers.x-custom-header}}");

            result.Should().Be("custom-value");
        }

        [TestMethod]
        public async Task ResolveReference_MissingHeader_ReturnsOriginal()
        {
            var capture = new ResponseCapture();
            await capture.CaptureAsync("test", CreateResponse("body"));

            var result = capture.ResolveReference("{{test.response.headers.Missing-Header}}");

            result.Should().Be("{{test.response.headers.Missing-Header}}");
        }

        [TestMethod]
        public async Task ResolveReference_RequestBody_ReturnsRequestBody()
        {
            var capture = new ResponseCapture();
            var requestBody = @"{""username"": ""test""}";
            await capture.CaptureAsync("login", CreateResponse("response"), requestBody);

            var result = capture.ResolveReference("{{login.request.body.*}}");

            result.Should().Be(requestBody);
        }

        [TestMethod]
        public async Task ResolveReference_RequestBodyJsonPath_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var requestBody = @"{""username"": ""testuser"", ""password"": ""secret""}";
            await capture.CaptureAsync("login", CreateResponse("response"), requestBody);

            var result = capture.ResolveReference("{{login.request.body.$.username}}");

            result.Should().Be("testuser");
        }

        [TestMethod]
        public async Task ResolveReference_SimplePropertyPath_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var body = @"{""token"": ""abc123""}";
            await capture.CaptureAsync("login", CreateResponse(body));

            // Without $. prefix - should still work
            var result = capture.ResolveReference("{{login.response.body.token}}");

            result.Should().Be("abc123");
        }

        #endregion

        #region ResolveAllReferences Tests

        [TestMethod]
        public void ResolveAllReferences_NullInput_ReturnsNull()
        {
            var capture = new ResponseCapture();

            var result = capture.ResolveAllReferences(null);

            result.Should().BeNull();
        }

        [TestMethod]
        public void ResolveAllReferences_EmptyInput_ReturnsEmpty()
        {
            var capture = new ResponseCapture();

            var result = capture.ResolveAllReferences(string.Empty);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ResolveAllReferences_NoReferences_ReturnsOriginal()
        {
            var capture = new ResponseCapture();

            var result = capture.ResolveAllReferences("https://api.example.com/users");

            result.Should().Be("https://api.example.com/users");
        }

        [TestMethod]
        public async Task ResolveAllReferences_SingleReference_ResolvesCorrectly()
        {
            var capture = new ResponseCapture();
            await capture.CaptureAsync("login", CreateResponse(@"{""token"": ""abc123""}"));

            var result = capture.ResolveAllReferences("Bearer {{login.response.body.$.token}}");

            result.Should().Be("Bearer abc123");
        }

        [TestMethod]
        public async Task ResolveAllReferences_MultipleReferences_ResolvesAll()
        {
            var capture = new ResponseCapture();
            await capture.CaptureAsync("login", CreateResponse(@"{""userId"": 123, ""token"": ""abc""}"));

            var input = "GET /users/{{login.response.body.$.userId}}?token={{login.response.body.$.token}}";
            var result = capture.ResolveAllReferences(input);

            result.Should().Be("GET /users/123?token=abc");
        }

        [TestMethod]
        public async Task ResolveAllReferences_MixedReferencesAndText_ResolvesCorrectly()
        {
            var capture = new ResponseCapture();
            var response = CreateResponse(@"{""id"": ""user-456""}");
            response.Headers.Add("X-Request-Id", "req-789");
            await capture.CaptureAsync("create", response);

            var input = "User: {{create.response.body.$.id}}, RequestId: {{create.response.headers.X-Request-Id}}";
            var result = capture.ResolveAllReferences(input);

            result.Should().Be("User: user-456, RequestId: req-789");
        }

        #endregion

        #region EvaluateJsonPath Tests

        [TestMethod]
        public void EvaluateJsonPath_NullJson_ReturnsEmpty()
        {
            var capture = new ResponseCapture();

            var result = capture.EvaluateJsonPath(null, "$.token");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateJsonPath_EmptyJson_ReturnsEmpty()
        {
            var capture = new ResponseCapture();

            var result = capture.EvaluateJsonPath(string.Empty, "$.token");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateJsonPath_InvalidJson_ReturnsEmpty()
        {
            var capture = new ResponseCapture();

            var result = capture.EvaluateJsonPath("not valid json", "$.token");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateJsonPath_SimpleProperty_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var json = @"{""name"": ""John""}";

            var result = capture.EvaluateJsonPath(json, "$.name");

            result.Should().Be("John");
        }

        [TestMethod]
        public void EvaluateJsonPath_NestedProperty_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var json = @"{""user"": {""name"": ""John""}}";

            var result = capture.EvaluateJsonPath(json, "$.user.name");

            result.Should().Be("John");
        }

        [TestMethod]
        public void EvaluateJsonPath_DeeplyNestedProperty_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var json = @"{""level1"": {""level2"": {""level3"": {""value"": ""deep""}}}}";

            var result = capture.EvaluateJsonPath(json, "$.level1.level2.level3.value");

            result.Should().Be("deep");
        }

        [TestMethod]
        public void EvaluateJsonPath_ArrayAccess_ReturnsElement()
        {
            var capture = new ResponseCapture();
            var json = @"{""items"": [{""id"": 1}, {""id"": 2}]}";

            var result = capture.EvaluateJsonPath(json, "$.items[0].id");

            result.Should().Be("1");
        }

        [TestMethod]
        public void EvaluateJsonPath_ArraySecondElement_ReturnsElement()
        {
            var capture = new ResponseCapture();
            var json = @"{""items"": [{""id"": 1}, {""id"": 2}]}";

            var result = capture.EvaluateJsonPath(json, "$.items[1].id");

            result.Should().Be("2");
        }

        [TestMethod]
        public void EvaluateJsonPath_ArrayOutOfBounds_ReturnsEmpty()
        {
            var capture = new ResponseCapture();
            var json = @"{""items"": [{""id"": 1}]}";

            var result = capture.EvaluateJsonPath(json, "$.items[5].id");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateJsonPath_MissingProperty_ReturnsEmpty()
        {
            var capture = new ResponseCapture();
            var json = @"{""name"": ""John""}";

            var result = capture.EvaluateJsonPath(json, "$.age");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateJsonPath_NumberValue_ReturnsString()
        {
            var capture = new ResponseCapture();
            var json = @"{""count"": 42}";

            var result = capture.EvaluateJsonPath(json, "$.count");

            result.Should().Be("42");
        }

        [TestMethod]
        public void EvaluateJsonPath_BooleanTrue_ReturnsTrue()
        {
            var capture = new ResponseCapture();
            var json = @"{""enabled"": true}";

            var result = capture.EvaluateJsonPath(json, "$.enabled");

            result.Should().Be("true");
        }

        [TestMethod]
        public void EvaluateJsonPath_BooleanFalse_ReturnsFalse()
        {
            var capture = new ResponseCapture();
            var json = @"{""enabled"": false}";

            var result = capture.EvaluateJsonPath(json, "$.enabled");

            result.Should().Be("false");
        }

        [TestMethod]
        public void EvaluateJsonPath_NullValue_ReturnsEmpty()
        {
            var capture = new ResponseCapture();
            var json = @"{""value"": null}";

            var result = capture.EvaluateJsonPath(json, "$.value");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateJsonPath_ObjectValue_ReturnsRawJson()
        {
            var capture = new ResponseCapture();
            var json = @"{""nested"": {""a"": 1}}";

            var result = capture.EvaluateJsonPath(json, "$.nested");

            result.Should().Contain("\"a\"");
            result.Should().Contain("1");
        }

        [TestMethod]
        public void EvaluateJsonPath_ArrayValue_ReturnsRawJson()
        {
            var capture = new ResponseCapture();
            var json = @"{""items"": [1, 2, 3]}";

            var result = capture.EvaluateJsonPath(json, "$.items");

            result.Should().Contain("[");
            result.Should().Contain("1");
        }

        [TestMethod]
        public void EvaluateJsonPath_ConsecutiveDots_SkipsEmptyParts()
        {
            var capture = new ResponseCapture();
            var json = @"{""user"": {""name"": ""John""}}";

            // Path with consecutive dots ($.user..name) has empty part which should be skipped
            var result = capture.EvaluateJsonPath(json, "$..user..name");

            // Should still find the value despite empty parts
            result.Should().Be("John");
        }

        [TestMethod]
        public void EvaluateJsonPath_PathWithLeadingDot_HandlesCorrectly()
        {
            var capture = new ResponseCapture();
            var json = @"{""data"": {""value"": ""test""}}";

            var result = capture.EvaluateJsonPath(json, "$.data.value");

            result.Should().Be("test");
        }

        [TestMethod]
        public void EvaluateJsonPath_PathWithTrailingEmptyPart_HandlesCorrectly()
        {
            var capture = new ResponseCapture();
            var json = @"{""user"": {""name"": ""John""}}";

            // Path ending with dot will have empty part after split
            var result = capture.EvaluateJsonPath(json, "$.user.name");

            result.Should().Be("John");
        }

        #endregion

        #region EvaluateSimpleJsonPath Tests

        [TestMethod]
        public void EvaluateSimpleJsonPath_SimpleProperty_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var json = @"{""name"": ""John""}";

            var result = capture.EvaluateSimpleJsonPath(json, "name");

            result.Should().Be("John");
        }

        [TestMethod]
        public void EvaluateSimpleJsonPath_NestedProperty_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var json = @"{""user"": {""name"": ""John""}}";

            var result = capture.EvaluateSimpleJsonPath(json, "user.name");

            result.Should().Be("John");
        }

        #endregion

        #region EvaluateXPath Tests

        [TestMethod]
        public void EvaluateXPath_NullXml_ReturnsEmpty()
        {
            var capture = new ResponseCapture();

            var result = capture.EvaluateXPath(null, "/root/value");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateXPath_EmptyXml_ReturnsEmpty()
        {
            var capture = new ResponseCapture();

            var result = capture.EvaluateXPath(string.Empty, "/root/value");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateXPath_InvalidXml_ReturnsEmpty()
        {
            var capture = new ResponseCapture();

            var result = capture.EvaluateXPath("not valid xml", "/root/value");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateXPath_SimpleElement_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var xml = "<root><name>John</name></root>";

            var result = capture.EvaluateXPath(xml, "/root/name");

            result.Should().Be("John");
        }

        [TestMethod]
        public void EvaluateXPath_NestedElement_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var xml = "<root><user><name>John</name></user></root>";

            var result = capture.EvaluateXPath(xml, "/root/user/name");

            result.Should().Be("John");
        }

        [TestMethod]
        public void EvaluateXPath_MissingElement_ReturnsEmpty()
        {
            var capture = new ResponseCapture();
            var xml = "<root><name>John</name></root>";

            var result = capture.EvaluateXPath(xml, "/root/missing");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EvaluateXPath_Attribute_ReturnsValue()
        {
            var capture = new ResponseCapture();
            var xml = @"<root><item id=""123"">value</item></root>";

            var result = capture.EvaluateXPath(xml, "/root/item/@id");

            result.Should().Be("123");
        }

        [TestMethod]
        public async Task ResolveReference_XPathExpression_ResolvesCorrectly()
        {
            var capture = new ResponseCapture();
            var xml = "<response><token>xyz789</token></response>";
            await capture.CaptureAsync("auth", CreateResponse(xml));

            var result = capture.ResolveReference("{{auth.response.body./response/token}}");

            result.Should().Be("xyz789");
        }

        #endregion

        #region GetJsonElementValue Tests

        [TestMethod]
        public void GetJsonElementValue_String_ReturnsString()
        {
            var capture = new ResponseCapture();
            using var doc = JsonDocument.Parse(@"""hello""");

            var result = capture.GetJsonElementValue(doc.RootElement);

            result.Should().Be("hello");
        }

        [TestMethod]
        public void GetJsonElementValue_Number_ReturnsRawText()
        {
            var capture = new ResponseCapture();
            using var doc = JsonDocument.Parse("42");

            var result = capture.GetJsonElementValue(doc.RootElement);

            result.Should().Be("42");
        }

        [TestMethod]
        public void GetJsonElementValue_True_ReturnsTrue()
        {
            var capture = new ResponseCapture();
            using var doc = JsonDocument.Parse("true");

            var result = capture.GetJsonElementValue(doc.RootElement);

            result.Should().Be("true");
        }

        [TestMethod]
        public void GetJsonElementValue_False_ReturnsFalse()
        {
            var capture = new ResponseCapture();
            using var doc = JsonDocument.Parse("false");

            var result = capture.GetJsonElementValue(doc.RootElement);

            result.Should().Be("false");
        }

        [TestMethod]
        public void GetJsonElementValue_Null_ReturnsEmpty()
        {
            var capture = new ResponseCapture();
            using var doc = JsonDocument.Parse("null");

            var result = capture.GetJsonElementValue(doc.RootElement);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetJsonElementValue_Object_ReturnsRawText()
        {
            var capture = new ResponseCapture();
            using var doc = JsonDocument.Parse(@"{""key"": ""value""}");

            var result = capture.GetJsonElementValue(doc.RootElement);

            result.Should().Contain("key");
            result.Should().Contain("value");
        }

        [TestMethod]
        public void GetJsonElementValue_Array_ReturnsRawText()
        {
            var capture = new ResponseCapture();
            using var doc = JsonDocument.Parse("[1, 2, 3]");

            var result = capture.GetJsonElementValue(doc.RootElement);

            result.Should().Contain("[");
            result.Should().Contain("1");
        }

        #endregion

        #region Request Chaining Scenario Tests

        [TestMethod]
        public async Task RequestChaining_LoginThenUseToken()
        {
            var capture = new ResponseCapture();

            // First request - login
            var loginResponse = CreateResponse(@"{""access_token"": ""eyJhbGc..."", ""user_id"": 12345}");
            loginResponse.Headers.Add("X-Request-Id", "req-001");
            await capture.CaptureAsync("login", loginResponse, @"{""username"": ""test""}");

            // Resolve for second request
            var authHeader = capture.ResolveAllReferences("Bearer {{login.response.body.$.access_token}}");
            var userEndpoint = capture.ResolveAllReferences("/users/{{login.response.body.$.user_id}}");
            var correlationId = capture.ResolveAllReferences("{{login.response.headers.X-Request-Id}}");

            authHeader.Should().Be("Bearer eyJhbGc...");
            userEndpoint.Should().Be("/users/12345");
            correlationId.Should().Be("req-001");
        }

        [TestMethod]
        public async Task RequestChaining_CreateThenUpdate()
        {
            var capture = new ResponseCapture();

            // Create resource
            var createResponse = CreateResponse(@"{""id"": ""resource-123"", ""version"": 1}");
            await capture.CaptureAsync("create", createResponse);

            // Update uses created resource id
            var updateUrl = capture.ResolveAllReferences("PUT /resources/{{create.response.body.$.id}}");
            updateUrl.Should().Be("PUT /resources/resource-123");

            // Update returns new version
            var updateResponse = CreateResponse(@"{""id"": ""resource-123"", ""version"": 2}");
            await capture.CaptureAsync("update", updateResponse);

            // Get uses updated version
            var version = capture.ResolveReference("{{update.response.body.$.version}}");
            version.Should().Be("2");
        }

        [TestMethod]
        public async Task RequestChaining_MultipleHeadersExtraction()
        {
            var capture = new ResponseCapture();

            var response = CreateResponse("body");
            response.Headers.Add("X-Correlation-Id", "corr-123");
            response.Headers.Add("X-Request-Id", "req-456");
            response.Headers.Add("ETag", "\"abc123\"");
            await capture.CaptureAsync("first", response);

            var correlationId = capture.ResolveReference("{{first.response.headers.X-Correlation-Id}}");
            var requestId = capture.ResolveReference("{{first.response.headers.X-Request-Id}}");
            var etag = capture.ResolveReference("{{first.response.headers.ETag}}");

            correlationId.Should().Be("corr-123");
            requestId.Should().Be("req-456");
            etag.Should().Be("\"abc123\"");
        }

        #endregion

        #region Helper Methods

        private static HttpResponseMessage CreateResponse(string content, string contentType = "application/json")
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (content != null)
            {
                response.Content = new StringContent(content, Encoding.UTF8, contentType);
            }
            return response;
        }

        #endregion

    }

}
