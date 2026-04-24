using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Abstractions;

public class EnumValueTests
{
    [Fact]
    public void ErrorCategory_HasExactly10Values()
    {
        Enum.GetValues<ErrorCategory>().Should().HaveCount(10);
    }

    [Fact]
    public void ErrorCategory_ContainsExpectedValues()
    {
        var values = Enum.GetValues<ErrorCategory>();

        values.Should().Contain(ErrorCategory.Validation);
        values.Should().Contain(ErrorCategory.Business);
        values.Should().Contain(ErrorCategory.NotFound);
        values.Should().Contain(ErrorCategory.Conflict);
        values.Should().Contain(ErrorCategory.Security);
        values.Should().Contain(ErrorCategory.Integration);
        values.Should().Contain(ErrorCategory.Dependency);
        values.Should().Contain(ErrorCategory.Data);
        values.Should().Contain(ErrorCategory.Concurrency);
        values.Should().Contain(ErrorCategory.System);
    }

    [Fact]
    public void ErrorSeverity_HasExactly4Values()
    {
        Enum.GetValues<ErrorSeverity>().Should().HaveCount(4);
    }

    [Fact]
    public void ErrorSeverity_ContainsExpectedValues()
    {
        var values = Enum.GetValues<ErrorSeverity>();

        values.Should().Contain(ErrorSeverity.Info);
        values.Should().Contain(ErrorSeverity.Warning);
        values.Should().Contain(ErrorSeverity.Error);
        values.Should().Contain(ErrorSeverity.Critical);
    }

    [Fact]
    public void AuditOutcome_HasExactly3Values()
    {
        Enum.GetValues<AuditOutcome>().Should().HaveCount(3);
    }

    [Fact]
    public void AuditOutcome_ContainsExpectedValues()
    {
        var values = Enum.GetValues<AuditOutcome>();

        values.Should().Contain(AuditOutcome.Success);
        values.Should().Contain(AuditOutcome.Failure);
        values.Should().Contain(AuditOutcome.Rejected);
    }

    [Fact]
    public void SensitivityMode_HasExactly3Values()
    {
        Enum.GetValues<SensitivityMode>().Should().HaveCount(3);
    }

    [Fact]
    public void SensitivityMode_ContainsExpectedValues()
    {
        var values = Enum.GetValues<SensitivityMode>();

        values.Should().Contain(SensitivityMode.Mask);
        values.Should().Contain(SensitivityMode.Hash);
        values.Should().Contain(SensitivityMode.Redact);
    }
}
