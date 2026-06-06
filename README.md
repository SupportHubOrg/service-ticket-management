# Ticket Management Service

## General information
The Ticket Management service owns the lifecycle of support tickets, including creation, assignment, tagging, messaging, auditing, and SLA tracking. It exposes a REST API and integrates with other services through Kafka events.

Core characteristics:
- ASP.NET Core minimal API with MediatR for commands/queries.
- SQL Server persistence with EF Core and automatic migrations on startup.
- JWT Bearer authentication; all endpoints require authorization.
- Kafka for incoming and outgoing domain events.
- Structured logging with Serilog.
- Health checks and Prometheus metrics.
- CORS configured via `Cors:AllowedOrigins`.

Configuration highlights:
- `Authentication:Authority` and `Authentication:Audience` are required.
- `Kafka` settings include `BootstrapServers`, `PublishTopic`, `SubscribeTopics`, `GroupId`, and `DeadLetterQueueTopic`.

## Entities

### `Ticket`
*Represents a support ticket created by a user. Contains information about the issue, its status, associated SLA deadlines, and relationships to users and tags.*

**Core Fields**

| Field | Type | Restrictions |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key, Required |
| `CreatorId` | `Guid` | Required |
| `AssignedOperatorId` | `Guid?` | Optional |
| `Subject` | `string` | Required, Length: 5–200 characters |
| `Description` | `string` | Required, Length: 15–10000 characters |
| `Status` | `Status` | Required, Valid Enum |
| `SlaFirstResponseDeadline` | `DateTime?` | Optional |
| `SlaHumanResponseDeadline` | `DateTime?` | Optional |
| `SlaResolutionDeadline` | `DateTime?` | Optional |
| `CreatedAt` | `DateTime` | Required |
| `UpdatedAt` | `DateTime` | Required |

**Relationships**
* `Creator`: Many-to-1 (`UserSnapshot`)
* `AssignedOperator`: Many-to-1 (`UserSnapshot`)
* `TicketTags`: 1-to-Many (`ICollection<TicketTag>`)
* `Messages`: 1-to-Many (`ICollection<TicketMessage>`)
* `AuditLogs`: 1-to-Many (`ICollection<TicketAuditLog>`)

---

### `Tag`
*Represents a tag that can be associated with support tickets for categorization and filtering purposes.*

**Core Fields**

| Field | Type | Restrictions |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key, Required |
| `OrganizationId` | `Guid` | Required |
| `Name` | `string` | Required, Length: 5–200 characters |
| `Description` | `string` | Required, Length: 15–10000 characters |
| `ExcludedFromAi` | `bool` | Required |

**Relationships**
* `TicketTags`: 1-to-Many (`ICollection<TicketTag>`)
* `OperatorTags`: 1-to-Many (`ICollection<OperatorTag>`)

---

### `TicketTag`
*Represents the many-to-many relationship between Tickets and Tags. Also tracks which user assigned the tag to the ticket for auditing purposes.*

**Core Fields**

| Field | Type | Restrictions |
| :--- | :--- | :--- |
| `TicketId` | `Guid` | Required |
| `TagId` | `Guid` | Required |
| `AssignedById` | `Guid` | Required |

**Relationships**
* `Ticket`: Many-to-1 (`Ticket`)
* `Tag`: Many-to-1 (`Tag`)
* `AssignedBy`: Many-to-1 (`UserSnapshot`)

---

### `OperatorTag`
*Represents the many-to-many relationship between support ticket operators and tags. Operators are assigned tags based on their expertise and skills, which can be used for intelligent ticket routing and assignment.*

**Core Fields**

| Field | Type | Restrictions |
| :--- | :--- | :--- |
| `OperatorId` | `Guid` | Required |
| `TagId` | `Guid` | Required |

**Relationships**
* `Tag`: Many-to-1 (`Tag`)
* `Operator`: Many-to-1 (`UserSnapshot`)

---

### `TicketMessage`
*Represents a message or comment added to a support ticket, which can be created by either the ticket creator, an operator, or an automated system. Each message is associated with a specific ticket and includes information about the sender and the content of the message.*

**Core Fields**

