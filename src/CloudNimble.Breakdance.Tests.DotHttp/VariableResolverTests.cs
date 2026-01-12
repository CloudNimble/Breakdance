using System;
using System.Collections.Generic;
using CloudNimble.Breakdance.DotHttp;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.Breakdance.Tests.DotHttp
{

    /// <summary>
    /// Tests for <see cref="VariableResolver"/>.
    /// </summary>
    [TestClass]
    public class VariableResolverTests
    {

        #region SetVariable Tests

        [TestMethod]
        public void SetVariable_SetsValue_CanBeResolved()
        {
            var resolver = new VariableResolver();
            resolver.SetVariable("baseUrl", "https://api.example.com");

            var result = resolver.Resolve("{{baseUrl}}/users");

            result.Should().Be("https://api.example.com/users");
        }

        [TestMethod]
        public void SetVariable_OverwritesExistingValue()
        {
            var resolver = new VariableResolver();
            resolver.SetVariable("url", "old-value");
            resolver.SetVariable("url", "new-value");

            var result = resolver.Resolve("{{url}}");

            result.Should().Be("new-value");
        }

        #endregion

        #region SetVariables Tests

        [TestMethod]
        public void SetVariables_SetsMultipleValues()
        {
            var resolver = new VariableResolver();
            resolver.SetVariables(new Dictionary<string, string>
            {
                ["baseUrl"] = "https://api.example.com",
                ["apiKey"] = "secret-key"
            });

            var result = resolver.Resolve("{{baseUrl}}?key={{apiKey}}");

            result.Should().Be("https://api.example.com?key=secret-key");
        }

        [TestMethod]
        public void SetVariables_NullDictionary_DoesNotThrow()
        {
            var resolver = new VariableResolver();

            var action = () => resolver.SetVariables(null);

            action.Should().NotThrow();
        }

        [TestMethod]
        public void SetVariables_EmptyDictionary_DoesNotThrow()
        {
            var resolver = new VariableResolver();

            var action = () => resolver.SetVariables(new Dictionary<string, string>());

            action.Should().NotThrow();
        }

        #endregion

        #region Clear Tests

        [TestMethod]
        public void Clear_RemovesAllVariables()
        {
            var resolver = new VariableResolver();
            resolver.SetVariable("foo", "bar");
            resolver.Clear();

            var result = resolver.Resolve("{{foo}}");

            result.Should().Be("{{foo}}");
        }

        #endregion

        #region Resolve Tests

        [TestMethod]
        public void Resolve_NullInput_ReturnsNull()
        {
            var resolver = new VariableResolver();

            var result = resolver.Resolve(null);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Resolve_EmptyInput_ReturnsEmpty()
        {
            var resolver = new VariableResolver();

            var result = resolver.Resolve(string.Empty);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Resolve_NoVariables_ReturnsOriginal()
        {
            var resolver = new VariableResolver();

            var result = resolver.Resolve("plain text");

            result.Should().Be("plain text");
        }

        [TestMethod]
        public void Resolve_UnknownVariable_ReturnsOriginalPlaceholder()
        {
            var resolver = new VariableResolver();

            var result = resolver.Resolve("{{unknown}}");

            result.Should().Be("{{unknown}}");
        }

        [TestMethod]
        public void Resolve_MultipleVariables_ResolvesAll()
        {
            var resolver = new VariableResolver();
            resolver.SetVariable("host", "localhost");
            resolver.SetVariable("port", "8080");

            var result = resolver.Resolve("http://{{host}}:{{port}}");

            result.Should().Be("http://localhost:8080");
        }

        #endregion

        #region ResolveSimpleVariables Tests

        [TestMethod]
        public void ResolveSimpleVariables_NullInput_ReturnsNull()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveSimpleVariables(null);

            result.Should().BeNull();
        }

        [TestMethod]
        public void ResolveSimpleVariables_EmptyInput_ReturnsEmpty()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveSimpleVariables(string.Empty);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ResolveSimpleVariables_ResolvesKnownVariables()
        {
            var resolver = new VariableResolver();
            resolver.SetVariable("name", "John");

            var result = resolver.ResolveSimpleVariables("Hello, {{name}}!");

            result.Should().Be("Hello, John!");
        }

        #endregion

        #region ResolveDynamicVariables Tests

        [TestMethod]
        public void ResolveDynamicVariables_NullInput_ReturnsNull()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables(null);

            result.Should().BeNull();
        }

        [TestMethod]
        public void ResolveDynamicVariables_EmptyInput_ReturnsEmpty()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables(string.Empty);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ResolveDynamicVariables_Guid_ReturnsValidGuid()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$guid}}");

            Guid.TryParse(result, out _).Should().BeTrue();
        }

        [TestMethod]
        public void ResolveDynamicVariables_Timestamp_ReturnsUnixTimestamp()
        {
            var resolver = new VariableResolver();
            var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var result = resolver.ResolveDynamicVariables("{{$timestamp}}");

            var timestamp = long.Parse(result);
            timestamp.Should().BeGreaterThanOrEqualTo(before);
        }

        [TestMethod]
        public void ResolveDynamicVariables_RandomInt_ReturnsNumber()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$randomInt}}");

            int.TryParse(result, out _).Should().BeTrue();
        }

        [TestMethod]
        public void ResolveDynamicVariables_RandomIntWithMax_ReturnsNumberInRange()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$randomInt 100}}");

            var value = int.Parse(result);
            value.Should().BeGreaterThanOrEqualTo(0);
            value.Should().BeLessThan(100);
        }

        [TestMethod]
        public void ResolveDynamicVariables_RandomIntWithMinMax_ReturnsNumberInRange()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$randomInt 50 100}}");

            var value = int.Parse(result);
            value.Should().BeGreaterThanOrEqualTo(50);
            value.Should().BeLessThan(100);
        }

        [TestMethod]
        public void ResolveDynamicVariables_RandomIntWithInvalidArgs_ReturnsDefaultRange()
        {
            var resolver = new VariableResolver();

            // Non-numeric arguments should fall back to 0 to int.MaxValue
            var result = resolver.ResolveDynamicVariables("{{$randomInt invalid args}}");

            int.TryParse(result, out var value).Should().BeTrue();
            value.Should().BeGreaterThanOrEqualTo(0);
        }

        [TestMethod]
        public void ResolveDynamicVariables_RandomIntWithNonNumericArg_ReturnsDefaultRange()
        {
            var resolver = new VariableResolver();

            // Single non-numeric argument should fall back to 0 to int.MaxValue
            var result = resolver.ResolveDynamicVariables("{{$randomInt abc}}");

            int.TryParse(result, out var value).Should().BeTrue();
            value.Should().BeGreaterThanOrEqualTo(0);
        }

        [TestMethod]
        public void ResolveDynamicVariables_RandomIntWithMixedArgs_ReturnsDefaultRange()
        {
            var resolver = new VariableResolver();

            // One numeric, one non-numeric should fall back to 0 to int.MaxValue
            var result = resolver.ResolveDynamicVariables("{{$randomInt 10 abc}}");

            int.TryParse(result, out var value).Should().BeTrue();
            value.Should().BeGreaterThanOrEqualTo(0);
        }

        [TestMethod]
        public void ResolveDynamicVariables_Datetime_ReturnsIso8601()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$datetime}}");

            result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z");
        }

        [TestMethod]
        public void ResolveDynamicVariables_DatetimeRfc1123_ReturnsRfc1123Format()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$datetime rfc1123}}");

            result.Should().Contain(",");
            result.Should().Contain("GMT");
        }

        [TestMethod]
        public void ResolveDynamicVariables_DatetimeIso8601_ReturnsIso8601Format()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$datetime iso8601}}");

            result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z");
        }

        [TestMethod]
        public void ResolveDynamicVariables_DatetimeWithOffset_AppliesOffset()
        {
            var resolver = new VariableResolver();
            var tomorrow = DateTimeOffset.UtcNow.AddDays(1);

            var result = resolver.ResolveDynamicVariables("{{$datetime iso8601 1 d}}");

            var parsed = DateTimeOffset.Parse(result);
            parsed.Date.Should().Be(tomorrow.Date);
        }

        [TestMethod]
        public void ResolveDynamicVariables_LocalDatetime_ReturnsLocalTime()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$localDatetime}}");

            result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}");
        }

        [TestMethod]
        public void ResolveDynamicVariables_ProcessEnv_ReturnsEnvironmentVariable()
        {
            var resolver = new VariableResolver();
            Environment.SetEnvironmentVariable("TEST_VAR_123", "test-value");

            var result = resolver.ResolveDynamicVariables("{{$processEnv TEST_VAR_123}}");

            result.Should().Be("test-value");
            Environment.SetEnvironmentVariable("TEST_VAR_123", null);
        }

        [TestMethod]
        public void ResolveDynamicVariables_ProcessEnv_MissingVar_ReturnsEmpty()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$processEnv NONEXISTENT_VAR_XYZ}}");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ResolveDynamicVariables_DotEnv_FallsBackToProcessEnv()
        {
            var resolver = new VariableResolver();
            Environment.SetEnvironmentVariable("DOTENV_TEST_VAR", "dotenv-value");

            var result = resolver.ResolveDynamicVariables("{{$dotEnv DOTENV_TEST_VAR}}");

            result.Should().Be("dotenv-value");
            Environment.SetEnvironmentVariable("DOTENV_TEST_VAR", null);
        }

        [TestMethod]
        public void ResolveDynamicVariables_UnknownFunction_ReturnsOriginalWithLowercasedName()
        {
            // Unknown functions are returned with the function name lowercased
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$unknownFunc}}");

            result.Should().Be("{{$unknownfunc}}");
        }

        [TestMethod]
        public void ResolveDynamicVariables_UnknownFunctionWithArgs_ReturnsOriginalWithLowercasedName()
        {
            // Unknown functions are returned with the function name lowercased
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$unknownFunc arg1}}");

            result.Should().Be("{{$unknownfunc arg1}}");
        }

        #endregion

        #region GetVariableNames Tests

        [TestMethod]
        public void GetVariableNames_NullInput_ReturnsEmptySet()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetVariableNames(null);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetVariableNames_EmptyInput_ReturnsEmptySet()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetVariableNames(string.Empty);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetVariableNames_NoVariables_ReturnsEmptySet()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetVariableNames("plain text");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetVariableNames_SingleVariable_ReturnsVariableName()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetVariableNames("{{baseUrl}}/users");

            result.Should().ContainSingle().Which.Should().Be("baseUrl");
        }

        [TestMethod]
        public void GetVariableNames_MultipleVariables_ReturnsAllNames()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetVariableNames("{{host}}:{{port}}/{{path}}");

            result.Should().HaveCount(3);
            result.Should().Contain("host");
            result.Should().Contain("port");
            result.Should().Contain("path");
        }

        [TestMethod]
        public void GetVariableNames_DuplicateVariables_ReturnsUniqueNames()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetVariableNames("{{url}}/{{url}}");

            result.Should().ContainSingle().Which.Should().Be("url");
        }

        #endregion

        #region GetResponseReferenceNames Tests

        [TestMethod]
        public void GetResponseReferenceNames_NullInput_ReturnsEmptySet()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetResponseReferenceNames(null);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetResponseReferenceNames_EmptyInput_ReturnsEmptySet()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetResponseReferenceNames(string.Empty);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetResponseReferenceNames_NoReferences_ReturnsEmptySet()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetResponseReferenceNames("{{baseUrl}}/users");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetResponseReferenceNames_SingleReference_ReturnsRequestName()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetResponseReferenceNames("Bearer {{login.response.body.$.token}}");

            result.Should().ContainSingle().Which.Should().Be("login");
        }

        [TestMethod]
        public void GetResponseReferenceNames_MultipleReferences_ReturnsAllNames()
        {
            var resolver = new VariableResolver();

            var result = resolver.GetResponseReferenceNames(
                "{{auth.response.body.$.token}} {{user.response.headers.X-Id}}");

            result.Should().HaveCount(2);
            result.Should().Contain("auth");
            result.Should().Contain("user");
        }

        #endregion

        #region HasResponseReferences Tests

        [TestMethod]
        public void HasResponseReferences_NullInput_ReturnsFalse()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasResponseReferences(null);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasResponseReferences_EmptyInput_ReturnsFalse()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasResponseReferences(string.Empty);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasResponseReferences_NoReferences_ReturnsFalse()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasResponseReferences("{{baseUrl}}/users");

            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasResponseReferences_WithReferences_ReturnsTrue()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasResponseReferences("Bearer {{login.response.body.$.token}}");

            result.Should().BeTrue();
        }

        #endregion

        #region HasUnresolvedVariables Tests

        [TestMethod]
        public void HasUnresolvedVariables_NullInput_ReturnsFalse()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasUnresolvedVariables(null);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasUnresolvedVariables_EmptyInput_ReturnsFalse()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasUnresolvedVariables(string.Empty);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasUnresolvedVariables_NoVariables_ReturnsFalse()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasUnresolvedVariables("plain text");

            result.Should().BeFalse();
        }

        [TestMethod]
        public void HasUnresolvedVariables_WithSimpleVariable_ReturnsTrue()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasUnresolvedVariables("{{baseUrl}}");

            result.Should().BeTrue();
        }

        [TestMethod]
        public void HasUnresolvedVariables_WithDynamicVariable_ReturnsTrue()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasUnresolvedVariables("{{$guid}}");

            result.Should().BeTrue();
        }

        [TestMethod]
        public void HasUnresolvedVariables_WithResponseReference_ReturnsTrue()
        {
            var resolver = new VariableResolver();

            var result = resolver.HasUnresolvedVariables("{{login.response.body.$.token}}");

            result.Should().BeTrue();
        }

        #endregion

        #region ApplyOffset Tests

        [TestMethod]
        public void ApplyOffset_Milliseconds_AddsMilliseconds()
        {
            var dateTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, 500, "ms");

            result.Should().Be(dateTime.AddMilliseconds(500));
        }

        [TestMethod]
        public void ApplyOffset_Seconds_AddsSeconds()
        {
            var dateTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, 30, "s");

            result.Should().Be(dateTime.AddSeconds(30));
        }

        [TestMethod]
        public void ApplyOffset_Minutes_AddsMinutes()
        {
            var dateTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, 15, "m");

            result.Should().Be(dateTime.AddMinutes(15));
        }

        [TestMethod]
        public void ApplyOffset_Hours_AddsHours()
        {
            var dateTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, 2, "h");

            result.Should().Be(dateTime.AddHours(2));
        }

        [TestMethod]
        public void ApplyOffset_Days_AddsDays()
        {
            var dateTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, 7, "d");

            result.Should().Be(dateTime.AddDays(7));
        }

        [TestMethod]
        public void ApplyOffset_Weeks_AddsWeeks()
        {
            var dateTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, 2, "w");

            result.Should().Be(dateTime.AddDays(14));
        }

        [TestMethod]
        public void ApplyOffset_Months_AddsMonths()
        {
            var dateTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, 3, "M");

            result.Should().Be(dateTime.AddMonths(3));
        }

        [TestMethod]
        public void ApplyOffset_Years_AddsYears()
        {
            var dateTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, 1, "y");

            result.Should().Be(dateTime.AddYears(1));
        }

        [TestMethod]
        public void ApplyOffset_NegativeValue_SubtractsTime()
        {
            var dateTime = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, -7, "d");

            result.Should().Be(dateTime.AddDays(-7));
        }

        [TestMethod]
        public void ApplyOffset_UnknownUnit_ReturnsOriginal()
        {
            var dateTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var result = VariableResolver.ApplyOffset(dateTime, 5, "unknown");

            result.Should().Be(dateTime);
        }

        #endregion

        #region ParseDateTimeArguments Tests

        [TestMethod]
        public void ParseDateTimeArguments_NullInput_SetsDefaultValues()
        {
            VariableResolver.ParseDateTimeArguments(null, out var format, out var offsetValue, out var offsetUnit);

            format.Should().BeNull();
            offsetValue.Should().Be(0);
            offsetUnit.Should().BeNull();
        }

        [TestMethod]
        public void ParseDateTimeArguments_EmptyInput_SetsDefaultValues()
        {
            VariableResolver.ParseDateTimeArguments("", out var format, out var offsetValue, out var offsetUnit);

            format.Should().BeNull();
            offsetValue.Should().Be(0);
            offsetUnit.Should().BeNull();
        }

        [TestMethod]
        public void ParseDateTimeArguments_WhitespaceInput_SetsDefaultValues()
        {
            VariableResolver.ParseDateTimeArguments("   ", out var format, out var offsetValue, out var offsetUnit);

            format.Should().BeNull();
            offsetValue.Should().Be(0);
            offsetUnit.Should().BeNull();
        }

        [TestMethod]
        public void ParseDateTimeArguments_Rfc1123Format_ParsesFormat()
        {
            VariableResolver.ParseDateTimeArguments("rfc1123", out var format, out var offsetValue, out var offsetUnit);

            format.Should().Be("rfc1123");
            offsetValue.Should().Be(0);
            offsetUnit.Should().BeNull();
        }

        [TestMethod]
        public void ParseDateTimeArguments_Iso8601Format_ParsesFormat()
        {
            VariableResolver.ParseDateTimeArguments("iso8601", out var format, out var offsetValue, out var offsetUnit);

            format.Should().Be("iso8601");
            offsetValue.Should().Be(0);
            offsetUnit.Should().BeNull();
        }

        [TestMethod]
        public void ParseDateTimeArguments_FormatWithOffset_ParsesBoth()
        {
            VariableResolver.ParseDateTimeArguments("rfc1123 1 d", out var format, out var offsetValue, out var offsetUnit);

            format.Should().Be("rfc1123");
            offsetValue.Should().Be(1);
            offsetUnit.Should().Be("d");
        }

        [TestMethod]
        public void ParseDateTimeArguments_OffsetOnly_ParsesOffset()
        {
            VariableResolver.ParseDateTimeArguments("5 h", out var format, out var offsetValue, out var offsetUnit);

            format.Should().BeNull();
            offsetValue.Should().Be(5);
            offsetUnit.Should().Be("h");
        }

        [TestMethod]
        public void ParseDateTimeArguments_NegativeOffset_ParsesNegativeValue()
        {
            VariableResolver.ParseDateTimeArguments("-3 d", out var format, out var offsetValue, out var offsetUnit);

            format.Should().BeNull();
            offsetValue.Should().Be(-3);
            offsetUnit.Should().Be("d");
        }

        [TestMethod]
        public void ParseDateTimeArguments_QuotedFormat_ParsesQuotedFormat()
        {
            VariableResolver.ParseDateTimeArguments("\"dd-MM-yyyy\"", out var format, out var offsetValue, out var offsetUnit);

            format.Should().Be("dd-MM-yyyy");
            offsetValue.Should().Be(0);
            offsetUnit.Should().BeNull();
        }

        [TestMethod]
        public void ParseDateTimeArguments_SingleQuotedFormat_ParsesQuotedFormat()
        {
            VariableResolver.ParseDateTimeArguments("'yyyy/MM/dd'", out var format, out var offsetValue, out var offsetUnit);

            format.Should().Be("yyyy/MM/dd");
            offsetValue.Should().Be(0);
            offsetUnit.Should().BeNull();
        }

        [TestMethod]
        public void ParseDateTimeArguments_CustomFormatWithSpaces_ParsesEntireFormat()
        {
            VariableResolver.ParseDateTimeArguments("\"MMM dd yyyy\"", out var format, out var offsetValue, out var offsetUnit);

            format.Should().Be("MMM dd yyyy");
        }

        [TestMethod]
        public void ParseDateTimeArguments_CustomFormatNoQuotes_ParsesAsFormat()
        {
            VariableResolver.ParseDateTimeArguments("yyyy-MM-dd", out var format, out var offsetValue, out var offsetUnit);

            format.Should().Be("yyyy-MM-dd");
        }

        #endregion

        #region Timestamp Tests

        [TestMethod]
        public void ResolveDynamicVariables_TimestampWithOffset_AppliesOffset()
        {
            var resolver = new VariableResolver();
            var tomorrow = DateTimeOffset.UtcNow.AddDays(1);

            var result = resolver.ResolveDynamicVariables("{{$timestamp 1 d}}");

            var timestamp = long.Parse(result);
            var resultTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            resultTime.Date.Should().Be(tomorrow.Date);
        }

        [TestMethod]
        public void ResolveDynamicVariables_TimestampNegativeOffset_SubtractsTime()
        {
            var resolver = new VariableResolver();
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1);

            var result = resolver.ResolveDynamicVariables("{{$timestamp -1 d}}");

            var timestamp = long.Parse(result);
            var resultTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            resultTime.Date.Should().Be(yesterday.Date);
        }

        #endregion

        #region ProcessEnv Edge Cases

        [TestMethod]
        public void ResolveDynamicVariables_ProcessEnv_EmptyName_ReturnsOriginalString()
        {
            // The regex requires whitespace followed by at least one character for arguments
            // A trailing space alone doesn't match, so the string remains unchanged
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$processEnv }}");

            result.Should().Be("{{$processEnv }}");
        }

        [TestMethod]
        public void ResolveDynamicVariables_ProcessEnv_WhitespaceName_ReturnsEmpty()
        {
            // With multiple spaces, the regex matches and captures spaces as arguments
            // After trimming, it's an empty string, which ResolveProcessEnv returns as empty
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$processEnv   }}");

            result.Should().BeEmpty();
        }

        #endregion

        #region LocalDatetime Tests

        [TestMethod]
        public void ResolveDynamicVariables_LocalDatetimeRfc1123_ReturnsRfc1123()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$localDatetime rfc1123}}");

            result.Should().Contain(",");
            result.Should().Contain("GMT");
        }

        [TestMethod]
        public void ResolveDynamicVariables_LocalDatetimeWithOffset_AppliesOffset()
        {
            var resolver = new VariableResolver();
            var tomorrow = DateTimeOffset.Now.AddDays(1);

            var result = resolver.ResolveDynamicVariables("{{$localDatetime iso8601 1 d}}");

            var parsed = DateTimeOffset.Parse(result);
            parsed.Date.Should().Be(tomorrow.Date);
        }

        [TestMethod]
        public void ResolveDynamicVariables_LocalDatetimeCustomFormat_UsesCustomFormat()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$localDatetime yyyy}}");

            result.Should().Be(DateTime.Now.Year.ToString());
        }

        #endregion

        #region Datetime Custom Format Tests

        [TestMethod]
        public void ResolveDynamicVariables_DatetimeCustomFormat_UsesFormatLiterally()
        {
            // .NET's DateTime.ToString() doesn't throw for most "invalid" format strings
            // Instead it interprets known characters and outputs unknown ones literally
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$datetime yyyy-MM-dd}}");

            result.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}");
        }

        [TestMethod]
        public void ResolveDynamicVariables_DatetimeQuotedFormat_UsesQuotedFormat()
        {
            var resolver = new VariableResolver();

            var result = resolver.ResolveDynamicVariables("{{$datetime \"yyyy/MM/dd HH:mm\"}}");

            result.Should().MatchRegex(@"\d{4}/\d{2}/\d{2} \d{2}:\d{2}");
        }

        #endregion

    }

}
