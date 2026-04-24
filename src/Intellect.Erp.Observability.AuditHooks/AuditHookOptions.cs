using System.ComponentModel.DataAnnotations;

namespace Intellect.Erp.Observability.AuditHooks;

/// <summary>
/// Configuration options for audit hook mode selection.
/// Binds to the <c>Observability:AuditHook</c> configuration section.
/// </summary>
public sealed class AuditHookOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Observability:AuditHook";

    /// <summary>
    /// The audit hook mode. Valid values: <c>LogOnly</c>, <c>TraceabilityBridge</c>, <c>Kafka</c>.
    /// Defaults to <c>LogOnly</c>.
    /// </summary>
    [Required]
    public string Mode { get; set; } = "LogOnly";

    /// <summary>
    /// The Kafka topic to publish audit events to. Required when <see cref="Mode"/> is <c>Kafka</c>.
    /// </summary>
    public string? Topic { get; set; }
}
