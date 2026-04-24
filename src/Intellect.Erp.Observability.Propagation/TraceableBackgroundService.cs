using Intellect.Erp.Observability.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Intellect.Erp.Observability.Propagation;

/// <summary>
/// Abstract base class extending <see cref="BackgroundService"/> that establishes
/// a scoped correlation ID, opens an enrichment scope with module and operation context,
/// and provides resilient error handling with continuation.
/// </summary>
/// <remarks>
/// Subclasses override <see cref="ExecuteTracedAsync"/> instead of <c>ExecuteAsync</c>.
/// </remarks>
public abstract class TraceableBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TraceableBackgroundService"/>.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating DI scopes.</param>
    /// <param name="logger">Logger for error reporting.</param>
    protected TraceableBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the module name for log enrichment. Override to provide a custom value.
    /// Defaults to the concrete type name.
    /// </summary>
    protected virtual string ModuleName => GetType().Name;

    /// <summary>
    /// Gets the operation name for log enrichment. Override to provide a custom value.
    /// Defaults to "BackgroundExecution".
    /// </summary>
    protected virtual string OperationName => "BackgroundExecution";

    /// <inheritdoc />
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            // Generate or inherit correlation ID
            var correlationId = Guid.NewGuid().ToString("N");

            try
            {
                // Resolve IAppLogger from the scope to get enrichment
                var appLogger = scope.ServiceProvider.GetService<IAppLogger<TraceableBackgroundService>>();

                var scopeData = new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["module"] = ModuleName,
                    ["operation"] = OperationName
                };

                IDisposable? enrichmentScope = null;
                try
                {
                    enrichmentScope = appLogger?.BeginScope(scopeData);
                    await ExecuteTracedAsync(stoppingToken);
                    // If the subclass completes without requesting continuation, exit
                    return;
                }
                finally
                {
                    enrichmentScope?.Dispose();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — exit the loop
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "TraceableBackgroundService {ServiceName} failed with CorrelationId {CorrelationId}. Continuing operation.",
                    GetType().Name,
                    correlationId);

                // Continue operation — loop back and retry
            }
        }
    }

    /// <summary>
    /// Override this method to implement the background service logic.
    /// Called within a DI scope with correlation ID and enrichment context established.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token signaling shutdown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task ExecuteTracedAsync(CancellationToken stoppingToken);
}
