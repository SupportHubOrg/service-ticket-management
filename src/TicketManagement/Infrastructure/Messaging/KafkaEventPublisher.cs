using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace SupportHub.TicketManagement.Infrastructure.Messaging
{
    /// <summary>
    /// Kafka-based implementation of IEventPublisher that serializes events to JSON and publishes them to a specified Kafka topic.
    /// </summary>
    public class KafkaEventPublisher : IEventPublisher, IDisposable
    {
        private readonly IProducer<Null, string> _producer;
        private readonly ILogger<KafkaEventPublisher> _logger;
        private readonly string _topic;

        /// <summary>
        /// Initializes a new instance of a KafkaEventPublisher with the given Kafka settings and logger.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        public KafkaEventPublisher(IOptions<KafkaSettings> settings, ILogger<KafkaEventPublisher> logger)
        {
            _logger = logger;
            _topic = settings.Value.PublishTopic;

            //Configure the Kafka producer with the provided settings
            var config = new ProducerConfig
            {
                BootstrapServers = settings.Value.BootstrapServers,
                AllowAutoCreateTopics = true, //Allow automatic topic creation if the topic does not exist
                Acks = Acks.All, //Ensure all replicas acknowledge the message for durability
                EnableIdempotence = true, // Enable idempotence to prevent duplicate messages
                MessageSendMaxRetries = 5, //Limit the number of retries for failed messages
                MaxInFlight = 5 //Limit the number of in-flight messages to maintain ordering guarantees when retries occur
            };
            
            //Create the Kafka producer instance
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        /// <summary>
        /// Publishes an event to Kafka by wrapping the payload in an EventEnvelope and serializing it to JSON. Logs the result of the publish operation, including any errors that occur.
        /// </summary>
        /// <typeparam name="T">The type of the event payload.</typeparam>
        /// <param name="eventType">The type of the event, used for routing and processing on the consumer side.</param>
        /// <param name="payload">The actual event data to be published.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the publish operation.</param>
        /// <returns></returns>
        public async Task PublishAsync<T>(string eventType, T payload, CancellationToken cancellationToken = default)
        {
            //Wrap the payload in an EventEnvelope
            var envelope = new EventEnvelope<T>
            {
                Metadata = new EventMetadata { EventType = eventType },
                Payload = payload
            };

            //Configure JSON serialization options to use camelCase naming
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            };

            //Serialize the envelope to JSON
            var messageString = JsonSerializer.Serialize(envelope, jsonOptions);

            try
            {   
                //Publish the message to Kafka
                var result = await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = messageString }, cancellationToken);
                _logger.LogInformation("Published event {EventType} to partition {Partition} @ offset {Offset}", eventType, result.Partition, result.Offset);
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "Failed to publish event {EventType}", eventType);
                throw;
            }
        }

        /// <summary>
        /// Flushes any pending messages and disposes of the Kafka producer to release resources.
        /// Should be called when the publisher is no longer needed, such as during application shutdown.
        /// </summary>
        public void Dispose()
        {
            //Flush any pending messages to ensure they are sent before disposing
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();

            //Suppress finalization since the resources have already been released
            GC.SuppressFinalize(this);
        }
    }
}