| Field | Type | Restrictions |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key, Required |
| `TicketId` | `Guid` | Required |
| `SenderId` | `Guid` | Required |
| `SenderType` | `Role` | Required, Valid Enum |
| `Message` | `string` | Required, Length: 1–2000 characters |
| `SentAt` | `DateTime` | Required |

**Relationships**
* `Ticket`: Many-to-1 (`Ticket`)
* `Sender`: Many-to-1 (`UserSnapshot`)

---

### `TicketAuditLog`
*Represents an audit log entry for changes made to a support ticket. Contains information about the change, who made it, and when it occurred. Used for tracking the history of actions taken on a ticket for accountability and troubleshooting purposes.*

**Core Fields**

| Field | Type | Restrictions |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key, Required |
| `TicketId` | `Guid` | Required |
| `ChangedById` | `Guid?` | Optional |
| `ActorType` | `Role` | Required, Valid Enum |
| `Action` | `string` | Required, Max Length: 255 |
| `OldValue` | `string?` | Optional, Max Length: 2000 |
| `NewValue` | `string?` | Optional, Max Length: 2000 |
| `Message` | `string` | Required, Length: 1–2000 characters |
| `CreatedAt` | `DateTime` | Required |

**Relationships**
* `Ticket`: Many-to-1 (`Ticket`)
* `ChangedBy`: Many-to-1 (`UserSnapshot`)

---

### `OrganizationSnapshot`
*Represents a snapshot of an organization's settings and configurations at the time of a user's interaction. The original table is stored in the organization access service, but a local copy is maintained to avoid cross-service calls during ticket operations.*

**Core Fields**

| Field | Type | Restrictions |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key, Required |
| `SlaFirstResponseMinutes` | `decimal` | Required |
| `AiTrustThreshold` | `decimal` | Required |
| `SlaResolutionMinutes` | `decimal` | Required |
| `SlaHumanResponseMinutes` | `decimal` | Required |

**Relationships**
* `Users`: 1-to-Many (`ICollection<UserSnapshot>`)

---

### `UserSnapshot`
*Represents a snapshot of a user at the time of ticket creation or modification. The original table is stored in the organization access service, but a local copy is maintained to avoid cross-service calls during ticket operations.*

**Core Fields**

| Field | Type | Restrictions |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key, Required |
| `OrganizationId` | `Guid` | Required |
| `Role` | `Role` | Required, Valid Enum |

**Relationships**
* `Organization`: Many-to-1 (`OrganizationSnapshot`)
* `TicketAuditLogs`: 1-to-Many (`ICollection<TicketAuditLog>`)
* `CreatedTickets`: 1-to-Many (`ICollection<Ticket>`)
* `AssignedTickets`: 1-to-Many (`ICollection<Ticket>`)
* `AssignedTicketTags`: 1-to-Many (`ICollection<TicketTag>`)

## Enums:
- `Status`: `NEW`, `IN_PROGRESS_LEVEL_1`, `IN_PROGRESS_LEVEL_2`, `UNASSIGNED`, `AWAITING_CUSTOMER_RESPONSE`, `CLOSED`, `IN_KNOWLEDGE_BASE`, `NOT_IN_KNOWLEDGE_BASE`, `SOLUTION_IMPOSSIBLE`.
- `Role`: `AI`, `EMPLOYEE`, `LEVEL_1_OPERATOR`, `LEVEL_2_OPERATOR`, `ORGANIZATION_MANAGER`, `SUPPORT_HUB_ADMINISTRATOR`, `SYSTEM`.
- `SlaType`: `FIRST_RESPONSE`, `HUMAN_RESPONSE`, `RESOLUTION`.
- `AiFailureCode`: `AI_SERVICE_UNAVAILABLE`, `AI_RESPONSE_INVALID`, `INVALID_TICKET_DATA`, `TRIAGE_TECHNICAL_ERROR`.

## API endpoints
All endpoints require a valid JWT; the user ID is read from the `NameIdentifier` claim.

### Tickets
- `POST /api/tickets`
  - Body: `{ "subject": "...", "description": "..." }`
  - Creates a new ticket.
  - **Publishes:** `TicketCreated`
