using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Xunit;

namespace Intellect.Erp.ErrorHandling.UnitTests;

/// <summary>
/// Unit tests for <see cref="YamlErrorCatalogLoader"/> — valid YAML, invalid schema,
/// missing fields, duplicate codes, and empty YAML.
/// </summary>
public class YamlErrorCatalogLoaderTests
{
    // ── Valid YAML produces correct entries ──

    [Fact]
    public void Load_valid_yaml_produces_correct_entries()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                title: "Unhandled system error"
                userMessage: "An unexpected error occurred."
                supportMessage: "Check logs for correlation ID."
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "System"
              - code: "ERP-CORE-VAL-0001"
                title: "Validation failed"
                userMessage: "One or more fields are invalid."
                supportMessage: "See fieldErrors for details."
                httpStatus: 400
                severity: "Warning"
                retryable: false
                category: "Validation"
            """;

        var entries = YamlErrorCatalogLoader.Load(new StringReader(yaml));

        entries.Should().HaveCount(2);

        entries[0].Code.Should().Be("ERP-CORE-SYS-0001");
        entries[0].Title.Should().Be("Unhandled system error");
        entries[0].UserMessage.Should().Be("An unexpected error occurred.");
        entries[0].SupportMessage.Should().Be("Check logs for correlation ID.");
        entries[0].HttpStatus.Should().Be(500);
        entries[0].Severity.Should().Be(ErrorSeverity.Error);
        entries[0].Retryable.Should().BeFalse();
        entries[0].Category.Should().Be(ErrorCategory.System);

        entries[1].Code.Should().Be("ERP-CORE-VAL-0001");
        entries[1].Category.Should().Be(ErrorCategory.Validation);
        entries[1].Severity.Should().Be(ErrorSeverity.Warning);
    }

    [Fact]
    public void Load_valid_yaml_with_support_message_omitted_defaults_to_empty()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                title: "System error"
                userMessage: "An error occurred."
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "System"
            """;

        var entries = YamlErrorCatalogLoader.Load(new StringReader(yaml));

        entries.Should().HaveCount(1);
        entries[0].SupportMessage.Should().BeEmpty();
    }

    // ── Invalid error code format throws ──

    [Fact]
    public void Load_invalid_error_code_format_throws()
    {
        var yaml = """
            errors:
              - code: "INVALID-CODE"
                title: "Bad code"
                userMessage: "Bad"
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "System"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not match the required format*");
    }

    [Fact]
    public void Load_lowercase_error_code_throws()
    {
        var yaml = """
            errors:
              - code: "erp-core-sys-0001"
                title: "Bad code"
                userMessage: "Bad"
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "System"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not match the required format*");
    }

    // ── Missing required fields throws ──

    [Fact]
    public void Load_missing_code_throws()
    {
        var yaml = """
            errors:
              - title: "No code"
                userMessage: "Missing code"
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "System"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing required fields*code*");
    }

    [Fact]
    public void Load_missing_title_throws()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                userMessage: "Missing title"
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "System"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing required fields*title*");
    }

    [Fact]
    public void Load_missing_userMessage_throws()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                title: "System error"
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "System"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing required fields*userMessage*");
    }

    [Fact]
    public void Load_missing_httpStatus_throws()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                title: "System error"
                userMessage: "An error occurred."
                severity: "Error"
                retryable: false
                category: "System"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing required fields*httpStatus*");
    }

    [Fact]
    public void Load_missing_severity_throws()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                title: "System error"
                userMessage: "An error occurred."
                httpStatus: 500
                retryable: false
                category: "System"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing required fields*severity*");
    }

    [Fact]
    public void Load_missing_category_throws()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                title: "System error"
                userMessage: "An error occurred."
                httpStatus: 500
                severity: "Error"
                retryable: false
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing required fields*category*");
    }

    [Fact]
    public void Load_missing_multiple_fields_reports_all()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing required fields*");
    }

    // ── Duplicate codes throws ──

    [Fact]
    public void Load_duplicate_codes_throws()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                title: "First"
                userMessage: "First message"
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "System"
              - code: "ERP-CORE-SYS-0001"
                title: "Duplicate"
                userMessage: "Duplicate message"
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "System"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate error code*ERP-CORE-SYS-0001*");
    }

    // ── Empty YAML returns empty list ──

    [Fact]
    public void Load_empty_yaml_returns_empty_list()
    {
        var yaml = """
            errors:
            """;

        var entries = YamlErrorCatalogLoader.Load(new StringReader(yaml));

        entries.Should().BeEmpty();
    }

    [Fact]
    public void Load_yaml_with_no_errors_key_returns_empty_list()
    {
        var yaml = "# empty document";

        var entries = YamlErrorCatalogLoader.Load(new StringReader(yaml));

        entries.Should().BeEmpty();
    }

    // ── Invalid severity value throws ──

    [Fact]
    public void Load_invalid_severity_value_throws()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                title: "System error"
                userMessage: "An error occurred."
                httpStatus: 500
                severity: "SuperCritical"
                retryable: false
                category: "System"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid severity*");
    }

    // ── Invalid category value throws ──

    [Fact]
    public void Load_invalid_category_value_throws()
    {
        var yaml = """
            errors:
              - code: "ERP-CORE-SYS-0001"
                title: "System error"
                userMessage: "An error occurred."
                httpStatus: 500
                severity: "Error"
                retryable: false
                category: "Unknown"
            """;

        var act = () => YamlErrorCatalogLoader.Load(new StringReader(yaml));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid category*");
    }

    // ── Null reader throws ──

    [Fact]
    public void Load_null_reader_throws_ArgumentNullException()
    {
        var act = () => YamlErrorCatalogLoader.Load((TextReader)null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
