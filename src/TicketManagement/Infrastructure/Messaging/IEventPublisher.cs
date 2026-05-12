namespace SupportHub.TicketManagement.Infrastructure.Messaging
{
    public interface IEventPublisher
    {
        /// <summary>
        /// Wraps the payload in an EventEnvelope and publishes it to Kafka.
        /// </summary>
        Task PublishAsync<T>(string eventType, T payload, CancellationToken cancellationToken = default);
    }
}