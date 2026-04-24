namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Represents a single field-level validation error within an <see cref="ErrorResponse"/>.
/// </summary>
/// <param name="Field">The name of the field that failed validation.</param>
/// <param name="Code">A stable error code identifying the validation rule.</param>
/// <param name="Message">A human-readable description of the validation failure.</param>
public sealed record FieldError(string Field, string Code, string Message);
