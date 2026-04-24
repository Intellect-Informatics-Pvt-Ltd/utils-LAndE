namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// A single entry in the centralized error catalog, loaded from per-module YAML files.
/// </summary>
/// <param name="Code">Stable error code in the format <c>ERP-MODULE-CATEGORY-SEQ4</c>.</param>
/// <param name="Title">Short, human-readable title for the error.</param>
/// <param name="UserMessage">Consumer-safe message suitable for display in a UI.</param>
/// <param name="SupportMessage">Internal message with resolution guidance for support engineers.</param>
/// <param name="HttpStatus">The HTTP status code to return when this error is raised.</param>
/// <param name="Severity">The severity level of this error.</param>
/// <param name="Retryable">Whether the operation that caused this error can be retried.</param>
/// <param name="Category">The error category for classification and HTTP mapping.</param>
public sealed record ErrorCatalogEntry(
    string Code,
    string Title,
    string UserMessage,
    string SupportMessage,
    int HttpStatus,
    ErrorSeverity Severity,
    bool Retryable,
    ErrorCategory Category);
