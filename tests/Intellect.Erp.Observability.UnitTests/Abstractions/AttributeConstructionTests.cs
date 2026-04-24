using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Abstractions;

public class AttributeConstructionTests
{
    [Fact]
    public void BusinessOperationAttribute_SetsModuleFeatureOperation()
    {
        var attr = new BusinessOperationAttribute("Loans", "LoanDisbursement", "Create");

        attr.Module.Should().Be("Loans");
        attr.Feature.Should().Be("LoanDisbursement");
        attr.Operation.Should().Be("Create");
    }

    [Fact]
    public void BusinessOperationAttribute_TargetsMethodAndClass()
    {
        var usage = typeof(BusinessOperationAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.Should().HaveFlag(AttributeTargets.Method);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Class);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeTrue();
    }

    [Fact]
    public void ErrorCodeAttribute_SetsCode()
    {
        var attr = new ErrorCodeAttribute("ERP-LOANS-VAL-0001");

        attr.Code.Should().Be("ERP-LOANS-VAL-0001");
    }

    [Fact]
    public void ErrorCodeAttribute_TargetsMethod()
    {
        var usage = typeof(ErrorCodeAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.Should().Be(AttributeTargets.Method);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeTrue();
    }

    [Fact]
    public void SensitiveAttribute_DefaultValues()
    {
        var attr = new SensitiveAttribute();

        attr.Mode.Should().Be(SensitivityMode.Mask);
        attr.KeepLast.Should().Be(4);
    }

    [Fact]
    public void SensitiveAttribute_CustomValues()
    {
        var attr = new SensitiveAttribute(SensitivityMode.Hash, keepLast: 6);

        attr.Mode.Should().Be(SensitivityMode.Hash);
        attr.KeepLast.Should().Be(6);
    }

    [Fact]
    public void SensitiveAttribute_TargetsPropertyFieldParameter()
    {
        var usage = typeof(SensitiveAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.Should().HaveFlag(AttributeTargets.Property);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Field);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Parameter);
        usage.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void DoNotLogAttribute_CanBeConstructed()
    {
        var attr = new DoNotLogAttribute();

        attr.Should().NotBeNull();
    }

    [Fact]
    public void DoNotLogAttribute_TargetsPropertyFieldParameter()
    {
        var usage = typeof(DoNotLogAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.Should().HaveFlag(AttributeTargets.Property);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Field);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Parameter);
        usage.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void MaskAttribute_SetsRegexAndReplacement()
    {
        var attr = new MaskAttribute(@"\d{4}", "XXXX");

        attr.Regex.Should().Be(@"\d{4}");
        attr.Replacement.Should().Be("XXXX");
    }

    [Fact]
    public void MaskAttribute_DefaultReplacementIsThreeStars()
    {
        var attr = new MaskAttribute(@"\d+");

        attr.Replacement.Should().Be("***");
    }

    [Fact]
    public void MaskAttribute_TargetsPropertyAndField()
    {
        var usage = typeof(MaskAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.Should().HaveFlag(AttributeTargets.Property);
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Field);
        usage.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void PublicAPIAttribute_CanBeConstructed()
    {
        var attr = new PublicAPIAttribute();

        attr.Should().NotBeNull();
    }

    [Fact]
    public void PublicAPIAttribute_TargetsAll()
    {
        var usage = typeof(PublicAPIAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        usage.ValidOn.Should().Be(AttributeTargets.All);
        usage.AllowMultiple.Should().BeFalse();
    }
}
