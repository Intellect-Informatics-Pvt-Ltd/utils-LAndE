using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core;
using Microsoft.Extensions.Options;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Core;

/// <summary>
/// Property-based test: DoNotLog exclusion — for any value on a [DoNotLog] property,
/// the field is absent (null/default) from redacted output.
/// **Validates: Requirements 6.4**
/// </summary>
public class DoNotLogExclusionPropertyTests
{
    /// <summary>
    /// Property 11: For any value on a property annotated with [DoNotLog], the redacted
    /// output has that field set to null/default.
    /// **Validates: Requirements 6.4**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(DoNotLogArbitrary) }, MaxTest = 100)]
    public Property DoNotLogField_IsNullInRedactedOutput(DoNotLogInput input)
    {
        var engine = CreateEngine();
        var dto = new DoNotLogTestDto
        {
            PublicField = input.PublicValue,
            SecretField = input.SecretValue
        };

        var result = (DoNotLogTestDto)engine.RedactObject(dto);

        var secretIsNull = result.SecretField == null;
        var publicPreserved = result.PublicField != null;

        return (secretIsNull && publicPreserved)
            .Label($"SecretField should be null (was '{result.SecretField}'), " +
                   $"PublicField should be non-null (was '{result.PublicField}')");
    }

    private static DefaultRedactionEngine CreateEngine()
    {
        var options = Options.Create(new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = "TestModule",
            Masking = new MaskingOptions
            {
                Enabled = true,
                Paths = [],
                Regexes = []
            }
        });
        return new DefaultRedactionEngine(options);
    }

    public class DoNotLogInput
    {
        public string PublicValue { get; set; } = "";
        public string SecretValue { get; set; } = "";
    }

    public static class DoNotLogArbitrary
    {
        public static Arbitrary<DoNotLogInput> InputArb()
        {
            // Use simple alpha strings to avoid regex pattern interference
            var charGen = Gen.Elements(
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
                'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't');

            var strGen = from length in Gen.Choose(1, 20)
                         from chars in Gen.ArrayOf(length, charGen)
                         select new string(chars);

            var gen = from pub in strGen
                      from secret in strGen
                      select new DoNotLogInput
                      {
                          PublicValue = pub,
                          SecretValue = secret
                      };

            return Arb.From(gen);
        }
    }

    public class DoNotLogTestDto
    {
        public string? PublicField { get; set; }

        [DoNotLog]
        public string? SecretField { get; set; }
    }
}
