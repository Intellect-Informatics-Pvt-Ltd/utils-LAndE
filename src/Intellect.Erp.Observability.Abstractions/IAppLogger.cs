namespace Intellect.Erp.Observability.Abstractions;

/// <summary>
/// Structured application logger that wraps <see cref="Microsoft.Extensions.Logging.ILogger{T}"/>
/// and provides business-context-aware logging methods including scoped operations and checkpoints.
/// </summary>
/// <typeparam name="T">The type whose name is used for the logger category.</typeparam>
public interface IAppLogger<T>
{
    /// <summary>
    /// Writes a Debug-level log entry.
    /// </summary>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Debug(string messageTemplate, params object[] args);

    /// <summary>
    /// Writes a Debug-level log entry with an associated exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Debug(Exception? exception, string messageTemplate, params object[] args);

    /// <summary>
    /// Writes an Information-level log entry.
    /// </summary>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Information(string messageTemplate, params object[] args);

    /// <summary>
    /// Writes an Information-level log entry with an associated exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Information(Exception? exception, string messageTemplate, params object[] args);

    /// <summary>
    /// Writes a Warning-level log entry.
    /// </summary>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Warning(string messageTemplate, params object[] args);

    /// <summary>
    /// Writes a Warning-level log entry with an associated exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Warning(Exception? exception, string messageTemplate, params object[] args);

    /// <summary>
    /// Writes an Error-level log entry.
    /// </summary>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Error(string messageTemplate, params object[] args);

    /// <summary>
    /// Writes an Error-level log entry with an associated exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Error(Exception? exception, string messageTemplate, params object[] args);

    /// <summary>
    /// Writes a Critical-level log entry.
    /// </summary>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Critical(string messageTemplate, params object[] args);

    /// <summary>
    /// Writes a Critical-level log entry with an associated exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="messageTemplate">A structured message template.</param>
    /// <param name="args">Arguments to fill the message template placeholders.</param>
    void Critical(Exception? exception, string messageTemplate, params object[] args);

    /// <summary>
    /// Begins a logging scope by pushing all key-value pairs from the dictionary
    /// into the Serilog <c>LogContext</c>.
    /// </summary>
    /// <param name="state">A dictionary of key-value pairs to push into the log scope.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the scope when disposed.</returns>
    IDisposable BeginScope(IReadOnlyDictionary<string, object?> state);

    /// <summary>
    /// Begins a business operation scope by pushing module, feature, operation,
    /// and optional extra context into the Serilog <c>LogContext</c>.
    /// </summary>
    /// <param name="module">The module name (e.g., "Loans", "FAS").</param>
    /// <param name="feature">The feature name (e.g., "LoanDisbursement").</param>
    /// <param name="operation">The operation name (e.g., "Create", "Approve").</param>
    /// <param name="extraContext">Optional additional key-value pairs to include in the scope.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the scope when disposed.</returns>
    IDisposable BeginOperation(
        string module,
        string feature,
        string operation,
        IReadOnlyDictionary<string, object?>? extraContext = null);

    /// <summary>
    /// Emits a structured log entry at Information level with a named checkpoint
    /// and optional data dictionary for tracking business process progress.
    /// </summary>
    /// <param name="checkpoint">The name of the checkpoint (e.g., "ValidationComplete", "PaymentInitiated").</param>
    /// <param name="data">Optional data dictionary to include in the checkpoint log entry.</param>
    void Checkpoint(string checkpoint, IReadOnlyDictionary<string, object?>? data = null);
}
