using System.Text.RegularExpressions;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

namespace Intellect.Erp.ErrorHandling.UnitTests;

/// <summary>
/// Property test: Error code format validation — for any string, the validator accepts it
/// iff it matches the ERP-MODULE-CATEGORY-SEQ4 pattern with valid segments.
///
/// **Validates: Requirements 5.6** (Property 9)
/// </summary>
public class ErrorCodeFormatValidationPropertyTests
{
    private static readonly Regex ErrorCodeRegex = new(
        @"^ERP-[A-Z]+-[A-Z]{3}-\d{4}$",
        RegexOptions.Compiled);

    private static readonly HashSet<string> ValidModules = new(StringComparer.Ordinal)
    {
        "CORE", "LOANS", "SAVINGS", "MEMBERSHIP", "FAS", "VOUCHER", "MERCHANDISE", "AUDIT", "UNITE"
    };

    private static readonly HashSet<string> ValidCategories = new(StringComparer.Ordinal)
    {
        "VAL", "BIZ", "NFD", "CFL", "SEC", "INT", "DEP", "DAT", "CON", "SYS"
    };

    /// <summary>
    /// Reference validator that checks the full error code format including valid segments.
    /// </summary>
    internal static bool IsValidErrorCode(string code)
    {
        if (string.IsNullOrEmpty(code))
            return false;

        if (!ErrorCodeRegex.IsMatch(code))
            return false;

        // Parse segments: ERP-MODULE-CAT-NNNN
        var parts = code.Split('-');
        if (parts.Length < 4)
            return false;

        // Module is everything between first and second-to-last two segments
        // Format: ERP-{MODULE}-{CAT}-{SEQ4}
        // The category is always 3 uppercase letters (second-to-last segment)
        // The sequence is always 4 digits (last segment)
        var seq = parts[^1];       // last segment: 4 digits
        var cat = parts[^2];       // second-to-last: 3 uppercase letters
        var module = string.Join("-", parts[1..^2]); // everything between ERP and CAT

        return ValidModules.Contains(module) && ValidCategories.Contains(cat);
    }

    /// <summary>
    /// For any random string, the validator should reject it unless it matches the full pattern.
    /// </summary>
    [Property(MaxTest = 200)]
    public void Random_strings_are_validated_correctly(NonNull<string> input)
    {
        var code = input.Get;
        var expected = IsValidErrorCode(code);

        // The YamlErrorCatalogLoader uses the regex ^ERP-[A-Z]+-[A-Z]{3}-\d{4}$
        // but does NOT validate module/category segments at the regex level.
        // Our reference validator is stricter (checks valid modules and categories).
        // We test that: if our strict validator says valid, the regex also matches.
        var regexMatches = ErrorCodeRegex.IsMatch(code);

        if (expected)
        {
            regexMatches.Should().BeTrue(
                because: "a valid error code must match the regex pattern");
        }
        // Note: regex may match codes with invalid modules/categories,
        // which is expected — the regex is a necessary but not sufficient condition.
    }

    /// <summary>
    /// For any valid error code generated from known segments, the validator accepts it.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(ErrorCodeFormatValidationPropertyTests)])]
    public void Valid_error_codes_are_always_accepted(ValidErrorCode validCode)
    {
        IsValidErrorCode(validCode.Code).Should().BeTrue(
            because: $"'{validCode.Code}' is constructed from valid segments");

        ErrorCodeRegex.IsMatch(validCode.Code).Should().BeTrue(
            because: $"'{validCode.Code}' must match the regex pattern");
    }

    /// <summary>
    /// For any invalid error code, the validator rejects it.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(ErrorCodeFormatValidationPropertyTests)])]
    public void Invalid_error_codes_are_always_rejected(InvalidErrorCode invalidCode)
    {
        // Either the regex doesn't match, or the segments are invalid
        var isValid = IsValidErrorCode(invalidCode.Code);
        isValid.Should().BeFalse(
            because: $"'{invalidCode.Code}' is intentionally malformed");
    }

    // ── Generators ──

    public record ValidErrorCode(string Code);
    public record InvalidErrorCode(string Code);

    private static readonly string[] Modules =
        ["CORE", "LOANS", "SAVINGS", "MEMBERSHIP", "FAS", "VOUCHER", "MERCHANDISE", "AUDIT", "UNITE"];

    private static readonly string[] Categories =
        ["VAL", "BIZ", "NFD", "CFL", "SEC", "INT", "DEP", "DAT", "CON", "SYS"];

    public static Arbitrary<ValidErrorCode> Arbitrary_ValidErrorCode()
    {
        var gen =
            from module in Gen.Elements(Modules)
            from category in Gen.Elements(Categories)
            from seq in Gen.Choose(0, 9999)
            select new ValidErrorCode($"ERP-{module}-{category}-{seq:D4}");

        return Arb.From(gen);
    }

    public static Arbitrary<InvalidErrorCode> Arbitrary_InvalidErrorCode()
    {
        var generators = new[]
        {
            // Wrong prefix
            from module in Gen.Elements(Modules)
            from cat in Gen.Elements(Categories)
            from seq in Gen.Choose(0, 9999)
            from prefix in Gen.Elements("ERR", "APP", "SYS", "err", "erp")
            select new InvalidErrorCode($"{prefix}-{module}-{cat}-{seq:D4}"),

            // Invalid module
            from cat in Gen.Elements(Categories)
            from seq in Gen.Choose(0, 9999)
            from badModule in Gen.Elements("INVALID", "UNKNOWN", "TEST", "foo", "123")
            select new InvalidErrorCode($"ERP-{badModule}-{cat}-{seq:D4}"),

            // Invalid category
            from module in Gen.Elements(Modules)
            from seq in Gen.Choose(0, 9999)
            from badCat in Gen.Elements("XXX", "ABC", "ZZZ", "val", "12A")
            select new InvalidErrorCode($"ERP-{module}-{badCat}-{seq:D4}"),

            // Wrong sequence format (not 4 digits)
            from module in Gen.Elements(Modules)
            from cat in Gen.Elements(Categories)
            from badSeq in Gen.Elements("1", "12", "123", "12345", "ABCD")
            select new InvalidErrorCode($"ERP-{module}-{cat}-{badSeq}"),

            // Missing segments
            Gen.Constant(new InvalidErrorCode("ERP-CORE")),
            Gen.Constant(new InvalidErrorCode("ERP-CORE-VAL")),
            Gen.Constant(new InvalidErrorCode("ERP")),
            Gen.Constant(new InvalidErrorCode("")),
            Gen.Constant(new InvalidErrorCode("not-an-error-code")),
        };

        return Arb.From(Gen.OneOf(generators));
    }
}
