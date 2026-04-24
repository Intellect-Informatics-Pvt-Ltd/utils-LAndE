using Microsoft.Extensions.Logging;
using Serilog.Context;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.Core;

/// <summary>
/// Structured application logger that wraps <see cref="ILogger{T}"/> and provides
/// business-context-aware logging with scoped operations and checkpoints.
/// </summary>
/// <typeparam name="T">The type whose name is used for the logger category.</typeparam>
public sealed class AppLogger<T> : IAppLogger<T>
{
    private readonly ILogger<T> _logger;

    public AppLogger(ILogger<T> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Debug(string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Debug, messageTemplate, args);

    public void Debug(Exception? exception, string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Debug, exception, messageTemplate, args);

    public void Information(string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Information, messageTemplate, args);

    public void Information(Exception? exception, string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Information, exception, messageTemplate, args);

    public void Warning(string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Warning, messageTemplate, args);

    public void Warning(Exception? exception, string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Warning, exception, messageTemplate, args);

    public void Error(string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Error, messageTemplate, args);

    public void Error(Exception? exception, string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Error, exception, messageTemplate, args);

    public void Critical(string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Critical, messageTemplate, args);

    public void Critical(Exception? exception, string messageTemplate, params object[] args)
        => _logger.Log(LogLevel.Critical, exception, messageTemplate, args);

    /// <inheritdoc />
    public IDisposable BeginScope(IReadOnlyDictionary<string, object?> state)
    {
        var disposables = new List<IDisposable>(state.Count);
        foreach (var kvp in state)
        {
            disposables.Add(LogContext.PushProperty(kvp.Key, kvp.Value));
        }
        return new CompositeDisposable(disposables);
    }

    /// <inheritdoc />
    public IDisposable BeginOperation(
        string module,
        string feature,
        string operation,
        IReadOnlyDictionary<string, object?>? extraContext = null)
    {
        var dict = new Dictionary<string, object?>
        {
            ["module"] = module,
            ["feature"] = feature,
            ["operation"] = operation
        };

        if (extraContext is not null)
        {
            foreach (var kvp in extraContext)
            {
                dict[kvp.Key] = kvp.Value;
            }
        }

        return BeginScope(dict);
    }

    /// <inheritdoc />
    public void Checkpoint(string checkpoint, IReadOnlyDictionary<string, object?>? data = null)
    {
        var disposables = new List<IDisposable> { LogContext.PushProperty("checkpoint", checkpoint) };
        try
        {
            if (data is not null)
            {
                foreach (var kvp in data)
                {
                    disposables.Add(LogContext.PushProperty(kvp.Key, kvp.Value));
                }
            }

            _logger.Log(LogLevel.Information, "Checkpoint {Checkpoint} reached", checkpoint);
        }
        finally
        {
            for (var i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
        }
    }
}
