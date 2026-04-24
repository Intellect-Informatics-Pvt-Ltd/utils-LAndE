using System.Text.Json;
using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core;
using Microsoft.Extensions.Options;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Core;

/// <summary>
/// Unit tests for <see cref="DefaultRedactionEngine"/> covering structural masking,
/// attribute masking, regex masking, layer ordering, and shallow copy guarantee.
/// </summary>
public class DefaultRedactionEngineTests
{
    private static DefaultRedactionEngine CreateEngine(
        string[]? paths = null,
        string[]? regexes = null)
    {
        var options = Options.Create(new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = "TestModule",
            Masking = new MaskingOptions
            {
                Enabled = true,
                Paths = paths ?? [],
                Regexes = regexes ?? []
            }
        });
        return new DefaultRedactionEngine(options);
    }

    #region Structural Path Masking

    [Fact]
    public void RedactJson_MasksFieldMatchingConfiguredPath()
    {
        var engine = CreateEngine(paths: new[] { "$.body.password" });
        var json = JsonDocument.Parse("""{"body":{"password":"secret123","name":"John"}}""");

        var result = engine.RedactJson(json.RootElement);
        var resultJson = result.GetRawText();

        resultJson.Should().Contain("***REDACTED***");
        resultJson.Should().Contain("John");
    }

    [Fact]
    public void RedactJson_MasksNestedFieldMatchingPath()
    {
        var engine = CreateEngine(paths: new[] { "$.headers.authorization" });
        var json = JsonDocument.Parse("""{"headers":{"authorization":"Bearer token","accept":"*/*"}}""");

        var result = engine.RedactJson(json.RootElement);
        var resultJson = result.GetRawText();

        resultJson.Should().Contain("***REDACTED***");
        resultJson.Should().Contain("*/*");
    }

    [Fact]
    public void RedactJson_DoesNotMaskUnmatchedPaths()
    {
        var engine = CreateEngine(paths: new[] { "$.body.password" });
        var json = JsonDocument.Parse("""{"body":{"name":"John","email":"test@test.com"}}""");

        var result = engine.RedactJson(json.RootElement);
        var resultJson = result.GetRawText();

        resultJson.Should().Contain("John");
        // Email may be masked by regex fallback, but name should not be
        resultJson.Should().NotContain("***REDACTED***");
    }

    [Fact]
    public void RedactJson_NoPathsConfigured_ReturnsOriginal()
    {
        var engine = CreateEngine(paths: Array.Empty<string>());
        var json = JsonDocument.Parse("""{"password":"secret"}""");

        var result = engine.RedactJson(json.RootElement);

        result.GetRawText().Should().Contain("secret");
    }

    #endregion

    #region Attribute-Driven Masking

    [Fact]
    public void RedactObject_MasksSensitiveProperty_KeepingLastNChars()
    {
        var engine = CreateEngine();
        var dto = new SensitiveDto { AccountNumber = "1234567890", Name = "John" };

        var result = (SensitiveDto)engine.RedactObject(dto);

        result.AccountNumber.Should().EndWith("7890");
        result.AccountNumber!.Substring(0, result.AccountNumber.Length - 4)
            .Should().MatchRegex(@"^\*+$");
    }

    [Fact]
    public void RedactObject_ExcludesDoNotLogProperty()
    {
        var engine = CreateEngine();
        var dto = new DoNotLogDto { PublicField = "visible", SecretField = "hidden" };

        var result = (DoNotLogDto)engine.RedactObject(dto);

        result.PublicField.Should().Be("visible");
        result.SecretField.Should().BeNull();
    }

    [Fact]
    public void RedactObject_AppliesMaskAttribute_WithRegex()
    {
        var engine = CreateEngine();
        var dto = new MaskDto { CardNumber = "4111-1111-1111-1111" };

        var result = (MaskDto)engine.RedactObject(dto);

        result.CardNumber.Should().NotBe("4111-1111-1111-1111");
        result.CardNumber.Should().Contain("XXXX");
    }

    [Fact]
    public void RedactObject_SensitiveRedactMode_ReplacesEntireValue()
    {
        var engine = CreateEngine();
        var dto = new RedactModeDto { Secret = "my-secret-value" };

        var result = (RedactModeDto)engine.RedactObject(dto);

        result.Secret.Should().Be("***REDACTED***");
    }

    #endregion

    #region Regex Fallback Patterns

    [Fact]
    public void Redact_MasksEmailAddress()
    {
        var engine = CreateEngine();

        var result = engine.Redact("Contact: user@example.com for details");

        result.Should().Contain("***EMAIL***");
        result.Should().NotContain("user@example.com");
    }

    [Fact]
    public void Redact_MasksPanNumber()
    {
        var engine = CreateEngine();

        var result = engine.Redact("PAN: ABCDE1234F");

        result.Should().Contain("***PAN***");
    }

    [Fact]
    public void Redact_MasksConnectionStringPassword()
    {
        var engine = CreateEngine();

        var result = engine.Redact("Server=db;password=MySecret123;Database=test");

        result.Should().NotContain("MySecret123");
        result.Should().Contain("password=***");
    }

    [Fact]
    public void Redact_EmptyString_ReturnsEmpty()
    {
        var engine = CreateEngine();

        engine.Redact("").Should().BeEmpty();
    }

    [Fact]
    public void Redact_NullString_ReturnsNull()
    {
        var engine = CreateEngine();

        engine.Redact(null!).Should().BeNull();
    }

    #endregion

    #region Shallow Copy Guarantee

    [Fact]
    public void RedactObject_DoesNotMutateOriginal()
    {
        var engine = CreateEngine();
        var original = new SensitiveDto { AccountNumber = "1234567890", Name = "John" };
        var originalAccount = original.AccountNumber;
        var originalName = original.Name;

        _ = engine.RedactObject(original);

        original.AccountNumber.Should().Be(originalAccount);
        original.Name.Should().Be(originalName);
    }

    [Fact]
    public void RedactObject_ReturnsNewInstance()
    {
        var engine = CreateEngine();
        var original = new SensitiveDto { AccountNumber = "1234567890", Name = "John" };

        var result = engine.RedactObject(original);

        result.Should().NotBeSameAs(original);
    }

    [Fact]
    public void RedactObject_DoNotLog_OriginalRetainsValue()
    {
        var engine = CreateEngine();
        var original = new DoNotLogDto { PublicField = "visible", SecretField = "hidden" };

        _ = engine.RedactObject(original);

        original.SecretField.Should().Be("hidden");
    }

    #endregion

    #region RedactProperties

    [Fact]
    public void RedactProperties_AppliesRegexToStringValues()
    {
        var engine = CreateEngine();
        var props = new Dictionary<string, object?>
        {
            ["email"] = "user@example.com",
            ["count"] = 42
        };

        var result = engine.RedactProperties(props);

        ((string)result["email"]!).Should().Contain("***EMAIL***");
        result["count"].Should().Be(42);
    }

    #endregion

    #region Test DTOs

    public class SensitiveDto
    {
        [Sensitive(SensitivityMode.Mask, keepLast: 4)]
        public string? AccountNumber { get; set; }

        public string? Name { get; set; }
    }

    public class DoNotLogDto
    {
        public string? PublicField { get; set; }

        [DoNotLog]
        public string? SecretField { get; set; }
    }

    public class MaskDto
    {
        [Mask(@"\d{4}", "XXXX")]
        public string? CardNumber { get; set; }
    }

    public class RedactModeDto
    {
        [Sensitive(SensitivityMode.Redact)]
        public string? Secret { get; set; }
    }

    #endregion
}
