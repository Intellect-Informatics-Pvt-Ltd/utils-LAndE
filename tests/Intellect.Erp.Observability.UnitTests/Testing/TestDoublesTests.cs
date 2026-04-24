using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Testing;
using Intellect.Erp.ErrorHandling.Exceptions;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Testing;

/// <summary>
/// Unit tests for test doubles: FakeCorrelationContextAccessor, FakeUserContextAccessor,
/// FakeTenantContextAccessor, FakeModuleContextAccessor, and FakeErrorFactory.
/// </summary>
public class TestDoublesTests
{
    [Fact]
    public void FakeCorrelationContextAccessor_AllPropertiesSettable()
    {
        var accessor = new FakeCorrelationContextAccessor
        {
            CorrelationId = "corr-123",
            CausationId = "cause-456",
            TraceParent = "00-abc-def-01"
        };

        accessor.CorrelationId.Should().Be("corr-123");
        accessor.CausationId.Should().Be("cause-456");
        accessor.TraceParent.Should().Be("00-abc-def-01");
    }

    [Fact]
    public void FakeCorrelationContextAccessor_DefaultsToNull()
    {
        var accessor = new FakeCorrelationContextAccessor();

        accessor.CorrelationId.Should().BeNull();
        accessor.CausationId.Should().BeNull();
        accessor.TraceParent.Should().BeNull();
    }

    [Fact]
    public void FakeUserContextAccessor_AllPropertiesSettable()
    {
        var accessor = new FakeUserContextAccessor
        {
            UserId = "user-1",
            UserName = "John Doe",
            Role = "Admin",
            ImpersonatingUserId = "admin-1"
        };

        accessor.UserId.Should().Be("user-1");
        accessor.UserName.Should().Be("John Doe");
        accessor.Role.Should().Be("Admin");
        accessor.ImpersonatingUserId.Should().Be("admin-1");
    }

    [Fact]
    public void FakeTenantContextAccessor_AllPropertiesSettable()
    {
        var accessor = new FakeTenantContextAccessor
        {
            TenantId = "tenant-1",
            StateCode = "MH",
            PacsId = "PACS-001",
            BranchCode = "BR-01"
        };

        accessor.TenantId.Should().Be("tenant-1");
        accessor.StateCode.Should().Be("MH");
        accessor.PacsId.Should().Be("PACS-001");
        accessor.BranchCode.Should().Be("BR-01");
    }

    [Fact]
    public void FakeModuleContextAccessor_AllPropertiesSettable()
    {
        var accessor = new FakeModuleContextAccessor
        {
            ModuleName = "Loans",
            ServiceName = "LoanService",
            Environment = "Development",
            Feature = "Disbursement",
            Operation = "Create"
        };

        accessor.ModuleName.Should().Be("Loans");
        accessor.ServiceName.Should().Be("LoanService");
        accessor.Environment.Should().Be("Development");
        accessor.Feature.Should().Be("Disbursement");
        accessor.Operation.Should().Be("Create");
    }

    [Fact]
    public void FakeErrorFactory_CreatesValidationException()
    {
        var factory = new FakeErrorFactory(correlationId: "corr-test");
        var fieldErrors = new[] { new FieldError("Name", "REQUIRED", "Name is required") };

        var ex = factory.Validation("Validation failed", fieldErrors);

        ex.Should().BeOfType<ValidationException>();
        ex.CorrelationId.Should().Be("corr-test");
        ex.Category.Should().Be(ErrorCategory.Validation);
        ((ValidationException)ex).FieldErrors.Should().HaveCount(1);
    }

    [Fact]
    public void FakeErrorFactory_CreatesAllExceptionTypes()
    {
        var factory = new FakeErrorFactory(correlationId: "corr-all");

        factory.BusinessRule("biz").Should().BeOfType<BusinessRuleException>();
        factory.NotFound("nf").Should().BeOfType<NotFoundException>();
        factory.Conflict("cf").Should().BeOfType<ConflictException>();
        factory.Unauthorized("ua").Should().BeOfType<UnauthorizedException>();
        factory.Forbidden("fb").Should().BeOfType<ForbiddenException>();
        factory.Integration("int").Should().BeOfType<IntegrationException>();
        factory.Dependency("dep").Should().BeOfType<DependencyException>();
        factory.DataIntegrity("di").Should().BeOfType<DataIntegrityException>();
        factory.Concurrency("con").Should().BeOfType<ConcurrencyException>();
        factory.ExternalSystem("ext").Should().BeOfType<ExternalSystemException>();
        factory.System("sys").Should().BeOfType<ErrorHandling.Exceptions.SystemException>();
    }

    [Fact]
    public void FakeErrorFactory_StampsCorrelationId()
    {
        var factory = new FakeErrorFactory(correlationId: "stamp-test");

        var ex = factory.NotFound("Not found");

        ex.CorrelationId.Should().Be("stamp-test");
    }

    [Fact]
    public void FakeErrorFactory_WithNullCorrelationId_StampsNull()
    {
        var factory = new FakeErrorFactory();

        var ex = factory.System("System error");

        ex.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void FakeErrorFactory_FromCatalog_UsesProvidedEntries()
    {
        var entries = new[]
        {
            new ErrorCatalogEntry(
                Code: "ERP-LOANS-BIZ-0001",
                Title: "Loan limit exceeded",
                UserMessage: "The loan amount exceeds the allowed limit.",
                SupportMessage: "Check loan limit configuration.",
                HttpStatus: 422,
                Severity: ErrorSeverity.Warning,
                Retryable: false,
                Category: ErrorCategory.Business)
        };

        var factory = new FakeErrorFactory(catalogEntries: entries);

        var ex = factory.FromCatalog("ERP-LOANS-BIZ-0001");

        ex.ErrorCode.Should().Be("ERP-LOANS-BIZ-0001");
        ex.Category.Should().Be(ErrorCategory.Business);
        ex.Message.Should().Be("The loan amount exceeds the allowed limit.");
    }

    [Fact]
    public void FakeErrorFactory_FromCatalog_UnknownCode_FallsBack()
    {
        var factory = new FakeErrorFactory();

        var ex = factory.FromCatalog("ERP-UNKNOWN-SYS-9999");

        // Falls back to ERP-CORE-SYS-0001 (the default from InMemoryErrorCatalog)
        ex.ErrorCode.Should().Be("ERP-CORE-SYS-0001");
        ex.Category.Should().Be(ErrorCategory.System);
    }
}
