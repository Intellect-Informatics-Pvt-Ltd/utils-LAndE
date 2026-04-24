using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Core;
using Microsoft.Extensions.Options;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Core;

/// <summary>
/// Property-based test: Redaction non-mutation — for any input object, original property
/// values remain unchanged after RedactObject.
/// **Validates: Requirements 6.8**
/// </summary>
public class RedactionNonMutationPropertyTests
{
    /// <summary>
    /// Property 12: For any input object passed to RedactObject, the original object's
    /// property values remain unchanged after redaction.
    /// **Validates: Requirements 6.8**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(NonMutationArbitrary) }, MaxTest = 100)]
    public Property OriginalObject_RemainsUnchanged_AfterRedaction(NonMutationInput input)
    {
        var engine = CreateEngine();
        var dto = new MixedDto
        {
            SensitiveField = input.SensitiveValue,
            SecretField = input.SecretValue,
            NormalField = input.NormalValue
        };

        // Snapshot original values
        var origSensitive = dto.SensitiveField;
        var origSecret = dto.SecretField;
        var origNormal = dto.NormalField;

        // Perform redaction
        _ = engine.RedactObject(dto);

        // Verify original is unchanged
        var sensitiveUnchanged = dto.SensitiveField == origSensitive;
        var secretUnchanged = dto.SecretField == origSecret;
        var normalUnchanged = dto.NormalField == origNormal;

        return (sensitiveUnchanged && secretUnchanged && normalUnchanged)
            .Label($"SensitiveUnchanged={sensitiveUnchanged}, SecretUnchanged={secretUnchanged}, " +
                   $"NormalUnchanged={normalUnchanged}");
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

    public class NonMutationInput
    {
        public string SensitiveValue { get; set; } = "";
        public string SecretValue { get; set; } = "";
        public string NormalValue { get; set; } = "";
    }

    public static class NonMutationArbitrary
    {
        public static Arbitrary<NonMutationInput> InputArb()
        {
            // Use simple alpha strings to avoid regex pattern interference
            var charGen = Gen.Elements(
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
                'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't');

            var strGen = from length in Gen.Choose(1, 20)
                         from chars in Gen.ArrayOf(length, charGen)
                         select new string(chars);

            var gen = from sensitive in strGen
                      from secret in strGen
                      from normal in strGen
                      select new NonMutationInput
                      {
                          SensitiveValue = sensitive,
                          SecretValue = secret,
                          NormalValue = normal
                      };

            return Arb.From(gen);
        }
    }

    public class MixedDto
    {
        [Sensitive(SensitivityMode.Mask, keepLast: 4)]
        public string? SensitiveField { get; set; }

        [DoNotLog]
        public string? SecretField { get; set; }

        public string? NormalField { get; set; }
    }
}
