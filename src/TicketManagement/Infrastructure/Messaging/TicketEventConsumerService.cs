using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Options;

using SupportHub.TicketManagement.Features.Events;

namespace SupportHub.TicketManagement.Infrastructure.Messaging
{
    /// <summary>
    /// Background service that consumes events from Kafka topics, deserializes them, and routes them to the appropriate MediatR commands for processing.
    /// Designed to run continuously as part of the application's hosted services.
    /// </summary>
    public class TicketEventConsumerService : BackgroundService
    {
        private readonly KafkaSettings _settings;
        private readonly ILogger<TicketEventConsumerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IProducer<Null, string> _dlqProducer;

        /// <summary>
        /// Initializes a new instance of the TicketEventConsumerService with the given Kafka settings, logger, and service scope factory.
        /// Also initializes a dedicated Kafka producer for sending messages to the Dead Letter Queue in case of processing failures.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        /// <param name="scopeFactory"></param>
        public TicketEventConsumerService(
            IOptions<KafkaSettings> settings,
            ILogger<TicketEventConsumerService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _settings = settings.Value;
            _logger = logger;
            _scopeFactory = scopeFactory;

            _dlqProducer = new ProducerBuilder<Null, string>(new ProducerConfig 
            { 
                BootstrapServers = _settings.BootstrapServers,
                Acks = Acks.All
            }).Build();
        }

        /// <summary>
        /// Continuously listens to the configured Kafka topics for new events.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token that signals when the service should stop.</param>
        /// <returns>/returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //Configure the Kafka consumer with the provided settings
            var config = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false, //Manual commit after processing ensures no dropped messages
                AllowAutoCreateTopics = true //Allow the consumer to create topics if they don't exist
            };

