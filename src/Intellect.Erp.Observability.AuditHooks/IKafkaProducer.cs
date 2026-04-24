namespace Intellect.Erp.Observability.AuditHooks;

/// <summary>
/// Placeholder interface for the Messaging utility's Kafka producer.
/// This interface will be replaced by the actual Messaging package type when available.
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Publishes a message to the specified Kafka topic.
    /// </summary>
    /// <param name="topic">The Kafka topic to publish to.</param>
    /// <param name="key">The message key.</param>
    /// <param name="value">The serialized message value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync(string topic, string key, string value, CancellationToken cancellationToken = default);
}
