using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Infrastructure.Messaging;

//Outgoing events published to Kafka
public record TicketCreatedEvent(Guid TicketId, Guid OrganizationId, Guid CreatorId, string Subject, string Description);
public record TicketStatusChangedEvent(Guid TicketId, Guid OrganizationId, Status OldStatus, Status NewStatus, Guid ChangedById, Role ActorType);
public record TicketReasignedEvent(Guid TicketId, Guid OrganizationId, Guid ReassignedById, Guid? PreviousOperatorId, Guid? NewOperatorId);
public record TicketDeletedEvent(Guid TicketId, Guid OrganizationId, Guid DeletedById, Role ActorType);
public record KnowledgeArticleProposedEvent(Guid TicketId, Guid OperatorId, Guid ProposerId, string ProposedSubject, Guid[] Tags);
public record TicketMessageAddedEvent(Guid TicketId, Guid OrganizationId, Guid MessageId, Guid SenderId, Role SenderType, string Message);
public record TicketTagUpdatedEvent(Guid TicketId, Guid OrganizationId, Guid UpdatedById, Guid[] CurrentTags);
public record TicketEscalatedEvent(Guid TicketId, Guid OrganizationId, Guid EscalatedById, Status TargetLevel);
public record SlaDeadlineApprachingEvent(Guid TicketId, Guid OrganizationId, SlaType SlaType, DateTime DeadlineTime, float MinutesRemaining);
public record SlaDeadlineBreachedEvent(Guid TicketId, Guid OrganizationId, SlaType SlaType, DateTime DeadlineTime);
public record OperatorTagAddedEvent(Guid OperatorId, Guid TagId, Guid AddedById, string TagName);
public record OperatorTagRemovedEvent(Guid OperatorId, Guid TagId, Guid RemovedById, string TagName);
public record TagCreatedEvent(Guid TagId, Guid OrganizationId, string TagName);
public record TagDeletedEvent(Guid TagId, Guid OrganizationId, Guid DeletedById);
public record TagAISettingUpdatedEvent(Guid TagId, Guid OrganizationId, Guid UpdatedById, bool ExludeFromAi);

//Incoming events consumed from AI Triage Service
public record AiAnalysisCompletedPayload(Guid AnalysisId, Guid TicketId, Guid OrganizationId, Guid[] TagSuggestions, string AnswerText, List<string> UsedKnowledgeArticleIds, double ConfidenceScore);
public record AiAnalysisFailedPayload(Guid AnalysisId, Guid TicketId, Guid OrganizationId, AiFailureCode FailureCode, string FailureMessage);

//Incoming events consumed from Organization Access Service
public record UserInvitationAcceptedPayload(Guid OrganizationId, Guid UserId, string UserEmail, Role Role);
public record UserRoleUpdatedPayload(Guid OrganizationId, Guid UserId, Role OldRole, Role NewRole);
public record UserRemovedFromOrganizationPayload(Guid OrganizationId, Guid UserId, string UserEmail);
public record OrganizationCreatedPayload(Guid OrganizationId, string OrganizationName, string Tier, Guid ManagerId);
public record OrganizationSettingsUpdatedPayload(Guid OrganizationId, decimal SlaFirstResponseMinutes, decimal SlaHumanResponseMinutes, decimal SlaResolutionMinutes, decimal AiTrustThreshold);
public record OrganizationDeletedPayload(Guid OrganizationId, string OrganizationName);

//Incoming events consumed from Knowledge Management Service
public record KnowledgeArticlePublishedPayload(Guid ArticleId, Guid DraftId, Guid SourceTicketId, Guid OrganizationId, Guid PublishedById, string Title, string ArticleContent, List<string> Tags);
public record KnowledgeArticleRejectedPayload(Guid DraftId, Guid SourceTicketId, Guid OrganizationId, Guid RejectedById, string RejectionReason);

//Incoming events consumed from Notification Service
public record NotificationFailedPayload(Guid NotificationId, Guid UserId, string FailureReason);

//A common tag payload
public record TagPayload(Guid TagId, string Name);