- `GET /api/tickets`
  - Query: `organizationId`, `creatorId`, `assignedOperatorId`, `createdAfter`, `createdBefore`, `sortBy`, `sortDescending`, `pageNumber`, `pageSize`
  - Returns a filtered, paged list.
- `GET /api/tickets/{ticketId}`
  - Returns ticket details.
- `PUT /api/tickets/{ticketId}`
  - Body: `{ "Status": "...", "OperatorId": "..." }`
  - Updates status and/or assigned operator.
  - **Publishes:** `TicketStatusChanged` (if status changed), `TicketReassigned` (if operator changed)
- `DELETE /api/tickets/{ticketId}`
  - Deletes a ticket.
  - **Publishes:** `TicketDeleted`
- `POST /api/tickets/{ticketId}/evaluate-ai-response`
  - Body: `{ "isHelpful": true|false, "feedback": "..." }`
  - Records AI response feedback; may close or escalate the ticket.
  - **Publishes:** `TicketEscalated` (if not helpful), `TicketStatusChanged` (if helpful)
- `POST /api/tickets/{ticketId}/propose-article`
  - Body: `{ "proposedSubject": "...", "tags": ["..."] }`
  - Proposes a knowledge base article from a ticket.
  - **Publishes:** `KnowledgeArticleProposed`

### Ticket messages
- `POST /api/tickets/{ticketId}/messages`
  - Body: `{ "message": "..." }`
  - Adds a message to a ticket.
  - **Publishes:** `TicketMessageAdded`
- `GET /api/tickets/messages`
  - Query: `ticketId`, `senderId`, `senderType`, `sentAfter`, `sentBefore`, `sortBy`, `sortDescending`, `pageNumber`, `pageSize`
  - Returns a filtered, paged list.
- `GET /api/tickets/messages/{ticketMessageId}`
  - Returns message details.
- `PUT /api/tickets/messages/{ticketMessageId}`
  - Body: `{ "message": "..." }`
  - Updates a ticket message.
- `DELETE /api/tickets/messages/{ticketMessageId}`
  - Deletes a ticket message.

### Ticket tags
- `POST /api/tickets/{ticketId}/tags/{tagId}`
  - Adds a tag to a ticket.
  - **Publishes:** `TicketTagUpdated`
- `DELETE /api/tickets/{ticketId}/tags/{tagId}`
  - Removes a tag from a ticket.
  - **Publishes:** `TicketTagUpdated`
- `GET /api/tickets/ticket-tags`
  - Query: `tagId`, `ticketId`, `assignedById`, `sortBy`, `sortDescending`, `pageNumber`, `pageSize`
  - Returns a filtered, paged list.

### Tags
- `POST /api/tickets/tags`
  - Body: `{ "name": "...", "organizationId": "...", "description": "...", "excludedFromAi": true|false }`
  - Creates a tag.
  - **Publishes:** `TagCreated`
- `GET /api/tickets/tags`
  - Query: `organizationId`, `excludedFromAi`, `sortBy`, `sortDescending`, `pageNumber`, `pageSize`
  - Returns a filtered, paged list.
- `GET /api/tickets/tags/{tagId}`
  - Returns tag details.
- `PUT /api/tickets/tags/{tagId}`
  - Body: `{ "excludedFromAi": true|false }`
  - Updates AI inclusion flag.
  - **Publishes:** `TagAISettingUpdated`
- `DELETE /api/tickets/tags/{tagId}`
  - Deletes a tag.
  - **Publishes:** `TagDeleted`

### Operator tags
- `POST /api/tickets/tags/{tagId}/operators`
  - Body: `{ "operatorId": "..." }`
  - Adds a tag to an operator.
  - **Publishes:** `OperatorTagAdded`
- `DELETE /api/tickets/tags/{tagId}/operators/{operatorId}`
  - Removes a tag from an operator.
  - **Publishes:** `OperatorTagRemoved`
- `GET /api/tickets/tags/operators`
  - Query: `tagId`, `operatorId`, `sortBy`, `sortDescending`, `pageNumber`, `pageSize`
  - Returns a filtered, paged list.

