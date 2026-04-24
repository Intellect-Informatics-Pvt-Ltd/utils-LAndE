namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Provides access to the centralized error catalog loaded from per-module YAML files.
/// </summary>
public interface IErrorCatalog
{
    /// <summary>
    /// Attempts to retrieve an error catalog entry by its code.
    /// </summary>
    /// <param name="code">The error code to look up (e.g., <c>ERP-CORE-SYS-0001</c>).</param>
    /// <param name="entry">When this method returns, contains the catalog entry if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the code was found in the catalog; otherwise, <c>false</c>.</returns>
    bool TryGet(string code, out ErrorCatalogEntry? entry);

    /// <summary>
    /// Retrieves an error catalog entry by its code, falling back to a default entry
    /// (<c>ERP-CORE-SYS-0001</c>) if the code is not found.
    /// </summary>
    /// <param name="code">The error code to look up.</param>
    /// <returns>The matching <see cref="ErrorCatalogEntry"/>, or the default entry if not found.</returns>
    ErrorCatalogEntry GetOrDefault(string code);

    /// <summary>
    /// Gets all loaded error catalog entries.
    /// </summary>
    IReadOnlyList<ErrorCatalogEntry> All { get; }
}
