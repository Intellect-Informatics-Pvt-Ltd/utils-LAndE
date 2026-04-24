using FluentAssertions;
using Intellect.Erp.Observability.Core;
using Microsoft.Extensions.Options;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Core;

/// <summary>
/// Unit tests for <see cref="ObservabilityOptionsValidator"/> covering valid config,
/// missing ApplicationName, missing ModuleName, invalid ES URL, and masking disabled in Production.
/// </summary>
public class ObservabilityOptionsValidationTests
{
    private readonly ObservabilityOptionsValidator _validator = new();

    [Fact]
    public void ValidConfig_PassesValidation()
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = "TestModule",
            Environment = "Development",
            Masking = new MaskingOptions { Enabled = true }
        };

        var result = _validator.Validate(null, options);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingApplicationName_FailsValidation(string? appName)
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = appName!,
            ModuleName = "TestModule",
            Environment = "Development"
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ApplicationName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingModuleName_FailsValidation(string? moduleName)
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = moduleName!,
            Environment = "Development"
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ModuleName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("   ")]
    public void InvalidElasticsearchUrl_WhenEsSinkEnabled_FailsValidation(string url)
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = "TestModule",
            Environment = "Development",
            Sinks = new SinkOptions
            {
                Elasticsearch = new ElasticsearchSinkOptions
                {
                    Enabled = true,
                    Url = url
                }
            }
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Elasticsearch");
    }

    [Fact]
    public void ValidElasticsearchUrl_WhenEsSinkEnabled_PassesValidation()
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = "TestModule",
            Environment = "Development",
            Sinks = new SinkOptions
            {
                Elasticsearch = new ElasticsearchSinkOptions
                {
                    Enabled = true,
                    Url = "https://elasticsearch.example.com:9200"
                }
            }
        };

        var result = _validator.Validate(null, options);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void ElasticsearchUrl_NotValidated_WhenSinkDisabled()
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = "TestModule",
            Environment = "Development",
            Sinks = new SinkOptions
            {
                Elasticsearch = new ElasticsearchSinkOptions
                {
                    Enabled = false,
                    Url = "not-a-url"
                }
            }
        };

        var result = _validator.Validate(null, options);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void MaskingDisabledInProduction_FailsValidation()
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = "TestModule",
            Environment = "Production",
            Masking = new MaskingOptions { Enabled = false }
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Masking");
    }

    [Fact]
    public void MaskingEnabledInProduction_PassesValidation()
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = "TestModule",
            Environment = "Production",
            Masking = new MaskingOptions { Enabled = true }
        };

        var result = _validator.Validate(null, options);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void MaskingDisabledInDevelopment_PassesValidation()
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = "TestApp",
            ModuleName = "TestModule",
            Environment = "Development",
            Masking = new MaskingOptions { Enabled = false }
        };

        var result = _validator.Validate(null, options);

        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void MultipleFailures_ReportsAll()
    {
        var options = new ObservabilityOptions
        {
            ApplicationName = "",
            ModuleName = "",
            Environment = "Production",
            Masking = new MaskingOptions { Enabled = false },
            Sinks = new SinkOptions
            {
                Elasticsearch = new ElasticsearchSinkOptions
                {
                    Enabled = true,
                    Url = ""
                }
            }
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ApplicationName");
        result.FailureMessage.Should().Contain("ModuleName");
        result.FailureMessage.Should().Contain("Masking");
        result.FailureMessage.Should().Contain("Elasticsearch");
    }
}
