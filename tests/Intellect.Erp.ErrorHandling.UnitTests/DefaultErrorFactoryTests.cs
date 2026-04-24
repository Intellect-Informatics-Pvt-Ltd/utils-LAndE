using FluentAssertions;
using Intellect.Erp.ErrorHandling.Exceptions;
using Intellect.Erp.Observability.Abstractions;
using Xunit;

namespace Intellect.Erp.ErrorHandling.UnitTests;

/// <summary>
/// Unit tests for <see cref="DefaultErrorFactory"/> — all factory methods,
/// correlation ID stamping, and FromCatalog with known/unknown codes.
/// </summary>
public class DefaultErrorFactoryTests
{
    private const string TestCorrelationId = "01HX1234567890ABCDEFGHIJKL";

    private static DefaultErrorFactory CreateFactory(
        string? correlationId = TestCorrelationId,
        IErrorCatalog? catalog = null)
    {
        var accessor = new StubCorrelationContextAccessor(correlationId);
        catalog ??= CreateDefaultCatalog();
        return new DefaultErrorFactory(accessor, catalog);
    }

    private static InMemoryErrorCatalog CreateDefaultCatalog()
    {
        var entries = YamlErrorCatalogLoader.Load(new StringReader(CoreYaml));
        return new InMemoryErrorCatalog(entries);
    }

    private const string CoreYaml = """
        errors:
          - code: "ERP-CORE-SYS-0001"
            title: "Unhandled system error"
            userMessage: "An unexpected error occurred."
            httpStatus: 500
            severity: "Error"
            retryable: false
            category: "System"
          - code: "ERP-CORE-VAL-0001"
            title: "Validation failed"
            userMessage: "One or more fields are invalid."
            httpStatus: 400
            severity: "Warning"
            retryable: false
            category: "Validation"
          - code: "ERP-CORE-BIZ-0001"
            title: "Business rule violation"
            userMessage: "A business rule was violated."
            httpStatus: 422
            severity: "Warning"
            retryable: false
            category: "Business"
        """;

    // ── Factory method type checks ──

    [Fact]
    public void Validation_creates_ValidationException()
    {
        var factory = CreateFactory();
        var ex = factory.Validation("bad input");
        ex.Should().BeOfType<ValidationException>();
        ex.ErrorCode.Should().Be(ValidationException.DefaultCode);
    }

    [Fact]
    public void Validation_with_field_errors_preserves_them()
    {
        var factory = CreateFactory();
        var fields = new[] { new FieldError("Email", "INVALID", "Invalid email") };
        var ex = factory.Validation("bad input", fields);
        ex.Should().BeOfType<ValidationException>();
        ((ValidationException)ex).FieldErrors.Should().HaveCount(1);
    }

    [Fact]
    public void BusinessRule_creates_BusinessRuleException()
    {
        var factory = CreateFactory();
        var ex = factory.BusinessRule("rule violated");
        ex.Should().BeOfType<BusinessRuleException>();
        ex.ErrorCode.Should().Be(BusinessRuleException.DefaultCode);
    }

    [Fact]
    public void NotFound_creates_NotFoundException()
    {
        var factory = CreateFactory();
        var ex = factory.NotFound("not found");
        ex.Should().BeOfType<NotFoundException>();
        ex.ErrorCode.Should().Be(NotFoundException.DefaultCode);
    }

    [Fact]
    public void Conflict_creates_ConflictException()
    {
        var factory = CreateFactory();
        var ex = factory.Conflict("conflict");
        ex.Should().BeOfType<ConflictException>();
        ex.ErrorCode.Should().Be(ConflictException.DefaultCode);
    }

    [Fact]
    public void Unauthorized_creates_UnauthorizedException()
    {
        var factory = CreateFactory();
        var ex = factory.Unauthorized("unauthorized");
        ex.Should().BeOfType<UnauthorizedException>();
        ex.ErrorCode.Should().Be(UnauthorizedException.DefaultCode);
    }

    [Fact]
    public void Forbidden_creates_ForbiddenException()
    {
        var factory = CreateFactory();
        var ex = factory.Forbidden("forbidden");
        ex.Should().BeOfType<ForbiddenException>();
        ex.ErrorCode.Should().Be(ForbiddenException.DefaultCode);
    }

    [Fact]
    public void Integration_creates_IntegrationException()
    {
        var factory = CreateFactory();
        var ex = factory.Integration("integration failure");
        ex.Should().BeOfType<IntegrationException>();
        ex.ErrorCode.Should().Be(IntegrationException.DefaultCode);
    }

    [Fact]
    public void Dependency_creates_DependencyException()
    {
        var factory = CreateFactory();
        var ex = factory.Dependency("dependency down");
        ex.Should().BeOfType<DependencyException>();
        ex.ErrorCode.Should().Be(DependencyException.DefaultCode);
    }

