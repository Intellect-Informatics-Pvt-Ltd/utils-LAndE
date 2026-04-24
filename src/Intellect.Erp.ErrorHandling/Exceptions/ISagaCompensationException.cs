namespace Intellect.Erp.ErrorHandling.Exceptions;

/// <summary>
/// Marker interface for exceptions that occur within a saga-scoped operation
/// and may require compensation logic.
/// </summary>
/// <remarks>
/// This is a placeholder interface defined in the ErrorHandling package.
/// When the Traceability utility is available, it will provide its own definition
/// and the integration shim will bridge the two.
/// </remarks>
public interface ISagaCompensationException;
