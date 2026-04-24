using FluentAssertions;
using Intellect.Erp.ErrorHandling.Exceptions;
using Intellect.Erp.Observability.Abstractions;
using Xunit;

namespace Intellect.Erp.ErrorHandling.UnitTests;

/// <summary>
/// Unit tests for the exception hierarchy — type checks, property defaults,
/// interface implementations, and base class behavior.
/// </summary>
public class ExceptionHierarchyTests
{
    // ── All 12 exception types extend AppException ──

    [Theory]
    [MemberData(nameof(AllExceptionInstances))]
    public void All_exception_types_extend_AppException(AppException exception, string _)
    {
        exception.Should().BeAssignableTo<AppException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    // ── Default error codes ──

    [Fact]
    public void ValidationException_has_correct_default_code()
        => new ValidationException("msg").ErrorCode.Should().Be("ERP-CORE-VAL-0001");

    [Fact]
    public void BusinessRuleException_has_correct_default_code()
        => new BusinessRuleException("msg").ErrorCode.Should().Be("ERP-CORE-BIZ-0001");

    [Fact]
    public void NotFoundException_has_correct_default_code()
        => new NotFoundException("msg").ErrorCode.Should().Be("ERP-CORE-NFD-0001");

    [Fact]
    public void ConflictException_has_correct_default_code()
        => new ConflictException("msg").ErrorCode.Should().Be("ERP-CORE-CFL-0001");

    [Fact]
    public void UnauthorizedException_has_correct_default_code()
        => new UnauthorizedException("msg").ErrorCode.Should().Be("ERP-CORE-SEC-0001");

    [Fact]
    public void ForbiddenException_has_correct_default_code()
        => new ForbiddenException("msg").ErrorCode.Should().Be("ERP-CORE-SEC-0002");

    [Fact]
    public void IntegrationException_has_correct_default_code()
        => new IntegrationException("msg").ErrorCode.Should().Be("ERP-CORE-INT-0001");

    [Fact]
    public void DependencyException_has_correct_default_code()
        => new DependencyException("msg").ErrorCode.Should().Be("ERP-CORE-DEP-0001");

    [Fact]
    public void DataIntegrityException_has_correct_default_code()
        => new DataIntegrityException("msg").ErrorCode.Should().Be("ERP-CORE-DAT-0001");

    [Fact]
    public void ConcurrencyException_has_correct_default_code()
        => new ConcurrencyException("msg").ErrorCode.Should().Be("ERP-CORE-CON-0001");

    [Fact]
    public void ExternalSystemException_has_correct_default_code()
        => new ExternalSystemException("msg").ErrorCode.Should().Be("ERP-CORE-INT-0002");

    [Fact]
    public void SystemException_has_correct_default_code()
        => new Exceptions.SystemException("msg").ErrorCode.Should().Be("ERP-CORE-SYS-0001");

    // ── Default Category, Severity, Retryable ──

    [Theory]
    [MemberData(nameof(DefaultPropertyData))]
    public void Exception_has_correct_default_properties(
        AppException exception,
        ErrorCategory expectedCategory,
        ErrorSeverity expectedSeverity,
        bool expectedRetryable)
    {
        exception.Category.Should().Be(expectedCategory);
        exception.Severity.Should().Be(expectedSeverity);
        exception.Retryable.Should().Be(expectedRetryable);
    }

    // ── ValidationException has FieldErrors property ──

    [Fact]
    public void ValidationException_has_empty_FieldErrors_by_default()
    {
        var ex = new ValidationException("msg");
        ex.FieldErrors.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ValidationException_preserves_FieldErrors()
    {
        var errors = new[] { new FieldError("Name", "REQUIRED", "Name is required") };
        var ex = new ValidationException("msg", errors);
        ex.FieldErrors.Should().HaveCount(1);
        ex.FieldErrors[0].Field.Should().Be("Name");
    }

    // ── BusinessRuleException implements IDomainPolicyRejectionException ──

    [Fact]
    public void BusinessRuleException_implements_IDomainPolicyRejectionException()
    {
        var ex = new BusinessRuleException("msg");
        ex.Should().BeAssignableTo<IDomainPolicyRejectionException>();
    }

    // ── ConcurrencyException implements ISagaCompensationException ──

    [Fact]
    public void ConcurrencyException_implements_ISagaCompensationException()
    {
        var ex = new ConcurrencyException("msg");
        ex.Should().BeAssignableTo<ISagaCompensationException>();
    }

    // ── CorrelationId can be set ──

    [Fact]
    public void CorrelationId_is_null_by_default()
    {
        var ex = new Exceptions.SystemException("msg");
        ex.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void CorrelationId_can_be_set()
    {
        var ex = new Exceptions.SystemException("msg");
        ex.CorrelationId = "test-correlation-id";
        ex.CorrelationId.Should().Be("test-correlation-id");
    }

    // ── Inner exception is preserved ──

    [Fact]
    public void Inner_exception_is_preserved()
    {
        var inner = new InvalidOperationException("inner error");
        var ex = new BusinessRuleException("outer", innerException: inner);
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Inner_exception_is_null_by_default()
    {
        var ex = new NotFoundException("msg");
        ex.InnerException.Should().BeNull();
    }

    // ── Custom error code override ──

    [Fact]
    public void Error_code_can_be_overridden()
    {
        var ex = new ValidationException("msg", errorCode: "ERP-LOANS-VAL-0001");
        ex.ErrorCode.Should().Be("ERP-LOANS-VAL-0001");
    }

    // ── Message is preserved ──

    [Fact]
    public void Message_is_preserved()
    {
        var ex = new NotFoundException("Resource not found");
        ex.Message.Should().Be("Resource not found");
    }

    // ── MemberData providers ──

    public static TheoryData<AppException, string> AllExceptionInstances => new()
    {
        { new ValidationException("msg"), nameof(ValidationException) },
        { new BusinessRuleException("msg"), nameof(BusinessRuleException) },
        { new NotFoundException("msg"), nameof(NotFoundException) },
        { new ConflictException("msg"), nameof(ConflictException) },
        { new UnauthorizedException("msg"), nameof(UnauthorizedException) },
        { new ForbiddenException("msg"), nameof(ForbiddenException) },
        { new IntegrationException("msg"), nameof(IntegrationException) },
        { new DependencyException("msg"), nameof(DependencyException) },
        { new DataIntegrityException("msg"), nameof(DataIntegrityException) },
        { new ConcurrencyException("msg"), nameof(ConcurrencyException) },
        { new ExternalSystemException("msg"), nameof(ExternalSystemException) },
        { new Exceptions.SystemException("msg"), nameof(Exceptions.SystemException) },
    };

    public static TheoryData<AppException, ErrorCategory, ErrorSeverity, bool> DefaultPropertyData => new()
    {
        { new ValidationException("m"),       ErrorCategory.Validation,  ErrorSeverity.Warning, false },
        { new BusinessRuleException("m"),     ErrorCategory.Business,    ErrorSeverity.Warning, false },
        { new NotFoundException("m"),         ErrorCategory.NotFound,    ErrorSeverity.Warning, false },
        { new ConflictException("m"),         ErrorCategory.Conflict,    ErrorSeverity.Warning, false },
        { new UnauthorizedException("m"),     ErrorCategory.Security,    ErrorSeverity.Warning, false },
        { new ForbiddenException("m"),        ErrorCategory.Security,    ErrorSeverity.Warning, false },
        { new IntegrationException("m"),      ErrorCategory.Integration, ErrorSeverity.Error,   false },
        { new DependencyException("m"),       ErrorCategory.Dependency,  ErrorSeverity.Error,   true  },
        { new DataIntegrityException("m"),    ErrorCategory.Data,        ErrorSeverity.Error,   false },
        { new ConcurrencyException("m"),      ErrorCategory.Concurrency, ErrorSeverity.Warning, true  },
        { new ExternalSystemException("m"),   ErrorCategory.Integration, ErrorSeverity.Error,   false },
        { new Exceptions.SystemException("m"),ErrorCategory.System,      ErrorSeverity.Error,   false },
    };
}
