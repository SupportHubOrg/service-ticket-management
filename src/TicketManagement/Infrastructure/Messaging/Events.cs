using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Infrastructure.Messaging;

//Outgoing events published to Kafka
public record TicketCreatedEvent(Guid TicketId, Guid OrganizationId, Guid CreatorId, string Subject, string Description);
public record TicketStatusChangedEvent(Guid TicketId, Guid OrganizationId, Status OldStatus, Status NewStatus, Guid ChangedById, Role ActorType);
public record TicketReasignedEvent(Guid TicketId, Guid OrganizationId, Guid ReassignedById, Guid? PreviousOperatorId, Guid? NewOperatorId);
public record TicketDeletedEvent(Guid TicketId, Guid OrganizationId, Guid DeletedById, Role ActorType);
public record KnowledgeArticleProposedEvent(Guid TicketId, Guid OperatorId, Guid ProposerId, string ProposedSubject, string[] Tags);
public record TicketMessageAddedEvent(Guid TicketId, Guid OrganizationId, Guid MessageId, Guid SenderId, Role SenderType, string Message);
public record TicketTagUpdatedEvent(Guid TicketId, Guid OrganizationId, Guid UpdatedById, string[] CurrentTags);
public record TicketEscalatedEvent(Guid TicketId, Guid OrganizationId, Guid EscalatedById, Status TargetLevel);
public record SlaDeadlineApprachingEvent(Guid TicketId, Guid OrganizationId, SlaType SlaType, DateTime DeadlineTime, float MinutesRemaining);
public record SlaDeadlineBreachedEvent(Guid TicketId, Guid OrganizationId, SlaType SlaType, DateTime DeadlineTime);

//Incoming events consumed from AI Triage Service
public record TagSuggestionPayload(Guid TagId, string Name);
public record AiAnalysisCompletedPayload(Guid AnalysisId, Guid TicketId, Guid OrganizationId, List<TagSuggestionPayload> TagSuggestions, string AnswerText, List<string> UsedKnowledgeArticleIds, double ConfidenceScore);
public record AiAnalysisFailedPayload(Guid AnalysisId, Guid TicketId, Guid OrganizationId, AiFailureCode FailureCode, string FailureMessage);

//Incoming events consumed from Organization Access Service
public record UserInvitationAcceptedPayload(Guid OrganizationId, Guid UserId, string UserEmail, Role Role);
public record UserRoleUpdatedPayload(Guid OrganizationId, Guid UserId, Role OldRole, Role NewRole);
public record OrganizationCreatedPayload(Guid OrganizationId, string OrganizationName, string Tier, Guid ManagerId);
public record OrganizationSettingsUpdated(Guid OrganizationId, decimal SlaMaxFirstResponseMinutes, decimal SlaMaxHumanResponseMinutes, decimal SlaMaxResolutionMinutes, decimal AiTrustThreshold);
public record OrganizationTagCreated(Guid OrganizationId, Guid TagId, string Name, string Description, bool ExcludedFromAi);

//Incoming events consumed from Knowledge Management Service
public record KnowledgeArticlePublishedPayload(Guid ArticleId, Guid DraftId, Guid SourceTicketId, Guid OrganizationId, string Title, string ArticleContent, List<string> Tags);
public record KnowledgeArticleRejectedPayload(Guid DraftId, Guid SourceTicketId, Guid OrganizationId, Guid RejectedById, string RejectionReason);

//Incoming events consumed from Notification Service
public record NotificationFailedPayload(Guid NotificationId, Guid UserId, string FailureReason);