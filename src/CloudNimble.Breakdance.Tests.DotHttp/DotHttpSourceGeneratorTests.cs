using CloudNimble.Breakdance.DotHttp.Generator;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for the <see cref="DotHttpSourceGenerator"/> class.
    /// </summary>
    [TestClass]
    public class DotHttpSourceGeneratorTests
    {

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

        #region GetTestMethodName Tests

        [TestMethod]
        public void GetTestMethodName_WithName_ReturnsName()
        {
            var request = new DotHttpRequest { Name = "GetUsers", Method = "GET", Url = "https://api.example.com/users" };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("Getusers");
        }

        [TestMethod]
        public void GetTestMethodName_WithoutName_GeneratesFromMethodAndUrl()
        {
            var request = new DotHttpRequest { Name = null, Method = "GET", Url = "https://api.example.com/users" };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Contain("GET");
        }

        [TestMethod]
        public void GetTestMethodName_UrlWithVariables_RemovesVariables()
        {
            var request = new DotHttpRequest { Name = null, Method = "GET", Url = "{{baseUrl}}/users/{{userId}}" };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().NotContain("{{");
            result.Should().NotContain("}}");
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