### Ticket audit logs
- `GET /api/tickets/logs`
  - Query: `ticketId`, `changedById`, `actorType`, `action`, `oldValue`, `newValue`, `createdAfter`, `createdBefore`, `organizationId`, `sortBy`, `sortDescending`, `pageNumber`, `pageSize`
  - Returns a filtered, paged list.
- `DELETE /api/tickets/logs/{ticketAuditLogId}`
  - Deletes an audit log entry.

### Health and metrics
- `GET /health/live`
- `GET /health/ready`
- `GET /metrics`

## Kafka events

### Envelope format
All published events are wrapped in a standard envelope:
{
  "metadata": {
    "eventId": "guid",
    "eventType": "string",
    "version": 1,
    "timestamp": "ISO-8601"
  },
  "payload": { ... }
}

### Outgoing events
Event type strings and payloads:
- `TicketCreated` → `TicketCreatedEvent`: `TicketId`, `OrganizationId`, `CreatorId`, `Subject`, `Description`
- `TicketStatusChanged` → `TicketStatusChangedEvent`: `TicketId`, `OrganizationId`, `OldStatus`, `NewStatus`, `ChangedById`, `ActorType`
- `TicketReassigned` → `TicketReasignedEvent`: `TicketId`, `OrganizationId`, `ReassignedById`, `PreviousOperatorId`, `NewOperatorId`
- `TicketDeleted` → `TicketDeletedEvent`: `TicketId`, `OrganizationId`, `DeletedById`, `ActorType`
- `KnowledgeArticleProposed` → `KnowledgeArticleProposedEvent`: `TicketId`, `OperatorId`, `ProposerId`, `ProposedSubject`, `Tags`
- `TicketMessageAdded` → `TicketMessageAddedEvent`: `TicketId`, `OrganizationId`, `MessageId`, `SenderId`, `SenderType`, `Message`
- `TicketTagUpdated` → `TicketTagUpdatedEvent`: `TicketId`, `OrganizationId`, `UpdatedById`, `CurrentTags`
- `TicketEscalated` → `TicketEscalatedEvent`: `TicketId`, `OrganizationId`, `EscalatedById`, `TargetLevel`
- `SlaDeadlineApproaching` → `SlaDeadlineApproachingEvent`: `TicketId`, `OrganizationId`, `SlaType`, `DeadlineTime`, `MinutesRemaining`
- `SlaDeadlineBreached` → `SlaDeadlineBreachedEvent`: `TicketId`, `OrganizationId`, `SlaType`, `DeadlineTime`
- `OperatorTagAdded` → `OperatorTagAddedEvent`: `OperatorId`, `TagId`, `AddedById`, `TagName`
- `OperatorTagRemoved` → `OperatorTagRemovedEvent`: `OperatorId`, `TagId`, `RemovedById`, `TagName`
- `TagCreated` → `TagCreatedEvent`: `TagId`, `OrganizationId`, `TagName`
- `TagDeleted` → `TagDeletedEvent`: `TagId`, `OrganizationId`, `DeletedById`
- `TagAISettingUpdated` → `TagAISettingUpdatedEvent`: `TagId`, `OrganizationId`, `UpdatedById`, `ExludeFromAi`

### Incoming events
Event type strings and payloads:
- `AiAnalysisCompleted` → `AiAnalysisCompletedPayload`: `AnalysisId`, `TicketId`, `OrganizationId`, `TagSuggestions`, `AnswerText`, `UsedKnowledgeArticleIds`, `ConfidenceScore`
- `AiAnalysisFailed` → `AiAnalysisFailedPayload`: `AnalysisId`, `TicketId`, `OrganizationId`, `FailureCode`, `FailureMessage`
- `KnowledgeArticleApproved` → `KnowledgeArticlePublishedPayload`: `ArticleId`, `DraftId`, `SourceTicketId`, `OrganizationId`, `PublishedById`, `Title`, `ArticleContent`, `Tags`
- `KnowledgeArticleRejected` → `KnowledgeArticleRejectedPayload`: `DraftId`, `SourceTicketId`, `OrganizationId`, `RejectedById`, `RejectionReason`
- `UserInvitationAccepted` → `UserInvitationAcceptedPayload`: `OrganizationId`, `UserId`, `UserEmail`, `Role`
- `UserRoleUpdated` → `UserRoleUpdatedPayload`: `OrganizationId`, `UserId`, `OldRole`, `NewRole`
- `UserRemovedFromOrganization` → `UserRemovedFromOrganizationPayload`: `OrganizationId`, `UserId`, `UserEmail`
- `OrganizationCreated` → `OrganizationCreatedPayload`: `OrganizationId`, `OrganizationName`, `Tier`, `ManagerId`
- `OrganizationSettingsUpdated` → `OrganizationSettingsUpdatedPayload`: `OrganizationId`, `SlaFirstResponseMinutes`, `SlaHumanResponseMinutes`, `SlaResolutionMinutes`, `AiTrustThreshold`
- `OrganizationDeleted` → `OrganizationDeletedPayload`: `OrganizationId`, `OrganizationName`

