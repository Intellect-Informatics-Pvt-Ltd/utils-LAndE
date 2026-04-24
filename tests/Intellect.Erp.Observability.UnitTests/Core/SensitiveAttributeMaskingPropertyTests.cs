using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core;
using Microsoft.Extensions.Options;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Core;

/// <summary>
/// Property-based test: Sensitive attribute masking — for any string with [Sensitive(keepLast=N)],
/// last N chars match original and preceding chars are masked.
/// **Validates: Requirements 6.3**
/// </summary>
public class SensitiveAttributeMaskingPropertyTests
{
    /// <summary>
    /// Property 10: For any string value on a property annotated with [Sensitive(keepLast=N)],
    /// the redacted output has the last N characters matching the original and all preceding
    /// characters are '*'.
    /// **Validates: Requirements 6.3**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(SensitiveMaskingArbitrary) }, MaxTest = 100)]
    public Property LastNChars_MatchOriginal_AndPrecedingAreMasked(SensitiveMaskingInput input)
    {
        var engine = CreateEngine();

        // Create a DTO dynamically with the given keepLast value
        // We use the fixed KeepLast4Dto since attribute values must be compile-time constants
        // and test with keepLast=4 (the default)
        var dto = new KeepLast4Dto { Value = input.Value };
        var result = (KeepLast4Dto)engine.RedactObject(dto);

        if (input.Value.Length <= 4)
        {
            // When string is shorter than or equal to keepLast, the entire string is returned
            return (result.Value == input.Value)
                .Label($"Short string '{input.Value}' should be returned as-is, got '{result.Value}'");
        }

        var keepLast = 4;
        var expectedTail = input.Value[^keepLast..];
        var actualTail = result.Value![^keepLast..];
        var maskedPrefix = result.Value[..^keepLast];

        var tailMatches = actualTail == expectedTail;
        var prefixAllMasked = maskedPrefix.All(c => c == '*');

        return (tailMatches && prefixAllMasked)
            .Label($"Input='{input.Value}', Result='{result.Value}', " +
                   $"TailMatch={tailMatches}, PrefixMasked={prefixAllMasked}");
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

    public class SensitiveMaskingInput
    {
        public string Value { get; set; } = "";
    }

    public static class SensitiveMaskingArbitrary
    {
        public static Arbitrary<SensitiveMaskingInput> InputArb()
        {
            // Generate alphanumeric strings to avoid regex pattern interference
            var charGen = Gen.Elements(
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
                'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
                'u', 'v', 'w', 'x', 'y', 'z',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H');

            var gen = from length in Gen.Choose(1, 30)
                      from chars in Gen.ArrayOf(length, charGen)
                      select new SensitiveMaskingInput
                      {
                          Value = new string(chars)
                      };

            return Arb.From(gen);
        }
    }

    public class KeepLast4Dto
    {
        [Sensitive(SensitivityMode.Mask, keepLast: 4)]
        public string? Value { get; set; }
    }
}
