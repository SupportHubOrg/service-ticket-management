using System.Text.Json.Serialization;

namespace SupportHub.TicketManagement.Infrastructure.Messaging
{
    /// <summary>
    /// Generic wrapper for events published to Kafka, containing both the event payload and metadata such as event type and timestamp.
    /// </summary>
    /// <typeparam name="T">The type of the event payload.</typeparam>
    public class EventEnvelope<T>
    {
        [JsonPropertyName("metadata")]
        public EventMetadata Metadata { get; set; } = new();

        [JsonPropertyName("payload")]
        public T Payload { get; set; } = default!;
    }

    /// <summary>
    /// Metadata for events, including the event type and the timestamp when the event was created.
    /// </summary>
    public class EventMetadata
    {
        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = string.Empty;
        
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}