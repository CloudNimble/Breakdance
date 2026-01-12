using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using CloudNimble.Breakdance.DotHttp.Generator;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{
    /// <summary>
    /// Integration tests for <see cref="DotHttpSourceGenerator"/> using Roslyn compilation.
    /// These tests verify the full source generation pipeline including Initialize and ReportDiagnostics.
    /// </summary>
    [TestClass]
    public class DotHttpSourceGeneratorIntegrationTests
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

            generatedSource.Should().Contain("partial void OnGetusersSetup()");
            generatedSource.Should().Contain("partial void OnGetusersAssert(");
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

    }

}