Dead-letter queue behavior:
- If processing fails, the raw message is sent to `DeadLetterQueueTopic` with failure metadata in headers.

## How incoming Kafka events are handled
Incoming events are consumed by a hosted Kafka consumer service, deserialized based on `metadata.eventType`, and routed to MediatR command handlers:

- `AiAnalysisCompleted`
  - Applies AI tag suggestions, adds an AI message, and writes audit logs.
  - If confidence meets organization threshold: sets status to `AWAITING_CUSTOMER_RESPONSE` and publishes `TicketStatusChanged`.
  - Otherwise: sets status to `UNASSIGNED`, publishes `TicketEscalated`.
  - Publishes `TicketMessageAdded` and, when tags change, `TicketTagUpdated`.
- `AiAnalysisFailed`
  - Sets status to `UNASSIGNED`, adds AI message and audit log.
  - Publishes `TicketMessageAdded` and `TicketEscalated`.
- `KnowledgeArticleApproved`
  - Sets status to `IN_KNOWLEDGE_BASE`, logs audit, publishes `TicketStatusChanged`.
- `KnowledgeArticleRejected`
  - Sets status to `NOT_IN_KNOWLEDGE_BASE`, logs audit, publishes `TicketStatusChanged`.
- `UserInvitationAccepted`
  - Creates a new `UserSnapshot`.
- `UserRoleUpdated`
  - Updates the stored role in `UserSnapshot`.
- `UserRemovedFromOrganization`
  - Reassigns created tickets to the AI user, unassigns operator tickets, writes audit logs.
  - Publishes `TicketStatusChanged` for impacted tickets.
  - Removes the user snapshot.
- `OrganizationCreated`
  - Creates an `OrganizationSnapshot` plus a manager and AI `UserSnapshot`.
- `OrganizationSettingsUpdated`
  - Updates SLA thresholds and AI trust threshold in `OrganizationSnapshot`.
- `OrganizationDeleted`
  - Deletes organization snapshot and all related tickets, tags, and users.

## SlaMonitoringBackgroundService
A hosted background service designed to monitor SLA deadlines on a fixed interval (1 minute). When running, it:

- Checks `SlaFirstResponseDeadline` for tickets in `NEW` status:
  - Publishes `SlaDeadlineApproaching` when a deadline is within the next minute.
  - When breached, sets ticket to `UNASSIGNED`, clears SLA fields, and publishes `SlaDeadlineBreached` plus `TicketEscalated`.
- Checks `SlaHumanResponseDeadline` for tickets in `IN_PROGRESS_LEVEL_1` or `IN_PROGRESS_LEVEL_2`:
  - Publishes `SlaDeadlineApproaching` when within the next minute.
  - When breached, sets ticket to `UNASSIGNED`, clears relevant SLA fields, and publishes `SlaDeadlineBreached` plus `TicketStatusChanged`.
- Checks `SlaResolutionDeadline` for tickets not in `NOT_IN_KNOWLEDGE_BASE`, `IN_KNOWLEDGE_BASE`, `CLOSED`, or `SOLUTION_IMPOSSIBLE`:
  - Publishes `SlaDeadlineApproaching` when within the next minute.
  - When breached, sets ticket to `UNASSIGNED`, clears relevant SLA fields, and publishes `SlaDeadlineBreached` plus `TicketStatusChanged`.

Note: This service must be registered with `AddHostedService` to run.