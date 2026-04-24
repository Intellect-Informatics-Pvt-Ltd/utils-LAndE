using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.AuditHooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.AuditHooks;

/// <summary>
/// Unit tests for <see cref="AuditHookServiceCollectionExtensions.AddAuditHooks"/>
/// verifying mode selection and DI registration.
/// </summary>
public sealed class AuditHookServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAuditHooks_LogOnlyMode_RegistersLogOnlyAuditHook()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Observability:AuditHook:Mode"] = "LogOnly"
        });

        var services = new ServiceCollection();
        services.AddAuditHooks(config);

        var provider = services.BuildServiceProvider();
        var hook = provider.GetRequiredService<IAuditHook>();
        hook.Should().BeOfType<LogOnlyAuditHook>();
    }

    [Fact]
    public void AddAuditHooks_TraceabilityBridgeMode_RegistersTraceabilityBridgeAuditHook()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Observability:AuditHook:Mode"] = "TraceabilityBridge"
        });

        var services = new ServiceCollection();
        services.AddSingleton<ITraceSink>(new FakeTraceSink());
        services.AddAuditHooks(config);

        var provider = services.BuildServiceProvider();
        var hook = provider.GetRequiredService<IAuditHook>();
        hook.Should().BeOfType<TraceabilityBridgeAuditHook>();
    }

    [Fact]
    public void AddAuditHooks_KafkaMode_RegistersKafkaAuditHook()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Observability:AuditHook:Mode"] = "Kafka",
            ["Observability:AuditHook:Topic"] = "audit-events"
        });

        var services = new ServiceCollection();
        services.AddSingleton<IKafkaProducer>(new FakeKafkaProducer());
        services.AddAuditHooks(config);

        var provider = services.BuildServiceProvider();
        var hook = provider.GetRequiredService<IAuditHook>();
        hook.Should().BeOfType<KafkaAuditHook>();
    }

    [Fact]
    public void AddAuditHooks_KafkaMode_WithoutTopic_ThrowsInvalidOperationException()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Observability:AuditHook:Mode"] = "Kafka"
        });

        var services = new ServiceCollection();
        services.AddSingleton<IKafkaProducer>(new FakeKafkaProducer());

        var act = () => services.AddAuditHooks(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Topic*");
    }

    [Fact]
    public void AddAuditHooks_UnknownMode_ThrowsInvalidOperationException()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Observability:AuditHook:Mode"] = "UnknownMode"
        });

        var services = new ServiceCollection();

        var act = () => services.AddAuditHooks(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unknown audit hook mode*");
    }

    [Fact]
    public void AddAuditHooks_DefaultMode_RegistersLogOnlyAuditHook()
    {
        // When no mode is specified, defaults to LogOnly
        var config = BuildConfig(new Dictionary<string, string?>());

        var services = new ServiceCollection();
        services.AddAuditHooks(config);

        var provider = services.BuildServiceProvider();
        var hook = provider.GetRequiredService<IAuditHook>();
        hook.Should().BeOfType<LogOnlyAuditHook>();
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class FakeTraceSink : ITraceSink
    {
        public Task RecordAsync(AuditActivityRecord record, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeKafkaProducer : IKafkaProducer
    {
        public Task PublishAsync(string topic, string key, string value, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
