namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Categorizes errors by their domain origin, used for HTTP status mapping
/// and error catalog classification.
/// </summary>
public enum ErrorCategory
{
    /// <summary>Input validation failure (HTTP 400).</summary>
    Validation,

    /// <summary>Business rule violation (HTTP 422).</summary>
    Business,

    /// <summary>Requested resource not found (HTTP 404).</summary>
    NotFound,

    /// <summary>State conflict with current resource version (HTTP 409).</summary>
    Conflict,

    /// <summary>Authentication or authorization failure (HTTP 401/403).</summary>
    Security,

    /// <summary>External integration or upstream service failure (HTTP 502).</summary>
    Integration,

    /// <summary>Required dependency unavailable (HTTP 503).</summary>
    Dependency,

    /// <summary>Data integrity or persistence failure (HTTP 500).</summary>
    Data,

    /// <summary>Optimistic concurrency conflict (HTTP 409).</summary>
    Concurrency,

    /// <summary>Unclassified system-level failure (HTTP 500).</summary>
    System
}