    [Fact]
    public void DataIntegrity_creates_DataIntegrityException()
    {
        var factory = CreateFactory();
        var ex = factory.DataIntegrity("data error");
        ex.Should().BeOfType<DataIntegrityException>();
        ex.ErrorCode.Should().Be(DataIntegrityException.DefaultCode);
    }

    [Fact]
    public void Concurrency_creates_ConcurrencyException()
    {
        var factory = CreateFactory();
        var ex = factory.Concurrency("concurrency conflict");
        ex.Should().BeOfType<ConcurrencyException>();
        ex.ErrorCode.Should().Be(ConcurrencyException.DefaultCode);
    }

    [Fact]
    public void ExternalSystem_creates_ExternalSystemException()
    {
        var factory = CreateFactory();
        var ex = factory.ExternalSystem("external failure");
        ex.Should().BeOfType<ExternalSystemException>();
        ex.ErrorCode.Should().Be(ExternalSystemException.DefaultCode);
    }

    [Fact]
    public void System_creates_SystemException()
    {
        var factory = CreateFactory();
        var ex = factory.System("system error");
        ex.Should().BeOfType<Exceptions.SystemException>();
        ex.ErrorCode.Should().Be(Exceptions.SystemException.DefaultCode);
    }

    // ── Correlation ID stamping ──

    [Fact]
    public void Factory_stamps_correlation_id_from_accessor()
    {
        var factory = CreateFactory(correlationId: "my-correlation-123");
        var ex = factory.NotFound("not found");
        ex.CorrelationId.Should().Be("my-correlation-123");
    }

    [Fact]
    public void Factory_stamps_null_correlation_id_when_accessor_returns_null()
    {
        var factory = CreateFactory(correlationId: null);
        var ex = factory.NotFound("not found");
        ex.CorrelationId.Should().BeNull();
    }

    // ── FromCatalog with known code ──

    [Fact]
    public void FromCatalog_with_known_validation_code_creates_ValidationException()
    {
        var factory = CreateFactory();
        var ex = factory.FromCatalog("ERP-CORE-VAL-0001");
        ex.Should().BeOfType<ValidationException>();
        ex.ErrorCode.Should().Be("ERP-CORE-VAL-0001");
        ex.Category.Should().Be(ErrorCategory.Validation);
    }

    [Fact]
    public void FromCatalog_with_known_system_code_creates_SystemException()
    {
        var factory = CreateFactory();
        var ex = factory.FromCatalog("ERP-CORE-SYS-0001");
        ex.Should().BeOfType<Exceptions.SystemException>();
        ex.ErrorCode.Should().Be("ERP-CORE-SYS-0001");
    }

    [Fact]
    public void FromCatalog_with_known_business_code_creates_BusinessRuleException()
    {
        var factory = CreateFactory();
        var ex = factory.FromCatalog("ERP-CORE-BIZ-0001");
        ex.Should().BeOfType<BusinessRuleException>();
        ex.ErrorCode.Should().Be("ERP-CORE-BIZ-0001");
    }

    [Fact]
    public void FromCatalog_uses_catalog_user_message_when_no_override()
    {
        var factory = CreateFactory();
        var ex = factory.FromCatalog("ERP-CORE-VAL-0001");
        ex.Message.Should().Be("One or more fields are invalid.");
    }

    [Fact]
    public void FromCatalog_uses_override_message_when_provided()
    {
        var factory = CreateFactory();
        var ex = factory.FromCatalog("ERP-CORE-VAL-0001", message: "Custom message");
        ex.Message.Should().Be("Custom message");
    }

    [Fact]
    public void FromCatalog_stamps_correlation_id()
    {
        var factory = CreateFactory(correlationId: "catalog-corr-id");
        var ex = factory.FromCatalog("ERP-CORE-SYS-0001");
        ex.CorrelationId.Should().Be("catalog-corr-id");
    }

    // ── FromCatalog with unknown code falls back to ERP-CORE-SYS-0001 ──

    [Fact]
    public void FromCatalog_with_unknown_code_falls_back_to_default()
    {
        var factory = CreateFactory();
        var ex = factory.FromCatalog("ERP-CORE-SYS-9999");
        ex.ErrorCode.Should().Be("ERP-CORE-SYS-0001");
        ex.Should().BeOfType<Exceptions.SystemException>();
    }

    // ── Inner exception forwarding ──

    [Fact]
    public void Factory_methods_preserve_inner_exception()
    {
        var factory = CreateFactory();
        var inner = new InvalidOperationException("inner");
        var ex = factory.Validation("msg", innerException: inner);
        ex.InnerException.Should().BeSameAs(inner);
    }

    // ── Stub ──

    private sealed class StubCorrelationContextAccessor : ICorrelationContextAccessor
    {
        public StubCorrelationContextAccessor(string? correlationId) => CorrelationId = correlationId;
        public string? CorrelationId { get; }
        public string? CausationId => null;
        public string? TraceParent => null;
    }
}