            //Create the Kafka consumer instance and subscribe to the configured topics
            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_settings.SubscribeTopics);

            _logger.LogInformation("Started listening to Kafka topics: {Topics}", string.Join(", ", _settings.SubscribeTopics));

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        //Consume a message from Kafka -> this call will block until a message is available or the stopping token is triggered
                        var consumeResult = consumer.Consume(stoppingToken);
                        var messageJson = consumeResult.Message.Value;
                        string eventType = "Unknown";

                        try
                        {
                            //Parse the message JSON to extract the event type from the metadata for routing purposes
                            using var document = JsonDocument.Parse(messageJson);
                            eventType = document.RootElement.GetProperty("metadata").GetProperty("eventType").GetString() ?? "Unknown";

                            _logger.LogInformation("Received event: {EventType}", eventType);

                            //Process the event
                            await ProcessEventAsync(eventType, messageJson, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing event of type {EventType}. Routing to DLQ.", eventType);

                            //If any exception occurs during processing, send the original message to the Dead Letter Queue for later analysis
                            await RouteToDeadLetterQueueAsync(messageJson, ex, stoppingToken);
                        }

                        //Commit the offset after successful processing to avoid re-processing on restart
                        consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka Consume Error");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka Consumer stopping due to application shutdown.");
            }
            finally
            {
                //Once the stopping token is triggered, close the consumer to release resources and leave the consumer group gracefully
                consumer.Close();

                //Flush and dispose the DLQ producer to ensure all messages are sent before shutdown
                _dlqProducer.Flush(TimeSpan.FromSeconds(5));
                _dlqProducer.Dispose();
            }
        }

        /// <summary>
        /// Processes a consumed Kafka event by routing it to the appropriate MediatR command based on the event type.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="rawJson"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private async Task ProcessEventAsync(string? eventType, string rawJson, CancellationToken stoppingToken)
        {
            //Create a new scope for MediatR and the DbContext
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            //Route the event to the appropriate command handler based on the event type
            switch (eventType)
            {
                case "AiAnalysisFailed":
                    var aiFailEvent = JsonSerializer.Deserialize<EventEnvelope<AiAnalysisFailedPayload>>(rawJson);
                    if (aiFailEvent?.Payload != null)
                        await mediator.Send(new ProcessAiFailureCommand(aiFailEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received AiAnalysisFailed event with null payload. Raw message: {RawJson}", rawJson);
                    break;

                case "KnowledgeArticleApproved":
                    var kbApprovedEvent = JsonSerializer.Deserialize<EventEnvelope<KnowledgeArticlePublishedPayload>>(rawJson);
                    if (kbApprovedEvent?.Payload != null)
                        await mediator.Send(new ProcessKnowledgeArticlePublishedCommand(kbApprovedEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received KnowledgeArticleApproved event with null payload. Raw message: {RawJson}", rawJson);
                    break;

                case "KnowledgeArticleRejected":
                    var kbRejectedEvent = JsonSerializer.Deserialize<EventEnvelope<KnowledgeArticleRejectedPayload>>(rawJson);
                    if (kbRejectedEvent?.Payload != null)
                        await mediator.Send(new ProcessKnowledgeArticleRejectedCommand(kbRejectedEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received KnowledgeArticleRejected event with null payload. Raw message: {RawJson}", rawJson);
                    break;
                
                case "AiAnalysisCompleted":
                    var aiCompletedEvent = JsonSerializer.Deserialize<EventEnvelope<AiAnalysisCompletedPayload>>(rawJson);
                    if (aiCompletedEvent?.Payload != null)
                        await mediator.Send(new ProcessAiAnalysisCompletedCommand(aiCompletedEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received AiAnalysisCompleted event with null payload. Raw message: {RawJson}", rawJson);
                    break;
                
                case "UserInvitationAccepted":
                    var invitationAcceptedEvent = JsonSerializer.Deserialize<EventEnvelope<UserInvitationAcceptedPayload>>(rawJson);
                    if (invitationAcceptedEvent?.Payload != null)
                        await mediator.Send(new ProcessUserInvitationAcceptedCommand(invitationAcceptedEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received UserInvitationAccepted event with null payload. Raw message: {RawJson}", rawJson);
                    break;

                case "UserRoleUpdated":
                    var userRoleUpdatedEvent = JsonSerializer.Deserialize<EventEnvelope<UserRoleUpdatedPayload>>(rawJson);
                    if (userRoleUpdatedEvent?.Payload != null)
                        await mediator.Send(new ProcessUserRoleUpdatedCommand(userRoleUpdatedEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received UserRoleUpdated event with null payload. Raw message: {RawJson}", rawJson);
                    break;

                case "UserRemovedFromOrganization":
                    var userRemovedEvent = JsonSerializer.Deserialize<EventEnvelope<UserRemovedFromOrganizationPayload>>(rawJson);
                    if (userRemovedEvent?.Payload != null)
                        await mediator.Send(new ProcessUserRemovedFromOrganizationCommand(userRemovedEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received UserRemovedFromOrganization event with null payload. Raw message: {RawJson}", rawJson);
                    break;
                
                case "OrganizationCreated":
                    var orgCreatedEvent = JsonSerializer.Deserialize<EventEnvelope<OrganizationCreatedPayload>>(rawJson);
                    if (orgCreatedEvent?.Payload != null)
                        await mediator.Send(new ProcessOrganizationCreatedCommand(orgCreatedEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received OrganizationCreated event with null payload. Raw message: {RawJson}", rawJson);
                    break;

                case "OrganizationSettingsUpdated":
                    var orgSettingsUpdatedEvent = JsonSerializer.Deserialize<EventEnvelope<OrganizationSettingsUpdatedPayload>>(rawJson);
                    if (orgSettingsUpdatedEvent?.Payload != null)
                        await mediator.Send(new ProcessOrganizationSettingsUpdatedCommand(orgSettingsUpdatedEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received OrganizationSettingsUpdated event with null payload. Raw message: {RawJson}", rawJson);
                    break;
                
                case "OrganizationDeleted":
                    var orgDeletedEvent = JsonSerializer.Deserialize<EventEnvelope<OrganizationDeletedPayload>>(rawJson);
                    if (orgDeletedEvent?.Payload != null)
                        await mediator.Send(new ProcessOrganizationDeletedCommand(orgDeletedEvent.Payload), stoppingToken);
                    else
                        _logger.LogWarning("Received OrganizationDeleted event with null payload. Raw message: {RawJson}", rawJson);
                    break;
                
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Routes a failed message to the Dead Letter Queue by publishing it a specified DLQ Kafka topic.
        /// </summary>
        /// <param name="messageJson">The original message JSON that failed to process, which will be sent to the DLQ for later analysis.</param>
        /// <param name="ex">The exception that was thrown during processing.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the publish operation if needed.</param>
        /// <returns></returns>
        private async Task RouteToDeadLetterQueueAsync(string messageJson, Exception ex, CancellationToken cancellationToken)
        {
            var deadLetterQueueMessage = new Message<Null, string>
            {
                Value = messageJson,
                Headers = new Headers
                {
                    { "ExceptionMessage", Encoding.UTF8.GetBytes(ex.Message) },
                    { "StackTrace", Encoding.UTF8.GetBytes(ex.StackTrace ?? string.Empty) },
                    { "FailedAt", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                }
            };

            await _dlqProducer.ProduceAsync(_settings.DeadLetterQueueTopic, deadLetterQueueMessage, cancellationToken);
        }
    }
}