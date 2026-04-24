using System.Text.Json;
using System.Text.Json.Serialization;
using Intellect.Erp.Observability.Abstractions;

namespace Intellect.Erp.Observability.AuditHooks;

/// <summary>
/// Audit hook that serializes <see cref="AuditEvent"/> to JSON and publishes
/// it to a configured Kafka topic via <see cref="IKafkaProducer"/>.
/// </summary>
public sealed class KafkaAuditHook : IAuditHook
{
    private readonly IKafkaProducer _producer;
    private readonly string _topic;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of <see cref="KafkaAuditHook"/>.
    /// </summary>
    /// <param name="producer">The Kafka producer to publish messages through.</param>
    /// <param name="topic">The Kafka topic to publish audit events to.</param>
    public KafkaAuditHook(IKafkaProducer producer, string topic)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _topic = !string.IsNullOrWhiteSpace(topic)
            ? topic
            : throw new ArgumentException("Topic must not be null or empty.", nameof(topic));
    }

    /// <inheritdoc />
    public Task EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var json = JsonSerializer.Serialize(auditEvent, SerializerOptions);
        return _producer.PublishAsync(_topic, auditEvent.EventId, json, cancellationToken);
    }
}
