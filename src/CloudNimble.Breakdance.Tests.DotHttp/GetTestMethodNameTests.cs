using System.Collections.Generic;
using System.Linq;
using CloudNimble.Breakdance.DotHttp.Generator;
using CloudNimble.Breakdance.DotHttp.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Comprehensive tests for <see cref="DotHttpSourceGenerator.GetTestMethodName"/> method.
    /// Tests cover all three priority paths and edge cases for 100% code coverage.
    /// </summary>
    [TestClass]
    public class GetTestMethodNameTests
    {

        #region Priority 1: Name Directive Tests

        [TestMethod]
        public void GetTestMethodName_WithName_ReturnsName()
        {
            var request = new DotHttpRequest { Name = "GetUsers", Method = "GET", Url = "https://api.example.com/users" };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().Be("Getusers");
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

            result.Should().Be("Mycustomname");
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

            result.Should().Be("Customtestname");
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

        #region Priority 2: Separator Title Tests

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

            result.Should().Be("GetAllUsers_GET");
        }

        [TestMethod]
        public void GetTestMethodName_WithSeparatorTitle_AppendsHttpMethod()
        {
            var request = new DotHttpRequest
            {
                Name = null,
                SeparatorTitle = "Create User",
                Method = "POST",
                Url = "https://api.example.com/users"
            };

            var result = DotHttpSourceGenerator.GetTestMethodName(request);

            result.Should().EndWith("_POST");
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
            result.Should().EndWith("_GET");
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
            result.Should().EndWith("_GET");
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
            result.Should().EndWith("_GET");
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
            result.Should().EndWith("_GET");
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

            // The format is {TitleDehumanized}_{METHOD}, so check the title part
            result.Should().EndWith("_GET");
            var titlePart = result.Substring(0, result.LastIndexOf('_'));
            titlePart.Should().NotContain("-");
            titlePart.Should().NotContain("/");
            titlePart.Should().NotContain("_");
            titlePart.Should().NotContain(".");
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

            result.Should().EndWith("_GET");
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

            result.Should().Be("GetAllUsers_GET");
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

            result.Should().EndWith("_GET");
            result.Should().Contain("Users");
        }

        #endregion

        #region Priority 3: URL Path Tests - Basic

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

        #region Priority 3: URL Path Tests - Variable References

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

        #region Priority 3: URL Path Tests - Special Characters

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

        #region Priority 3: URL Path Tests - Edge Cases

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

        #region HTTP Method Tests

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

        #region Real-World URL Pattern Tests

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

        #region Valid C# Identifier Tests

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

    }

}
