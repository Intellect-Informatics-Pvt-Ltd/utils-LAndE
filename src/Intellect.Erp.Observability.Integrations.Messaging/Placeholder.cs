namespace Intellect.Erp.Observability.Integrations.Messaging;

// Placeholder interfaces for the external Intellect.Erp.Messaging.Contracts package.
// These will be replaced with actual package references once the Messaging utility is published.

/// <summary>
/// Placeholder for Intellect.Erp.Messaging.Contracts.IProducerContextEnricher.
/// </summary>
public interface IProducerContextEnricher
{
    void Enrich(EventEnvelope envelope);
}

/// <summary>
/// Placeholder for Intellect.Erp.Messaging.Contracts.EventEnvelope.
/// </summary>
public class EventEnvelope
{
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public string? TraceParent { get; set; }
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Placeholder for Intellect.Erp.Messaging.Contracts.IKafkaProducer.
/// </summary>
public interface IKafkaProducer
{
    Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken = default);
}
