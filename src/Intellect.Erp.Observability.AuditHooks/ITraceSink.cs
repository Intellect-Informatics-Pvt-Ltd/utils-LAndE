namespace Intellect.Erp.Observability.AuditHooks;

/// <summary>
/// Placeholder interface for the Traceability utility's trace sink.
/// This interface will be replaced by the actual Traceability package type when available.
/// </summary>
public interface ITraceSink
{
    /// <summary>
    /// Records an audit activity asynchronously.
    /// </summary>
    /// <param name="record">The audit activity record to persist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordAsync(AuditActivityRecord record, CancellationToken cancellationToken = default);
}
