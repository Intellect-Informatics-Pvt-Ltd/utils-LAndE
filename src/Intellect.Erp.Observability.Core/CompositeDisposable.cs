namespace Intellect.Erp.Observability.Core;

/// <summary>
/// Aggregates multiple <see cref="IDisposable"/> instances and disposes them all
/// when this instance is disposed. Disposal occurs in reverse order.
/// </summary>
internal sealed class CompositeDisposable : IDisposable
{
    private readonly IReadOnlyList<IDisposable> _disposables;
    private bool _disposed;

    public CompositeDisposable(IReadOnlyList<IDisposable> disposables)
    {
        _disposables = disposables;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose in reverse order so the last-pushed scope is removed first.
        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            _disposables[i].Dispose();
        }
    }
}
