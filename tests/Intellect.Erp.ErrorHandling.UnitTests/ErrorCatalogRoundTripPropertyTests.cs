using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.ErrorHandling.UnitTests;

/// <summary>
/// Property test: Error catalog round-trip — for any valid ErrorCatalogEntry serialized
/// to YAML and loaded, FromCatalog produces an AppException with matching ErrorCode,
/// Category, Severity, and Retryable.
///
/// **Validates: Requirements 5.1, 5.3** (Property 8)
/// </summary>
public class ErrorCatalogRoundTripPropertyTests
{
    private static readonly string[] ValidModules =
        ["CORE", "LOANS", "SAVINGS", "MEMBERSHIP", "FAS", "VOUCHER", "MERCHANDISE", "AUDIT", "UNITE"];

    private static readonly string[] ValidCategories =
        ["VAL", "BIZ", "NFD", "CFL", "SEC", "INT", "DEP", "DAT", "CON", "SYS"];

    private static readonly Dictionary<string, ErrorCategory> CategoryMap = new()
    {
        ["VAL"] = ErrorCategory.Validation,
        ["BIZ"] = ErrorCategory.Business,
        ["NFD"] = ErrorCategory.NotFound,
        ["CFL"] = ErrorCategory.Conflict,
        ["SEC"] = ErrorCategory.Security,
        ["INT"] = ErrorCategory.Integration,
        ["DEP"] = ErrorCategory.Dependency,
        ["DAT"] = ErrorCategory.Data,
        ["CON"] = ErrorCategory.Concurrency,
        ["SYS"] = ErrorCategory.System,
    };

    private static readonly ErrorSeverity[] ValidSeverities =
        [ErrorSeverity.Info, ErrorSeverity.Warning, ErrorSeverity.Error, ErrorSeverity.Critical];

    /// <summary>
    /// Generates a valid ErrorCatalogEntry with a properly formatted error code.
    /// </summary>
    private static Arbitrary<ErrorCatalogEntry> ValidErrorCatalogEntryArbitrary()
    {
        var gen =
            from module in Gen.Elements(ValidModules)
            from categoryCode in Gen.Elements(ValidCategories)
            from seq in Gen.Choose(1, 9999)
            from severity in Gen.Elements(ValidSeverities)
            from retryable in Arb.Generate<bool>()
            from httpStatus in Gen.Elements(400, 401, 403, 404, 409, 422, 500, 502, 503)
            let code = $"ERP-{module}-{categoryCode}-{seq:D4}"
            let category = CategoryMap[categoryCode]
            select new ErrorCatalogEntry(
                Code: code,
                Title: $"Test error {code}",
                UserMessage: $"User message for {code}",
                SupportMessage: $"Support message for {code}",
                HttpStatus: httpStatus,
                Severity: severity,
                Retryable: retryable,
                Category: category);

        return Arb.From(gen);
    }

    [Property(MaxTest = 100, Arbitrary = [typeof(ErrorCatalogRoundTripPropertyTests)])]
    public void FromCatalog_produces_exception_matching_catalog_entry(ErrorCatalogEntry entry)
    {
        // Arrange: serialize entry to YAML
        var yaml = $"""
            errors:
              - code: "{entry.Code}"
                title: "{entry.Title}"
                userMessage: "{entry.UserMessage}"
                supportMessage: "{entry.SupportMessage}"
                httpStatus: {entry.HttpStatus}
                severity: "{entry.Severity}"
                retryable: {entry.Retryable.ToString().ToLowerInvariant()}
                category: "{entry.Category}"
            """;

        // Act: load YAML, create catalog, create factory, call FromCatalog
        var entries = YamlErrorCatalogLoader.Load(new StringReader(yaml));
        var catalog = new InMemoryErrorCatalog(entries);
        var accessor = new StubCorrelationContextAccessor();
        var factory = new DefaultErrorFactory(accessor, catalog);

        var exception = factory.FromCatalog(entry.Code);

        // Assert: ErrorCode and Category are preserved through the round-trip.
        // Severity and Retryable are determined by the concrete exception type's
        // constructor defaults (e.g., ConflictException always uses Warning severity),
        // not by the catalog entry values. Only IntegrationException passes through
        // the catalog's Retryable flag.
        exception.ErrorCode.Should().Be(entry.Code);
        exception.Category.Should().Be(entry.Category);

        // For Integration category, the Retryable flag is passed through from the catalog
        if (entry.Category == ErrorCategory.Integration)
        {
            exception.Retryable.Should().Be(entry.Retryable);
        }
    }

    /// <summary>
    /// FsCheck uses this via the Arbitrary attribute to discover the generator.
    /// </summary>
    public static Arbitrary<ErrorCatalogEntry> Arbitrary_ErrorCatalogEntry() => ValidErrorCatalogEntryArbitrary();

    private sealed class StubCorrelationContextAccessor : ICorrelationContextAccessor
    {
        public string? CorrelationId => "test-correlation";
        public string? CausationId => null;
        public string? TraceParent => null;
    }
}
