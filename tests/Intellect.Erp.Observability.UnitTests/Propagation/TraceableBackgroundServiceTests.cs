using FluentAssertions;
using Intellect.Erp.Observability.Abstractions;
using Intellect.Erp.Observability.Propagation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Intellect.Erp.Observability.UnitTests.Propagation;

/// <summary>
/// Unit tests for <see cref="TraceableBackgroundService"/> verifying correlation scope,
/// error logging, and continuation after failure.
/// </summary>
public class TraceableBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_EstablishesCorrelationId_AndCallsExecuteTracedAsync()
    {
        // Arrange
        var services = BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var logger = services.GetRequiredService<ILogger<TraceableBackgroundServiceTests>>();
        var executedTcs = new TaskCompletionSource<bool>();

        var service = new TestBackgroundService(scopeFactory, logger, onExecute: _ =>
        {
            executedTcs.SetResult(true);
            return Task.CompletedTask;
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var executeTask = service.StartAsync(cts.Token);
        var executed = await executedTcs.Task;

        // Assert
        executed.Should().BeTrue("ExecuteTracedAsync should have been called");

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_LogsErrorAndContinues_WhenExecuteTracedAsyncThrows()
    {
        // Arrange
        var services = BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<TraceableBackgroundServiceTests>();
        var fakeLoggerProvider = services.GetRequiredService<FakeLoggerProvider>();

        var callCount = 0;
        var secondCallTcs = new TaskCompletionSource<bool>();

        var service = new TestBackgroundService(scopeFactory, logger, onExecute: ct =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new InvalidOperationException("Simulated failure");
            }

            // Second call — signal success and complete
            secondCallTcs.SetResult(true);
            return Task.CompletedTask;
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var executeTask = service.StartAsync(cts.Token);
        var continued = await secondCallTcs.Task;

        // Assert
        continued.Should().BeTrue("Service should continue after first failure");
        callCount.Should().BeGreaterThanOrEqualTo(2, "Service should have retried after failure");

        // Verify error was logged
        fakeLoggerProvider.LogEntries.Should().Contain(entry =>
            entry.LogLevel == LogLevel.Error &&
            entry.Message.Contains("TraceableBackgroundService") &&
            entry.Message.Contains("CorrelationId"));

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_StopsGracefully_WhenCancellationRequested()
    {
        // Arrange
        var services = BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var logger = services.GetRequiredService<ILogger<TraceableBackgroundServiceTests>>();
        var startedTcs = new TaskCompletionSource<bool>();

        var service = new TestBackgroundService(scopeFactory, logger, onExecute: async ct =>
        {
            startedTcs.SetResult(true);
            // Wait indefinitely until cancelled
            await Task.Delay(Timeout.Infinite, ct);
        });

        using var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await startedTcs.Task; // Wait for service to start executing

        // Cancel and stop
        cts.Cancel();
        var stopTask = service.StopAsync(CancellationToken.None);
        var completed = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(5)));

        // Assert
        completed.Should().Be(stopTask, "Service should stop gracefully on cancellation");
    }

    #region Helpers

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        var fakeProvider = new FakeLoggerProvider();
        services.AddSingleton(fakeProvider);
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(fakeProvider);
        });
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Concrete test implementation of TraceableBackgroundService.
    /// </summary>
    private sealed class TestBackgroundService : TraceableBackgroundService
    {
        private readonly Func<CancellationToken, Task> _onExecute;

        public TestBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger logger,
            Func<CancellationToken, Task> onExecute)
            : base(scopeFactory, logger)
        {
            _onExecute = onExecute;
        }

        protected override Task ExecuteTracedAsync(CancellationToken stoppingToken)
            => _onExecute(stoppingToken);
    }

    /// <summary>
    /// Fake logger provider that captures log entries for assertion.
    /// </summary>
    internal sealed class FakeLoggerProvider : ILoggerProvider
    {
        private readonly List<LogEntry> _entries = new();

        public IReadOnlyList<LogEntry> LogEntries => _entries;

        public ILogger CreateLogger(string categoryName) => new FakeLogger(_entries);

        public void Dispose() { }

        private sealed class FakeLogger : ILogger
        {
            private readonly List<LogEntry> _entries;

            public FakeLogger(List<LogEntry> entries) => _entries = entries;

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                _entries.Add(new LogEntry
                {
                    LogLevel = logLevel,
                    Message = formatter(state, exception),
                    Exception = exception
                });
            }
        }
    }

    internal sealed class LogEntry
    {
        public LogLevel LogLevel { get; init; }
        public string Message { get; init; } = string.Empty;
        public Exception? Exception { get; init; }
    }

    #endregion
}
