using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Breakdance.DotHttp;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="DotHttpAssertions"/> class.
    /// </summary>
    [TestClass]
    public class DotHttpAssertionsTests
    {

        #region AssertValidResponseAsync Tests

        [TestMethod]
        public async Task AssertValidResponseAsync_SuccessStatusCode_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""data"": ""value""}");

            await DotHttpAssertions.AssertValidResponseAsync(response);
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_FailureStatusCode_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.InternalServerError, "Server error");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*500*InternalServerError*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_NotFound_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.NotFound, "Not found");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*404*NotFound*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_CheckStatusCodeDisabled_IgnoresFailure()
        {
            var response = CreateResponse(HttpStatusCode.InternalServerError, "Server error");

            await DotHttpAssertions.AssertValidResponseAsync(response, checkStatusCode: false);
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_ContentWithoutContentType_ThrowsException()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            // Create content without Content-Type
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes("test content"));
            content.Headers.ContentLength = 12;
            content.Headers.ContentType = null;
            response.Content = content;

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*no Content-Type header*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_CheckContentTypeDisabled_IgnoresMissingContentType()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var content = new ByteArrayContent(Encoding.UTF8.GetBytes("test content"));
            content.Headers.ContentLength = 12;
            content.Headers.ContentType = null;
            response.Content = content;

            await DotHttpAssertions.AssertValidResponseAsync(response, checkContentType: false);
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_ErrorPatternInBody_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""error"": ""Something went wrong""}");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*error*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_SuccessFalse_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""success"":false, ""message"": ""Failed""}");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*Success flag is false*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_SuccessFalseWithSpace_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""success"": false}");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*Success flag is false*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_CheckBodyForErrorsDisabled_IgnoresErrors()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""error"": ""Something went wrong""}");

            await DotHttpAssertions.AssertValidResponseAsync(response, checkBodyForErrors: false);
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_ErrorsArray_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""errors"": [{""message"": ""Invalid input""}]}");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*errors array*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_StatusError_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""status"":""error"", ""code"": 123}");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*Status field indicates error*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_XmlError_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, "<response><error>Something failed</error></response>", "application/xml");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*XML error element*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_FaultField_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""fault"": {""code"": ""E001""}}");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*fault field*");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_EmptyBody_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, string.Empty);

            await DotHttpAssertions.AssertValidResponseAsync(response);
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_WhitespaceBody_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, "   ");

            await DotHttpAssertions.AssertValidResponseAsync(response);
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_LogResponseOnFailureDisabled_ExcludesBodyPreview()
        {
            var response = CreateResponse(HttpStatusCode.InternalServerError, "Sensitive data here");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response, logResponseOnFailure: false);

            var exception = await act.Should().ThrowAsync<DotHttpAssertionException>();
            exception.Which.Message.Should().NotContain("Sensitive data here");
            exception.Which.Message.Should().NotContain("Body preview:");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_LogResponseOnFailureEnabled_IncludesBodyPreview()
        {
            var response = CreateResponse(HttpStatusCode.InternalServerError, "Error details here");

            Func<Task> act = () => DotHttpAssertions.AssertValidResponseAsync(response, logResponseOnFailure: true);

            var exception = await act.Should().ThrowAsync<DotHttpAssertionException>();
            exception.Which.Message.Should().Contain("Error details here");
            exception.Which.Message.Should().Contain("Body preview:");
        }

        [TestMethod]
        public async Task AssertValidResponseAsync_DefaultParameters_UsesDefaults()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""data"": ""value""}");

            await DotHttpAssertions.AssertValidResponseAsync(response);
        }

        #endregion

        #region AssertStatusCodeAsync Tests

        [TestMethod]
        public async Task AssertStatusCodeAsync_MatchingCode_Passes()
        {
            var response = CreateResponse(HttpStatusCode.Created, @"{""id"": 123}");

            await DotHttpAssertions.AssertStatusCodeAsync(response, 201);
        }

        [TestMethod]
        public async Task AssertStatusCodeAsync_MismatchedCode_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""id"": 123}");

            Func<Task> act = () => DotHttpAssertions.AssertStatusCodeAsync(response, 201);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*Expected status code 201*got 200*");
        }

        [TestMethod]
        public async Task AssertStatusCodeAsync_NoContent_Passes()
        {
            var response = new HttpResponseMessage(HttpStatusCode.NoContent);

            await DotHttpAssertions.AssertStatusCodeAsync(response, 204);
        }

        [TestMethod]
        public async Task AssertStatusCodeAsync_IncludesBodyPreview()
        {
            var response = CreateResponse(HttpStatusCode.BadRequest, @"{""message"": ""Invalid request body""}");

            Func<Task> act = () => DotHttpAssertions.AssertStatusCodeAsync(response, 200);

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*Invalid request body*");
        }

        #endregion

        #region AssertContentType Tests

        [TestMethod]
        public void AssertContentType_MatchingContentType_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""data"": ""value""}", "application/json");

            DotHttpAssertions.AssertContentType(response, "application/json");
        }

        [TestMethod]
        public void AssertContentType_MismatchedContentType_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, "<data>value</data>", "text/xml");

            Action act = () => DotHttpAssertions.AssertContentType(response, "application/json");

            act.Should().Throw<DotHttpAssertionException>()
                .WithMessage("*Expected Content-Type 'application/json'*got 'text/xml'*");
        }

        [TestMethod]
        public void AssertContentType_MissingContentType_ThrowsException()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = null
            };

            Action act = () => DotHttpAssertions.AssertContentType(response, "application/json");

            act.Should().Throw<DotHttpAssertionException>()
                .WithMessage("*no Content-Type header was present*");
        }

        [TestMethod]
        public void AssertContentType_CaseInsensitive_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, "data", "Application/JSON");

            DotHttpAssertions.AssertContentType(response, "application/json");
        }

        #endregion

        #region AssertHeader Tests

        [TestMethod]
        public void AssertHeader_PresentHeader_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, "body");
            response.Headers.Add("X-Custom-Header", "custom-value");

            DotHttpAssertions.AssertHeader(response, "X-Custom-Header");
        }

        [TestMethod]
        public void AssertHeader_PresentHeaderWithExpectedValue_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, "body");
            response.Headers.Add("X-Custom-Header", "expected-value");

            DotHttpAssertions.AssertHeader(response, "X-Custom-Header", "expected-value");
        }

        [TestMethod]
        public void AssertHeader_MissingHeader_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, "body");

            Action act = () => DotHttpAssertions.AssertHeader(response, "X-Missing-Header");

            act.Should().Throw<DotHttpAssertionException>()
                .WithMessage("*Expected header 'X-Missing-Header'*not present*");
        }

        [TestMethod]
        public void AssertHeader_WrongValue_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, "body");
            response.Headers.Add("X-Custom-Header", "actual-value");

            Action act = () => DotHttpAssertions.AssertHeader(response, "X-Custom-Header", "expected-value");

            act.Should().Throw<DotHttpAssertionException>()
                .WithMessage("*Expected header 'X-Custom-Header'*value 'expected-value'*got 'actual-value'*");
        }

        [TestMethod]
        public void AssertHeader_ContentHeader_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, "body", "application/json");

            DotHttpAssertions.AssertHeader(response, "Content-Type");
        }

        [TestMethod]
        public void AssertHeader_ContentHeaderWithValue_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, "body", "application/json");

            DotHttpAssertions.AssertHeader(response, "Content-Type", "application/json; charset=utf-8");
        }

        [TestMethod]
        public void AssertHeader_ValueCaseInsensitive_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, "body");
            response.Headers.Add("Cache-Control", "NO-STORE");

            DotHttpAssertions.AssertHeader(response, "Cache-Control", "no-store");
        }

        #endregion

        #region AssertBodyContainsAsync Tests

        [TestMethod]
        public async Task AssertBodyContainsAsync_ContainsText_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""message"": ""Hello, World!""}");

            await DotHttpAssertions.AssertBodyContainsAsync(response, "Hello, World!");
        }

        [TestMethod]
        public async Task AssertBodyContainsAsync_MissingText_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""message"": ""Hello""}");

            Func<Task> act = () => DotHttpAssertions.AssertBodyContainsAsync(response, "Goodbye");

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*Expected response body to contain 'Goodbye'*not found*");
        }

        [TestMethod]
        public async Task AssertBodyContainsAsync_IncludesBodyPreview()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""actual"": ""content""}");

            Func<Task> act = () => DotHttpAssertions.AssertBodyContainsAsync(response, "expected");

            await act.Should().ThrowAsync<DotHttpAssertionException>()
                .WithMessage("*Body preview:*actual*content*");
        }

        #endregion

        #region AssertNoErrorsInBodyAsync Tests

        [TestMethod]
        public async Task AssertNoErrorsInBodyAsync_NoErrors_Passes()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""data"": ""value"", ""success"": true}");

            await DotHttpAssertions.AssertNoErrorsInBodyAsync(response);
        }

        [TestMethod]
        public async Task AssertNoErrorsInBodyAsync_HasError_ThrowsException()
        {
            var response = CreateResponse(HttpStatusCode.OK, @"{""error"": ""Something went wrong""}");

            Func<Task> act = () => DotHttpAssertions.AssertNoErrorsInBodyAsync(response);

            await act.Should().ThrowAsync<DotHttpAssertionException>();
        }

        #endregion

        #region CheckForErrorPatterns Tests

        [TestMethod]
        public void CheckForErrorPatterns_NullBody_DoesNotThrow()
        {
            Action act = () => DotHttpAssertions.CheckForErrorPatterns(null, HttpStatusCode.OK, DotHttpAssertions.DefaultMaxBodyPreviewLength);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void CheckForErrorPatterns_EmptyBody_DoesNotThrow()
        {
            Action act = () => DotHttpAssertions.CheckForErrorPatterns(string.Empty, HttpStatusCode.OK, DotHttpAssertions.DefaultMaxBodyPreviewLength);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void CheckForErrorPatterns_WhitespaceBody_DoesNotThrow()
        {
            Action act = () => DotHttpAssertions.CheckForErrorPatterns("   ", HttpStatusCode.OK, DotHttpAssertions.DefaultMaxBodyPreviewLength);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void CheckForErrorPatterns_CleanBody_DoesNotThrow()
        {
            Action act = () => DotHttpAssertions.CheckForErrorPatterns(
                @"{""data"": ""value""}", HttpStatusCode.OK, DotHttpAssertions.DefaultMaxBodyPreviewLength);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void CheckForErrorPatterns_ErrorField_Throws()
        {
            Action act = () => DotHttpAssertions.CheckForErrorPatterns(
                @"{""error"": ""test""}", HttpStatusCode.OK, DotHttpAssertions.DefaultMaxBodyPreviewLength);

            act.Should().Throw<DotHttpAssertionException>();
        }

        [TestMethod]
        public void CheckForErrorPatterns_XmlErrorUppercase_Throws()
        {
            Action act = () => DotHttpAssertions.CheckForErrorPatterns(
                "<Error>Test</Error>", HttpStatusCode.OK, DotHttpAssertions.DefaultMaxBodyPreviewLength);

            act.Should().Throw<DotHttpAssertionException>();
        }

        [TestMethod]
        public void CheckForErrorPatterns_FaultUppercase_Throws()
        {
            Action act = () => DotHttpAssertions.CheckForErrorPatterns(
                @"{""Fault"": {}}", HttpStatusCode.OK, DotHttpAssertions.DefaultMaxBodyPreviewLength);

            act.Should().Throw<DotHttpAssertionException>();
        }

        #endregion

        #region GetBodyPreviewAsync Tests

        [TestMethod]
        public async Task GetBodyPreviewAsync_DefaultContent_ReturnsEmpty()
        {
            // Note: On modern .NET, HttpResponseMessage initializes Content to EmptyContent (not null).
            // On .NET Framework 4.8, HttpResponseMessage.Content is null by default.
            // GetBodyPreviewAsync handles both cases and returns "(empty)" for either.
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var result = await DotHttpAssertions.GetBodyPreviewAsync(response, 500);

            result.Should().Be("(empty)");
        }

        [TestMethod]
        public async Task GetBodyPreviewAsync_EmptyStringContent_ReturnsEmpty()
        {
            // Content is set but empty - ReadAsStringAsync returns "" which Truncate converts to "(empty)"
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            };

            var result = await DotHttpAssertions.GetBodyPreviewAsync(response, 500);

            result.Should().Be("(empty)");
        }

        [TestMethod]
        public async Task GetBodyPreviewAsync_ShortBody_ReturnsFullBody()
        {
            var response = CreateResponse(HttpStatusCode.OK, "short body");

            var result = await DotHttpAssertions.GetBodyPreviewAsync(response, 500);

            result.Should().Be("short body");
        }

        [TestMethod]
        public async Task GetBodyPreviewAsync_LongBody_TruncatesWithEllipsis()
        {
            var longBody = new string('x', 600);
            var response = CreateResponse(HttpStatusCode.OK, longBody);

            var result = await DotHttpAssertions.GetBodyPreviewAsync(response, 100);

            result.Should().HaveLength(103);
            result.Should().EndWith("...");
        }

        [TestMethod]
        public async Task GetBodyPreviewAsync_ContentThrowsOnRead_ReturnsUnableToReadBody()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ThrowingHttpContent()
            };

            var result = await DotHttpAssertions.GetBodyPreviewAsync(response, 500);

            result.Should().Be("(unable to read body)");
        }

        #endregion

        #region Truncate Tests

        [TestMethod]
        public void Truncate_NullValue_ReturnsEmpty()
        {
            var result = DotHttpAssertions.Truncate(null, 100);

            result.Should().Be("(empty)");
        }

        [TestMethod]
        public void Truncate_EmptyValue_ReturnsEmpty()
        {
            var result = DotHttpAssertions.Truncate(string.Empty, 100);

            result.Should().Be("(empty)");
        }

        [TestMethod]
        public void Truncate_ShortValue_ReturnsFull()
        {
            var result = DotHttpAssertions.Truncate("short", 100);

            result.Should().Be("short");
        }

        [TestMethod]
        public void Truncate_ExactLength_ReturnsFull()
        {
            var result = DotHttpAssertions.Truncate("12345", 5);

            result.Should().Be("12345");
        }

        [TestMethod]
        public void Truncate_LongValue_TruncatesWithEllipsis()
        {
            var result = DotHttpAssertions.Truncate("1234567890", 5);

            result.Should().Be("12345...");
        }

        #endregion

        #region DotHttpAssertionException Tests

        [TestMethod]
        public void DotHttpAssertionException_DefaultConstructor_CreatesException()
        {
            var exception = new DotHttpAssertionException();

            exception.Should().NotBeNull();
            exception.Message.Should().NotBeNullOrWhiteSpace();
        }

        [TestMethod]
        public void DotHttpAssertionException_MessageConstructor_SetsMessage()
        {
            var exception = new DotHttpAssertionException("Test error message");

            exception.Message.Should().Be("Test error message");
        }

        [TestMethod]
        public void DotHttpAssertionException_MessageAndInnerException_SetsBoth()
        {
            var inner = new InvalidOperationException("Inner");
            var exception = new DotHttpAssertionException("Outer message", inner);

            exception.Message.Should().Be("Outer message");
            exception.InnerException.Should().BeSameAs(inner);
        }

        [TestMethod]
        public void DotHttpAssertionException_IsException()
        {
            var exception = new DotHttpAssertionException("Test");

            exception.Should().BeAssignableTo<Exception>();
        }

        #endregion

        #region Helper Methods

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content, string contentType = "application/json")
        {
            var response = new HttpResponseMessage(statusCode);
            if (content != null)
            {
                response.Content = new StringContent(content, Encoding.UTF8, contentType);
            }
            return response;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// HttpContent that throws an exception when read, for testing exception handling paths.
        /// </summary>
        private class ThrowingHttpContent : HttpContent
        {

            protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context)
            {
                throw new IOException("Simulated read failure");
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }

        }

        #endregion

    }

}
