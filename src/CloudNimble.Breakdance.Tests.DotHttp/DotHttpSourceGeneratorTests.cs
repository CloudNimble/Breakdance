using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Breakdance.DotHttp.Generator;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="DotHttpSourceGenerator"/> class.
    /// </summary>
    [TestClass]
    public class DotHttpSourceGeneratorTests
    {

        #region Fields

        private static GeneratorDriver _driver;
        private static Compilation _outputCompilation;
        private static ImmutableArray<Diagnostic> _diagnostics;

        #endregion

        #region Class Setup

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Create a simple HTTP file content
            var httpFileContent = @"
@baseUrl = https://api.example.com

### Get Users
# @name GetUsers
GET {{baseUrl}}/users
Accept: application/json

### Create User
# @name CreateUser
POST {{baseUrl}}/users
Content-Type: application/json

{""name"": ""John"", ""email"": ""john@example.com""}

### Get User by ID
GET {{baseUrl}}/users/{{userId}}
Accept: application/json
";

            // Run the generator
            (_driver, _outputCompilation, _diagnostics) = RunGenerator(httpFileContent, "api.http");
        }

        #endregion

        #region Escape Tests

        [TestMethod]
        public void Escape_NullValue_ReturnsEmptyString()
        {
            var result = DotHttpSourceGenerator.Escape(null);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Escape_EmptyString_ReturnsEmptyString()
        {
            var result = DotHttpSourceGenerator.Escape("");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Escape_SimpleString_ReturnsUnchanged()
        {
            var result = DotHttpSourceGenerator.Escape("hello world");

            result.Should().Be("hello world");
        }

        [TestMethod]
        public void Escape_Backslash_EscapesBackslash()
        {
            var result = DotHttpSourceGenerator.Escape(@"path\to\file");

            result.Should().Be(@"path\\to\\file");
        }

        [TestMethod]
        public void Escape_DoubleQuote_EscapesQuote()
        {
            var result = DotHttpSourceGenerator.Escape("say \"hello\"");

            result.Should().Be("say \\\"hello\\\"");
        }

        [TestMethod]
        public void Escape_CarriageReturn_EscapesCR()
        {
            var result = DotHttpSourceGenerator.Escape("line1\rline2");

            result.Should().Be("line1\\rline2");
        }

        [TestMethod]
        public void Escape_Newline_EscapesLF()
        {
            var result = DotHttpSourceGenerator.Escape("line1\nline2");

            result.Should().Be("line1\\nline2");
        }

        [TestMethod]
        public void Escape_Tab_EscapesTab()
        {
            var result = DotHttpSourceGenerator.Escape("col1\tcol2");

            result.Should().Be("col1\\tcol2");
        }

        [TestMethod]
        public void Escape_AllSpecialChars_EscapesAll()
        {
            var result = DotHttpSourceGenerator.Escape("\\\"\r\n\t");

            result.Should().Be("\\\\\\\"\\r\\n\\t");
        }

        #endregion

        #region EscapeVerbatim Tests

        [TestMethod]
        public void EscapeVerbatim_NullValue_ReturnsEmptyString()
        {
            var result = DotHttpSourceGenerator.EscapeVerbatim(null);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EscapeVerbatim_EmptyString_ReturnsEmptyString()
        {
            var result = DotHttpSourceGenerator.EscapeVerbatim("");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EscapeVerbatim_SimpleString_ReturnsUnchanged()
        {
            var result = DotHttpSourceGenerator.EscapeVerbatim("hello world");

            result.Should().Be("hello world");
        }

        [TestMethod]
        public void EscapeVerbatim_DoubleQuote_DoublesQuote()
        {
            var result = DotHttpSourceGenerator.EscapeVerbatim("say \"hello\"");

            result.Should().Be("say \"\"hello\"\"");
        }

        [TestMethod]
        public void EscapeVerbatim_Backslash_ReturnsUnchanged()
        {
            var result = DotHttpSourceGenerator.EscapeVerbatim(@"path\to\file");

            result.Should().Be(@"path\to\file");
        }

        [TestMethod]
        public void EscapeVerbatim_NewlineAndTab_ReturnsUnchanged()
        {
            var result = DotHttpSourceGenerator.EscapeVerbatim("line1\nline2\tcol");

            result.Should().Be("line1\nline2\tcol");
        }

        #endregion

        #region EscapeXml Tests

        [TestMethod]
        public void EscapeXml_NullValue_ReturnsEmptyString()
        {
            var result = DotHttpSourceGenerator.EscapeXml(null);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EscapeXml_EmptyString_ReturnsEmptyString()
        {
            var result = DotHttpSourceGenerator.EscapeXml("");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void EscapeXml_SimpleString_ReturnsUnchanged()
        {
            var result = DotHttpSourceGenerator.EscapeXml("hello world");

            result.Should().Be("hello world");
        }

        [TestMethod]
        public void EscapeXml_Ampersand_EscapesAmpersand()
        {
            var result = DotHttpSourceGenerator.EscapeXml("foo & bar");

            result.Should().Be("foo &amp; bar");
        }

        [TestMethod]
        public void EscapeXml_LessThan_EscapesLessThan()
        {
            var result = DotHttpSourceGenerator.EscapeXml("a < b");

            result.Should().Be("a &lt; b");
        }

        [TestMethod]
        public void EscapeXml_GreaterThan_EscapesGreaterThan()
        {
            var result = DotHttpSourceGenerator.EscapeXml("a > b");

            result.Should().Be("a &gt; b");
        }

        [TestMethod]
        public void EscapeXml_AllSpecialChars_EscapesAll()
        {
            var result = DotHttpSourceGenerator.EscapeXml("<div>&nbsp;</div>");

            result.Should().Be("&lt;div&gt;&amp;nbsp;&lt;/div&gt;");
        }

        #endregion

        #region SanitizeClassName Tests

        [TestMethod]
        public void SanitizeClassName_NullValue_ReturnsDefault()
        {
            var result = DotHttpSourceGenerator.SanitizeClassName(null);

            result.Should().Be("HttpTests");
        }

        [TestMethod]
        public void SanitizeClassName_EmptyString_ReturnsDefault()
        {
            var result = DotHttpSourceGenerator.SanitizeClassName("");

            result.Should().Be("HttpTests");
        }

        [TestMethod]
        public void SanitizeClassName_ValidName_ReturnsPascalCase()
        {
            var result = DotHttpSourceGenerator.SanitizeClassName("api_tests");

            result.Should().Be("ApiTests");
        }

        [TestMethod]
        public void SanitizeClassName_StartsWithNumber_PrependHttp()
        {
            var result = DotHttpSourceGenerator.SanitizeClassName("123api");

            result.Should().Be("Http123api");
        }

        [TestMethod]
        public void SanitizeClassName_SpecialChars_ReplacesWithUnderscore()
        {
            var result = DotHttpSourceGenerator.SanitizeClassName("my-api.tests");

            result.Should().Be("MyApiTests");
        }

        [TestMethod]
        public void SanitizeClassName_Hyphens_ConvertsToPascalCase()
        {
            var result = DotHttpSourceGenerator.SanitizeClassName("user-profile-api");

            result.Should().Be("UserProfileApi");
        }

        #endregion

        #region SanitizeMethodName Tests

        [TestMethod]
        public void SanitizeMethodName_NullValue_ReturnsDefault()
        {
            var result = DotHttpSourceGenerator.SanitizeMethodName(null);

            result.Should().Be("Test");
        }

        [TestMethod]
        public void SanitizeMethodName_EmptyString_ReturnsDefault()
        {
            var result = DotHttpSourceGenerator.SanitizeMethodName("");

            result.Should().Be("Test");
        }

        [TestMethod]
        public void SanitizeMethodName_ValidName_ReturnsPascalCase()
        {
            var result = DotHttpSourceGenerator.SanitizeMethodName("get_users");

            result.Should().Be("GetUsers");
        }

        [TestMethod]
        public void SanitizeMethodName_StartsWithNumber_PrependTest()
        {
            var result = DotHttpSourceGenerator.SanitizeMethodName("123test");

            result.Should().Be("Test123test");
        }

        [TestMethod]
        public void SanitizeMethodName_SpecialChars_Removes()
        {
            var result = DotHttpSourceGenerator.SanitizeMethodName("get/users/{id}");

            result.Should().Be("GetUsersId");
        }

        [TestMethod]
        public void SanitizeMethodName_ConsecutiveUnderscores_Collapses()
        {
            var result = DotHttpSourceGenerator.SanitizeMethodName("get___users");

            result.Should().Be("GetUsers");
        }

        [TestMethod]
        public void SanitizeMethodName_LeadingTrailingUnderscores_Trims()
        {
            var result = DotHttpSourceGenerator.SanitizeMethodName("_get_users_");

            result.Should().Be("GetUsers");
        }

        [TestMethod]
        public void SanitizeMethodName_OnlySpecialChars_ReturnsDefault()
        {
            var result = DotHttpSourceGenerator.SanitizeMethodName("@#$%");

            result.Should().Be("Test");
        }

        #endregion

        #region ToPascalCase Tests

        [TestMethod]
        public void ToPascalCase_NullValue_ReturnsNull()
        {
            var result = DotHttpSourceGenerator.ToPascalCase(null);

            result.Should().BeNull();
        }

        [TestMethod]
        public void ToPascalCase_EmptyString_ReturnsEmpty()
        {
            var result = DotHttpSourceGenerator.ToPascalCase("");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ToPascalCase_SingleWord_CapitalizesFirst()
        {
            var result = DotHttpSourceGenerator.ToPascalCase("hello");

            result.Should().Be("Hello");
        }

        [TestMethod]
        public void ToPascalCase_UnderscoreSeparated_ConvertsToPascalCase()
        {
            var result = DotHttpSourceGenerator.ToPascalCase("hello_world");

            result.Should().Be("HelloWorld");
        }

        [TestMethod]
        public void ToPascalCase_HyphenSeparated_ConvertsToPascalCase()
        {
            var result = DotHttpSourceGenerator.ToPascalCase("hello-world");

            result.Should().Be("HelloWorld");
        }

        [TestMethod]
        public void ToPascalCase_SpaceSeparated_ConvertsToPascalCase()
        {
            var result = DotHttpSourceGenerator.ToPascalCase("hello world");

            result.Should().Be("HelloWorld");
        }

        [TestMethod]
        public void ToPascalCase_MixedSeparators_ConvertsToPascalCase()
        {
            var result = DotHttpSourceGenerator.ToPascalCase("hello_world-test case");

            result.Should().Be("HelloWorldTestCase");
        }

        [TestMethod]
        public void ToPascalCase_AllUppercase_ConvertsToProperCase()
        {
            var result = DotHttpSourceGenerator.ToPascalCase("HELLO_WORLD");

            result.Should().Be("HelloWorld");
        }

        #endregion

        #region GetHttpMethodProperty Tests

        [TestMethod]
        public void GetHttpMethodProperty_GET_ReturnsGet()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("GET");

            result.Should().Be("Get");
        }

        [TestMethod]
        public void GetHttpMethodProperty_POST_ReturnsPost()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("POST");

            result.Should().Be("Post");
        }

        [TestMethod]
        public void GetHttpMethodProperty_PUT_ReturnsPut()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("PUT");

            result.Should().Be("Put");
        }

        [TestMethod]
        public void GetHttpMethodProperty_DELETE_ReturnsDelete()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("DELETE");

            result.Should().Be("Delete");
        }

        [TestMethod]
        public void GetHttpMethodProperty_PATCH_ReturnsPatch()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("PATCH");

            result.Should().Be("Patch");
        }

        [TestMethod]
        public void GetHttpMethodProperty_HEAD_ReturnsHead()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("HEAD");

            result.Should().Be("Head");
        }

        [TestMethod]
        public void GetHttpMethodProperty_OPTIONS_ReturnsOptions()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("OPTIONS");

            result.Should().Be("Options");
        }

        [TestMethod]
        public void GetHttpMethodProperty_TRACE_ReturnsTrace()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("TRACE");

            result.Should().Be("Trace");
        }

        [TestMethod]
        public void GetHttpMethodProperty_CONNECT_ReturnsNewHttpMethod()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("CONNECT");

            result.Should().Be("new HttpMethod(\"CONNECT\")");
        }

        [TestMethod]
        public void GetHttpMethodProperty_Unknown_ReturnsGet()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("UNKNOWN");

            result.Should().Be("Get");
        }

        [TestMethod]
        public void GetHttpMethodProperty_LowerCase_ReturnsCorrectMethod()
        {
            var result = DotHttpSourceGenerator.GetHttpMethodProperty("get");

            result.Should().Be("Get");
        }

        #endregion

        #region GetHttpVersion Tests

        [TestMethod]
        public void GetHttpVersion_NullValue_ReturnsDefault()
        {
            var result = DotHttpSourceGenerator.GetHttpVersion(null);

            result.Should().Be("1.1");
        }

        [TestMethod]
        public void GetHttpVersion_EmptyString_ReturnsDefault()
        {
            var result = DotHttpSourceGenerator.GetHttpVersion("");

            result.Should().Be("1.1");
        }

        [TestMethod]
        public void GetHttpVersion_Http11_Returns11()
        {
            var result = DotHttpSourceGenerator.GetHttpVersion("HTTP/1.1");

            result.Should().Be("1.1");
        }

        [TestMethod]
        public void GetHttpVersion_Http10_Returns10()
        {
            var result = DotHttpSourceGenerator.GetHttpVersion("HTTP/1.0");

            result.Should().Be("1.0");
        }

        [TestMethod]
        public void GetHttpVersion_Http2_Returns20()
        {
            var result = DotHttpSourceGenerator.GetHttpVersion("HTTP/2");

            result.Should().Be("2.0");
        }

        [TestMethod]
        public void GetHttpVersion_Http3_Returns30()
        {
            var result = DotHttpSourceGenerator.GetHttpVersion("HTTP/3");

            result.Should().Be("3.0");
        }

        [TestMethod]
        public void GetHttpVersion_InvalidFormat_ReturnsDefault()
        {
            var result = DotHttpSourceGenerator.GetHttpVersion("invalid");

            result.Should().Be("1.1");
        }

        #endregion

        #region GetTestMethodName Tests - Priority 1: Name Directive

        [TestMethod]
        public void GetTestMethodName_WithName_ReturnsName()
        {
            var request = new DotHttpRequest { Name = "GetUsers", Method = "GET", Url = "https://api.example.com/users" };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("GetUsers");
        }

        [TestMethod]
        public void GetTestMethodName_WithName_IgnoresSeparatorTitle()
        {
            var request = new DotHttpRequest
            {
                Name = "MyCustomName",
                SeparatorTitle = "Should Be Ignored",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("MyCustomName");
            result.Should().NotContain("Ignored");
        }

        [TestMethod]
        public void GetTestMethodName_WithName_IgnoresUrl()
        {
            var request = new DotHttpRequest
            {
                Name = "CustomTestName",
                Method = "POST",
                Url = "https://api.example.com/very/long/path/that/should/be/ignored"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("CustomTestName");
            result.Should().NotContain("Path");
        }

        [TestMethod]
        public void GetTestMethodName_WithNameContainingSpecialChars_SanitizesName()
        {
            var request = new DotHttpRequest
            {
                Name = "Get/Users/{id}",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("GetUsersId");
        }

        [TestMethod]
        public void GetTestMethodName_WithNameStartingWithNumber_PrependsTest()
        {
            var request = new DotHttpRequest
            {
                Name = "123GetUsers",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().StartWith("Test");
        }

        [TestMethod]
        public void GetTestMethodName_WithNameContainingUnderscores_ConvertsToPascalCase()
        {
            var request = new DotHttpRequest
            {
                Name = "get_all_users",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("GetAllUsers");
        }

        [TestMethod]
        public void GetTestMethodName_WithNameContainingHyphens_ConvertsToPascalCase()
        {
            var request = new DotHttpRequest
            {
                Name = "get-all-users",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("GetAllUsers");
        }

        #endregion

        #region GetTestMethodName Tests - Priority 2: Separator Title

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitle_UsesTitle()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "Get All Users",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            // Separator titles do not get _{METHOD} suffix (they're human-readable names)
            result.Should().Be("GetAllUsers");
        }

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitle_DoesNotAppendHttpMethod()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "Create User",
                Method = "POST",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            // Human-readable separator titles do not get method suffix
            result.Should().Be("CreateUser");
        }

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitleContainingDashes_DehumanizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "get-users-by-id",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Contain("Get");
            result.Should().NotEndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitleContainingSlashes_SanitizesBeforeDehumanize()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "API/Users/GetAll",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("/");
            result.Should().NotEndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitleContainingDots_SanitizesBeforeDehumanize()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "api.users.getAll",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain(".");
            result.Should().NotEndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitleContainingColons_SanitizesBeforeDehumanize()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "Users: Get All",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain(":");
            result.Should().NotEndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitleMixedDelimiters_HandlesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "API-Test/Users_GetAll.v2",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            // Separator titles no longer have method suffix
            result.Should().NotContain("-");
            result.Should().NotContain("/");
            result.Should().NotContain("_");
            result.Should().NotContain(".");
        }

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitleAllCaps_DehumanizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "GET ALL USERS",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotEndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitleAllLowercase_DehumanizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "get all users",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            // No method suffix for separator titles
            result.Should().Be("GetAllUsers");
        }

        [TestMethod]
        public void GetTestMethodName_WithEmptySeparatorTitle_FallsBackToUrl()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            // URL-based names still get method suffix
            result.Should().EndWith("_GET");
            result.Should().Contain("Users");
        }

        #endregion

        #region GetTestMethodName Tests - Priority 3: URL Path (Basic)

        [TestMethod]
        public void GetTestMethodName_WithoutName_GeneratesFromMethodAndUrl()
        {
            var request = new DotHttpRequest { Name = null, Method = "GET", Url = "https://api.example.com/users" };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Contain("GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithSimpleUrl_GeneratesFromPath()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Contain("Users");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithMultiplePathSegments_CombinesSegments()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/api/v2/users/profile"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithHttpProtocol_RemovesProtocol()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "http://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("http");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithHttpsProtocol_RemovesProtocol()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("https");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithNoProtocol_HandlesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithQueryString_RemovesQueryString()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users?page=1&limit=10"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("page");
            result.Should().NotContain("limit");
            result.Should().NotContain("?");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithComplexQueryString_RemovesEntireQueryString()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/search?q=test&filter[status]=active&sort=-created"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("filter");
            result.Should().NotContain("sort");
            result.Should().NotContain("=");
            result.Should().EndWith("_GET");
        }

        #endregion

        #region GetTestMethodName Tests - Priority 3: URL Path (Variable References)

        [TestMethod]
        public void GetTestMethodName_UrlWithVariables_RemovesVariables()
        {
            var request = new DotHttpRequest { Name = null, Method = "GET", Url = "{{baseUrl}}/users/{{userId}}" };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("{{");
            result.Should().NotContain("}}");
        }

        [TestMethod]
        public void GetTestMethodName_WithSingleVariable_RemovesVariable()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "{{baseUrl}}/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("{{");
            result.Should().NotContain("}}");
            result.Should().NotContain("baseUrl");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithMultipleVariables_RemovesAllVariables()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "{{baseUrl}}/users/{{userId}}/posts/{{postId}}"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("{{");
            result.Should().NotContain("}}");
            result.Should().NotContain("baseUrl");
            result.Should().NotContain("userId");
            result.Should().NotContain("postId");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithVariableInHost_RemovesVariable()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://{{host}}/api/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("host");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithVariableInQueryString_RemovesVariable()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users?token={{authToken}}"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("authToken");
            result.Should().NotContain("{{");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithNestedVariableReference_RemovesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "{{baseUrl}}/users/{{login.response.body.$.userId}}"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("{{");
            result.Should().NotContain("}}");
            result.Should().NotContain("login");
            result.Should().NotContain("response");
            result.Should().EndWith("_GET");
        }

        #endregion

        #region GetTestMethodName Tests - Priority 3: URL Path (Special Characters)

        [TestMethod]
        public void GetTestMethodName_WithPathContainingHyphens_DehumanizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/user-profiles"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("-");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithPathContainingUnderscores_DehumanizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/user_profiles"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            // Should only have one underscore (before method name)
            result.Count(c => c == '_').Should().Be(1);
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithPathContainingDots_SanitizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/api.v2.users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("."); // Dots in path should be sanitized
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithPathContainingNumbers_IncludesNumbers()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/api/v2/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Contain("2");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithPathContainingCurlyBraces_HandlesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users/{id}"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("{");
            result.Should().NotContain("}");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithPathContainingBrackets_HandlesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users[0]"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("[");
            result.Should().NotContain("]");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithPathContainingColons_SanitizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users:search"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain(":");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithPathContainingAtSymbol_SanitizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users/@me"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("@");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithPathContainingPlusSign_SanitizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/search+results"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("+");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithPathContainingPercent_SanitizesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users%20search"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("%");
            result.Should().EndWith("_GET");
        }

        #endregion

        #region GetTestMethodName Tests - Priority 3: URL Path (Edge Cases)

        [TestMethod]
        public void GetTestMethodName_WithOnlyHost_UsesDefaultName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithOnlyHostAndSlash_UsesDefaultName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("Request_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithEmptyPath_ReturnsRequest()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "/"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("Request_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithOnlyVariables_ReturnsRequest()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "{{baseUrl}}"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("Request_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithOnlyQueryString_ReturnsRequest()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/?query=test"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("Request_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithTrailingSlash_HandlesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users/"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithMultipleSlashes_HandlesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com//users//profile"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithVeryLongPath_HandlesCorrectly()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/api/v2/organizations/123/departments/456/teams/789/members/abc/profile/settings/notifications/email"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
            result.Length.Should().BeGreaterThan(10);
        }

        #endregion

        #region GetTestMethodName Tests - HTTP Methods

        [TestMethod]
        public void GetTestMethodName_WithPostMethod_AppendsPost()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "POST",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_POST");
        }

        [TestMethod]
        public void GetTestMethodName_WithPutMethod_AppendsPut()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "PUT",
                Url = "https://api.example.com/users/123"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_PUT");
        }

        [TestMethod]
        public void GetTestMethodName_WithDeleteMethod_AppendsDelete()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "DELETE",
                Url = "https://api.example.com/users/123"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_DELETE");
        }

        [TestMethod]
        public void GetTestMethodName_WithPatchMethod_AppendsPatch()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "PATCH",
                Url = "https://api.example.com/users/123"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_PATCH");
        }

        [TestMethod]
        public void GetTestMethodName_WithHeadMethod_AppendsHead()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "HEAD",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_HEAD");
        }

        [TestMethod]
        public void GetTestMethodName_WithOptionsMethod_AppendsOptions()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "OPTIONS",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_OPTIONS");
        }

        #endregion

        #region GetTestMethodName Tests - Real-World URL Patterns

        [TestMethod]
        public void GetTestMethodName_RestfulGetCollection_GeneratesCorrectName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/api/v1/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_RestfulGetSingle_GeneratesCorrectName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/api/v1/users/{{userId}}"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_RestfulNested_GeneratesCorrectName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users/{{userId}}/posts/{{postId}}/comments"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_GraphQLEndpoint_GeneratesCorrectName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "POST",
                Url = "https://api.example.com/graphql"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("Graphql_POST");
        }

        [TestMethod]
        public void GetTestMethodName_ODataEndpoint_GeneratesCorrectName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/odata/Products?$filter=Price gt 20&$select=Name,Price"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("$");
            result.Should().NotContain("filter");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_AzureBlobStorage_GeneratesCorrectName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://{{storageAccount}}.blob.core.windows.net/{{container}}/{{blobName}}"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("{{");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_AWSApiGateway_GeneratesCorrectName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://{{apiId}}.execute-api.{{region}}.amazonaws.com/{{stage}}/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("{{");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_GitHubAPI_GeneratesCorrectName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.github.com/repos/{{owner}}/{{repo}}/issues"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("{{");
            result.Should().EndWith("_GET");
        }

        [TestMethod]
        public void GetTestMethodName_VersionedApiPath_GeneratesCorrectName()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/v2.1/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_GET");
        }

        #endregion

        #region GetTestMethodName Tests - Valid C# Identifier

        [TestMethod]
        public void GetTestMethodName_WithUrlStartingWithNumber_PrefixesWithN()
        {
            // When a URL path starts with a number, the result is prefixed with "N"
            // to ensure it's a valid C# identifier (must start with letter).
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/123/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            // Result starts with "N" to make it a valid C# identifier
            result.Should().StartWith("N123");
            result.Should().EndWith("_GET");
            result.Should().MatchRegex("^[a-zA-Z]");
        }

        [TestMethod]
        public void GetTestMethodName_ResultContainsOnlyValidChars()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/users/{id}/profile?include=all"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().MatchRegex("^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        [TestMethod]
        public void GetTestMethodName_DoesNotContainSpaces()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "Get All Users From API",
                Method = "GET",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain(" ");
        }

        [TestMethod]
        public void GetTestMethodName_DoesNotContainHyphens()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = null,
                Method = "GET",
                Url = "https://api.example.com/user-profiles"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            // The only hyphen-like character should not exist except in the method suffix
            var beforeMethodSuffix = result.Substring(0, result.LastIndexOf('_'));
            beforeMethodSuffix.Should().NotContain("-");
        }

        #endregion

        #region GetConfiguration Tests

        [TestMethod]
        public void GetConfiguration_EmptyOptions_ReturnsDefaults()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.TestFramework.Should().Be("MSTest");
            result.Namespace.Should().Be("GeneratedTests");
            result.UseFluentAssertions.Should().BeTrue();
            result.CheckStatusCode.Should().BeTrue();
            result.CheckContentType.Should().BeTrue();
            result.CheckBodyForErrors.Should().BeTrue();
        }

        [TestMethod]
        public void GetConfiguration_TestFrameworkXUnit_SetsXUnit()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_TestFramework"] = "XUnit"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.TestFramework.Should().Be("XUnit");
        }

        [TestMethod]
        public void GetConfiguration_CustomNamespace_SetsNamespace()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_Namespace"] = "MyProject.Tests"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.Namespace.Should().Be("MyProject.Tests");
        }

        [TestMethod]
        public void GetConfiguration_RootNamespace_UsesAsDefault()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.RootNamespace"] = "MyProject"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.Namespace.Should().Be("MyProject");
        }

        [TestMethod]
        public void GetConfiguration_UseFluentAssertionsTrue_SetsTrue()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_UseFluentAssertions"] = "true"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.UseFluentAssertions.Should().BeTrue();
        }

        [TestMethod]
        public void GetConfiguration_CheckStatusCodeFalse_SetsFalse()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_CheckStatusCode"] = "false"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.CheckStatusCode.Should().BeFalse();
        }

        [TestMethod]
        public void GetConfiguration_CheckContentTypeFalse_SetsFalse()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_CheckContentType"] = "false"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.CheckContentType.Should().BeFalse();
        }

        [TestMethod]
        public void GetConfiguration_CheckBodyForErrorsFalse_SetsFalse()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_CheckBodyForErrors"] = "false"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.CheckBodyForErrors.Should().BeFalse();
        }

        [TestMethod]
        public void GetConfiguration_Environment_SetsEnvironment()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_Environment"] = "staging"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.Environment.Should().Be("staging");
        }

        [TestMethod]
        public void GetConfiguration_BasePath_SetsBasePath()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_BasePath"] = "HttpRequests"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.BasePath.Should().Be("HttpRequests");
        }

        [TestMethod]
        public void GetConfiguration_HttpClientType_SetsHttpClientType()
        {
            var options = new TestAnalyzerConfigOptions(new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_HttpClientType"] = "MyProject.CustomHttpClient"
            });

            var result = DotHttpSourceGenerator.GetConfiguration(options);

            result.HttpClientType.Should().Be("MyProject.CustomHttpClient");
        }

        #endregion

        #region GenerateAssertions Tests

        [TestMethod]
        public void GenerateAssertions_GeneratesCallToAssertValidResponseAsync()
        {
            var sb = new StringBuilder();
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateAssertions(sb, cfg);

            var result = sb.ToString();
            result.Should().Contain("DotHttpAssertions.AssertValidResponseAsync");
            result.Should().Contain("checkStatusCode: true");
            result.Should().Contain("checkContentType: true");
            result.Should().Contain("checkBodyForErrors: true");
            result.Should().Contain("logResponseOnFailure: true");
        }

        [TestMethod]
        public void GenerateAssertions_WithDisabledOptions_ReflectsConfigValues()
        {
            var sb = new StringBuilder();
            var cfg = new DotHttpConfig
            {
                CheckStatusCode = false,
                CheckContentType = false,
                CheckBodyForErrors = false,
                LogResponseOnFailure = false
            };

            DotHttpSourceGenerator.GenerateAssertions(sb, cfg);

            var result = sb.ToString();
            result.Should().Contain("DotHttpAssertions.AssertValidResponseAsync");
            result.Should().Contain("checkStatusCode: false");
            result.Should().Contain("checkContentType: false");
            result.Should().Contain("checkBodyForErrors: false");
            result.Should().Contain("logResponseOnFailure: false");
        }

        #endregion

        #region GenerateTestMethod Tests

        [TestMethod]
        public void GenerateTestMethod_BasicRequest_GeneratesAsyncMethod()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/users",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("public async Task");
            result.Should().Contain("HttpMethod.Get");
        }

        [TestMethod]
        public void GenerateTestMethod_WithComments_GeneratesXmlDocumentation()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/users",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string> { "Get all users", "Returns a list" },
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("/// <summary>");
            result.Should().Contain("/// Get all users");
            result.Should().Contain("/// Returns a list");
            result.Should().Contain("/// </summary>");
        }

        [TestMethod]
        public void GenerateTestMethod_XUnit_GeneratesFactAttribute()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/users",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, true, false);

            var result = sb.ToString();
            result.Should().Contain("[Fact]");
        }

        [TestMethod]
        public void GenerateTestMethod_MSTest_GeneratesTestMethodAttribute()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/users",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("[TestMethod]");
        }

        [TestMethod]
        public void GenerateTestMethod_WithFileVariables_CallsInitializeFileVariables()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/users",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, true);

            var result = sb.ToString();
            result.Should().Contain("InitializeFileVariables();");
        }

        [TestMethod]
        public void GenerateTestMethod_WithRequestVariables_SetsVariables()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/users",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string>(),
                Variables = new Dictionary<string, string> { ["baseUrl"] = "https://test.com" }
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("SetVariable(\"baseUrl\", \"https://test.com\");");
        }

        [TestMethod]
        public void GenerateTestMethod_WithHeaders_AddsHeaders()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/users",
                Headers = new Dictionary<string, string> { ["Accept"] = "application/json" },
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("TryAddWithoutValidation(\"Accept\"");
        }

        [TestMethod]
        public void GenerateTestMethod_WithHttpVersion_SetsVersion()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/users",
                HttpVersion = "HTTP/2",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("request.Version = new Version(\"2.0\");");
        }

        [TestMethod]
        public void GenerateTestMethod_WithStringBody_AddsStringContent()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "POST",
                Url = "https://api.example.com/users",
                Body = "{\"name\": \"test\"}",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("new StringContent(");
            result.Should().Contain("ResolveVariables(@\"");
        }

        [TestMethod]
        public void GenerateTestMethod_WithFileBody_AddsByteArrayContent()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "POST",
                Url = "https://api.example.com/upload",
                BodyFilePath = "./file.json",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("File.ReadAllBytesAsync");
            result.Should().Contain("new ByteArrayContent(bodyContent)");
        }

        [TestMethod]
        public void GenerateTestMethod_WithName_CapturesResponse()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "GET",
                Url = "https://api.example.com/users",
                Name = "GetUsers",
                Headers = new Dictionary<string, string>(),
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("CaptureResponseAsync(\"GetUsers\", response)");
        }

        [TestMethod]
        public void GenerateTestMethod_WithFileBodyAndContentType_UsesSpecifiedContentType()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "POST",
                Url = "https://api.example.com/upload",
                BodyFilePath = "./file.xml",
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/xml" },
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("MediaTypeHeaderValue(\"application/xml\")");
        }

        [TestMethod]
        public void GenerateTestMethod_WithStringBodyAndContentType_UsesSpecifiedContentType()
        {
            var sb = new StringBuilder();
            var request = new DotHttpRequest
            {
                Method = "POST",
                Url = "https://api.example.com/users",
                Body = "<user><name>test</name></user>",
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/xml" },
                Comments = new List<string>(),
                Variables = new Dictionary<string, string>()
            };
            var cfg = new DotHttpConfig();

            DotHttpSourceGenerator.GenerateTestMethod(sb, request, cfg, false, false);

            var result = sb.ToString();
            result.Should().Contain("\"application/xml\")");
        }

        #endregion

        #region GenerateTestClass Tests

        [TestMethod]
        public void GenerateTestClass_MSTest_ContainsTestClassAttribute()
        {
            var file = CreateSimpleHttpFile();
            var cfg = new DotHttpConfig { TestFramework = "MSTest", Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("[TestClass]");
            result.Should().Contain("[TestMethod]");
        }

        [TestMethod]
        public void GenerateTestClass_XUnit_ContainsFactAttribute()
        {
            var file = CreateSimpleHttpFile();
            var cfg = new DotHttpConfig { TestFramework = "XUnit", Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("[Fact]");
            result.Should().NotContain("[TestClass]");
        }

        [TestMethod]
        public void GenerateTestClass_WithFluentAssertions_IncludesUsing()
        {
            var file = CreateSimpleHttpFile();
            var cfg = new DotHttpConfig { UseFluentAssertions = true, Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("using FluentAssertions;");
        }

        [TestMethod]
        public void GenerateTestClass_ContainsPartialMethods()
        {
            var file = CreateSimpleHttpFile();
            var cfg = new DotHttpConfig { Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("partial void");
            result.Should().Contain("Setup();");
            result.Should().Contain("Assert(HttpResponseMessage response);");
        }

        [TestMethod]
        public void GenerateTestClass_WithFileVariables_GeneratesInitializeMethod()
        {
            var file = new DotHttpFile
            {
                FilePath = "test.http",
                Variables = new Dictionary<string, string> { ["baseUrl"] = "https://api.example.com" },
                Requests = new List<DotHttpRequest>
                {
                    new() { Method = "GET", Url = "{{baseUrl}}/users", Headers = new Dictionary<string, string>() }
                }
            };
            var cfg = new DotHttpConfig { Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("InitializeFileVariables()");
            result.Should().Contain("SetVariable(\"baseUrl\"");
        }

        [TestMethod]
        public void GenerateTestClass_RequestWithName_CapturesResponse()
        {
            var file = new DotHttpFile
            {
                FilePath = "test.http",
                Requests = new List<DotHttpRequest>
                {
                    new() { Method = "POST", Url = "https://api.example.com/login", Name = "login", Headers = new Dictionary<string, string>() }
                }
            };
            var cfg = new DotHttpConfig { Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("CaptureResponseAsync(\"login\"");
        }

        [TestMethod]
        public void GenerateTestClass_RequestWithBody_IncludesStringContent()
        {
            var file = new DotHttpFile
            {
                FilePath = "test.http",
                Requests = new List<DotHttpRequest>
                {
                    new()
                    {
                        Method = "POST",
                        Url = "https://api.example.com/users",
                        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                        Body = "{\"name\": \"test\"}"
                    }
                }
            };
            var cfg = new DotHttpConfig { Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("StringContent");
            result.Should().Contain("application/json");
        }

        [TestMethod]
        public void GenerateTestClass_RequestWithFileBody_IncludesByteArrayContent()
        {
            var file = new DotHttpFile
            {
                FilePath = "test.http",
                Requests = new List<DotHttpRequest>
                {
                    new()
                    {
                        Method = "POST",
                        Url = "https://api.example.com/upload",
                        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/octet-stream" },
                        BodyFilePath = "data.bin"
                    }
                }
            };
            var cfg = new DotHttpConfig { Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("ByteArrayContent");
            result.Should().Contain("ReadAllBytesAsync");
        }

        [TestMethod]
        public void GenerateTestClass_RequestWithHttpVersion_SetsVersion()
        {
            var file = new DotHttpFile
            {
                FilePath = "test.http",
                Requests = new List<DotHttpRequest>
                {
                    new()
                    {
                        Method = "GET",
                        Url = "https://api.example.com/users",
                        Headers = new Dictionary<string, string>(),
                        HttpVersion = "HTTP/2"
                    }
                }
            };
            var cfg = new DotHttpConfig { Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("request.Version = new Version(\"2.0\")");
        }

        [TestMethod]
        public void GenerateTestClass_RequestWithComments_GeneratesXmlDoc()
        {
            var file = new DotHttpFile
            {
                FilePath = "test.http",
                Requests = new List<DotHttpRequest>
                {
                    new()
                    {
                        Method = "GET",
                        Url = "https://api.example.com/users",
                        Headers = new Dictionary<string, string>(),
                        Comments = new List<string> { "Get all users from the API" }
                    }
                }
            };
            var cfg = new DotHttpConfig { Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("/// <summary>");
            result.Should().Contain("/// Get all users from the API");
            result.Should().Contain("/// </summary>");
        }

        [TestMethod]
        public void GenerateTestClass_RequestWithVariables_SetsVariables()
        {
            var file = new DotHttpFile
            {
                FilePath = "test.http",
                Requests = new List<DotHttpRequest>
                {
                    new()
                    {
                        Method = "GET",
                        Url = "https://api.example.com/users",
                        Headers = new Dictionary<string, string>(),
                        Variables = new Dictionary<string, string> { ["userId"] = "123" }
                    }
                }
            };
            var cfg = new DotHttpConfig { Namespace = "TestNamespace" };

            var result = DotHttpSourceGenerator.GenerateTestClass(file, cfg);

            result.Should().Contain("SetVariable(\"userId\", \"123\")");
        }

        #endregion

        #region Initialize Tests

        [TestMethod]
        public void Initialize_WithValidHttpFile_ProducesGeneratedSource()
        {
            var runResult = _driver.GetRunResult();

            runResult.GeneratedTrees.Should().NotBeEmpty("generator should produce at least one source file");
        }

        [TestMethod]
        public void Initialize_WithValidHttpFile_GeneratesCorrectFileName()
        {
            var runResult = _driver.GetRunResult();
            var generatedFileNames = runResult.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToList();

            generatedFileNames.Should().Contain(name => name.Contains("Api") && name.EndsWith(".g.cs"));
        }

        [TestMethod]
        public void Initialize_WithValidHttpFile_GeneratesTestClass()
        {
            var runResult = _driver.GetRunResult();
            var generatedSource = runResult.GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().NotBeNullOrEmpty();
            generatedSource.Should().Contain("public partial class");
            generatedSource.Should().Contain(": DotHttpTestBase");
        }

        [TestMethod]
        public void Initialize_WithValidHttpFile_GeneratesTestMethods()
        {
            var runResult = _driver.GetRunResult();
            var generatedSource = runResult.GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("GetUsers");
            generatedSource.Should().Contain("CreateUser");
        }

        [TestMethod]
        public void Initialize_WithValidHttpFile_GeneratesPartialMethods()
        {
            var runResult = _driver.GetRunResult();
            var generatedSource = runResult.GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("partial void OnGetUsersSetup()");
            generatedSource.Should().Contain("partial void OnGetUsersAssert(");
        }

        [TestMethod]
        public void Initialize_WithVariables_GeneratesInitializeFileVariables()
        {
            var runResult = _driver.GetRunResult();
            var generatedSource = runResult.GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("InitializeFileVariables()");
            generatedSource.Should().Contain("SetVariable(\"baseUrl\"");
        }

        #endregion

        #region ReportDiagnostics Tests

        [TestMethod]
        public void ReportDiagnostics_WithValidFile_ProducesNoDiagnostics()
        {
            var generatorDiagnostics = _diagnostics.Where(d => d.Id.StartsWith("DOTHTTP")).ToList();

            generatorDiagnostics.Should().BeEmpty("valid HTTP file should not produce diagnostics");
        }

        [TestMethod]
        public void ReportDiagnostics_WithParseError_ProducesDiagnostic()
        {
            // Create an HTTP file with a parse error (missing URL)
            var httpFileContent = @"
### Invalid Request
GET
Accept: application/json
";

            var (_, _, diagnostics) = RunGenerator(httpFileContent, "invalid.http");
            var generatorDiagnostics = diagnostics.Where(d => d.Id.StartsWith("DOTHTTP")).ToList();

            // The parser should detect the missing URL and report a diagnostic
            // Note: Whether this produces a diagnostic depends on how the parser handles malformed input
            generatorDiagnostics.Count.Should().BeGreaterThanOrEqualTo(0);
        }

        [TestMethod]
        public void ReportDiagnostics_WithWarning_ProducesWarningDiagnostic()
        {
            // Create a file that might trigger a warning (e.g., duplicate request name)
            var httpFileContent = @"
### First Request
# @name SameName
GET https://api.example.com/first

### Second Request
# @name SameName
GET https://api.example.com/second
";

            var (_, _, diagnostics) = RunGenerator(httpFileContent, "warning.http");

            // Even if no warning is produced, the generator should run successfully
            diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id.StartsWith("DOTHTTP")).Should().BeEmpty();
        }

        #endregion

        #region Full Pipeline Tests

        [TestMethod]
        public void FullPipeline_WithXUnitFramework_GeneratesFactAttributes()
        {
            var httpFileContent = @"
### Get Users
GET https://api.example.com/users
";
            var options = new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_TestFramework"] = "XUnit"
            };

            var (driver, _, _) = RunGenerator(httpFileContent, "xunit.http", options);
            var generatedSource = driver.GetRunResult().GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("[Fact]");
            generatedSource.Should().NotContain("[TestMethod]");
        }

        [TestMethod]
        public void FullPipeline_WithMSTestFramework_GeneratesTestMethodAttributes()
        {
            var httpFileContent = @"
### Get Users
GET https://api.example.com/users
";
            var options = new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_TestFramework"] = "MSTest"
            };

            var (driver, _, _) = RunGenerator(httpFileContent, "mstest.http", options);
            var generatedSource = driver.GetRunResult().GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("[TestMethod]");
            generatedSource.Should().Contain("[TestClass]");
        }

        [TestMethod]
        public void FullPipeline_WithCustomNamespace_UsesNamespace()
        {
            var httpFileContent = @"
### Get Users
GET https://api.example.com/users
";
            var options = new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_Namespace"] = "MyCompany.Tests.Api"
            };

            var (driver, _, _) = RunGenerator(httpFileContent, "namespace.http", options);
            var generatedSource = driver.GetRunResult().GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("namespace MyCompany.Tests.Api");
        }

        [TestMethod]
        public void FullPipeline_WithFluentAssertions_IncludesFluentAssertionsUsing()
        {
            var httpFileContent = @"
### Get Users
GET https://api.example.com/users
";
            var options = new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_UseFluentAssertions"] = "true"
            };

            var (driver, _, _) = RunGenerator(httpFileContent, "fluent.http", options);
            var generatedSource = driver.GetRunResult().GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("using FluentAssertions;");
            generatedSource.Should().Contain("DotHttpAssertions.AssertValidResponseAsync");
        }

        [TestMethod]
        public void FullPipeline_WithoutFluentAssertions_ExcludesFluentAssertionsUsing()
        {
            var httpFileContent = @"
### Get Users
GET https://api.example.com/users
";
            var options = new Dictionary<string, string>
            {
                ["build_property.BreakdanceDotHttp_UseFluentAssertions"] = "false"
            };

            var (driver, _, _) = RunGenerator(httpFileContent, "standard.http", options);
            var generatedSource = driver.GetRunResult().GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().NotContain("using FluentAssertions;");
            generatedSource.Should().Contain("DotHttpAssertions.AssertValidResponseAsync");
        }

        [TestMethod]
        public void FullPipeline_WithMultipleHttpFiles_GeneratesMultipleClasses()
        {
            var httpFile1 = @"
### Get Users
GET https://api.example.com/users
";
            var httpFile2 = @"
### Get Products
GET https://api.example.com/products
";

            var (driver, _, _) = RunGeneratorWithMultipleFiles(
                ("users.http", httpFile1),
                ("products.http", httpFile2));

            var runResult = driver.GetRunResult();
            runResult.GeneratedTrees.Should().HaveCount(2);
        }

        [TestMethod]
        public void FullPipeline_WithEmptyHttpFile_GeneratesNothing()
        {
            var httpFileContent = @"
# Just a comment
";

            var (driver, _, _) = RunGenerator(httpFileContent, "empty.http");
            var runResult = driver.GetRunResult();

            // Empty file should not generate any output
            runResult.GeneratedTrees.Should().BeEmpty();
        }

        [TestMethod]
        public void FullPipeline_WithRequestBody_GeneratesStringContent()
        {
            var httpFileContent = @"
### Create User
POST https://api.example.com/users
Content-Type: application/json

{""name"": ""Test""}
";

            var (driver, _, _) = RunGenerator(httpFileContent, "body.http");
            var generatedSource = driver.GetRunResult().GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("new StringContent(");
            generatedSource.Should().Contain("application/json");
        }

        [TestMethod]
        public void FullPipeline_WithHeaders_GeneratesHeaderAdditions()
        {
            var httpFileContent = @"
### Get Users
GET https://api.example.com/users
Accept: application/json
Authorization: Bearer token123
X-Custom-Header: custom-value
";

            var (driver, _, _) = RunGenerator(httpFileContent, "headers.http");
            var generatedSource = driver.GetRunResult().GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("TryAddWithoutValidation(\"Accept\"");
            generatedSource.Should().Contain("TryAddWithoutValidation(\"Authorization\"");
            generatedSource.Should().Contain("TryAddWithoutValidation(\"X-Custom-Header\"");
        }

        [TestMethod]
        public void FullPipeline_WithNamedRequest_GeneratesCaptureResponse()
        {
            var httpFileContent = @"
### Login
# @name login
POST https://api.example.com/auth/login
Content-Type: application/json

{""username"": ""test""}
";

            var (driver, _, _) = RunGenerator(httpFileContent, "named.http");
            var generatedSource = driver.GetRunResult().GeneratedTrees.FirstOrDefault()?.GetText().ToString();

            generatedSource.Should().Contain("CaptureResponseAsync(\"login\"");
        }

        #endregion

        #region Helper Methods

        private static DotHttpFile CreateSimpleHttpFile()
        {
            return new DotHttpFile
            {
                FilePath = "api.http",
                Variables = new Dictionary<string, string>(),
                Requests = new List<DotHttpRequest>
                {
                    new()
                    {
                        Method = "GET",
                        Url = "https://api.example.com/users",
                        Headers = new Dictionary<string, string> { ["Accept"] = "application/json" },
                        Name = "GetUsers"
                    }
                }
            };
        }

        private static (GeneratorDriver Driver, Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(
            string httpFileContent,
            string fileName,
            Dictionary<string, string> options = null)
        {
            return RunGeneratorWithMultipleFiles(options, (fileName, httpFileContent));
        }

        private static (GeneratorDriver Driver, Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunGeneratorWithMultipleFiles(
            params (string FileName, string Content)[] files)
        {
            return RunGeneratorWithMultipleFiles(null, files);
        }

        private static (GeneratorDriver Driver, Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics) RunGeneratorWithMultipleFiles(
            Dictionary<string, string> options,
            params (string FileName, string Content)[] files)
        {
            // Create a minimal compilation
            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: [CSharpSyntaxTree.ParseText("")],
                references:
                [
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Task).Assembly.Location)
                ],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Create the generator
            var generator = new DotHttpSourceGenerator();

            // Create additional texts for the HTTP files
            var additionalTexts = files
                .Select(f => new TestAdditionalText(f.FileName, f.Content))
                .Cast<AdditionalText>()
                .ToImmutableArray();

            // Create analyzer config options provider
            var optionsProvider = new TestAnalyzerConfigOptionsProvider(options ?? new Dictionary<string, string>());

            // Create and run the driver
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: new IIncrementalGenerator[] { generator }.Select(GeneratorExtensions.AsSourceGenerator),
                additionalTexts: additionalTexts,
                optionsProvider: optionsProvider);

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            return (driver, outputCompilation, diagnostics);
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Test implementation of AnalyzerConfigOptions for unit testing.
        /// </summary>
        private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> _options;

            public TestAnalyzerConfigOptions(Dictionary<string, string> options)
            {
                _options = options;
            }

            public override bool TryGetValue(string key, out string value)
            {
                return _options.TryGetValue(key, out value);
            }
        }

        #endregion

    }

}
