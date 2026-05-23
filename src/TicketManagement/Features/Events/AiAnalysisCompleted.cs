using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessAiAnalysisCompletedCommand(AiAnalysisCompletedPayload Payload) : IRequest;

public class ProcessAiAnalysisCompletedHandler(
    TicketDbContext db, 
    IEventPublisher publisher,
    ILogger<ProcessAiAnalysisCompletedHandler> logger) : IRequestHandler<ProcessAiAnalysisCompletedCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;
    private readonly ILogger<ProcessAiAnalysisCompletedHandler> _logger = logger;

    public async Task Handle(ProcessAiAnalysisCompletedCommand request, CancellationToken cancellationToken)
    {
        //Get the payload from the command
        AiAnalysisCompletedPayload payload = request.Payload;

        //Fetch the ticket from the database for the treshold validation
        var ticket = await _db.Tickets
            .Include(t => t.Creator)
                .ThenInclude(c => c.Organization)
            .FirstOrDefaultAsync(t => t.Id == payload.TicketId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket with ID {TicketId} not found for AI analysis completion processing.", payload.TicketId);
            return;
        }

        //Retrieve the AI user snapshot to use as the sender of the AI message
        UserSnapshot? aiUser = await _db.UserSnapshots.Where(u => u.Role == Role.AI)
            .FirstOrDefaultAsync(cancellationToken);
        if (aiUser == null)
        {
            _logger.LogError("No AI user found in the database for AI analysis completion processing.");
            return;
        }

        //Apply AI suggested tags to the ticket
        bool hasNewTags = false;
        foreach (var tagSuggestion in payload.TagSuggestions)
        {
            //Check if the tag already exists for the ticket to avoid duplicates
            bool tagExists = await _db.TicketTags.AnyAsync(tt => tt.TicketId == payload.TicketId && tt.TagId == tagSuggestion, cancellationToken);
            if (!tagExists)
            {
                //Add the new tag to the ticket
                TicketTag newTag = new()
                {
                    TicketId = payload.TicketId,
                    TagId = tagSuggestion,
                    AssignedById = aiUser.Id
                };
                _db.TicketTags.Add(newTag);

                hasNewTags = true;
            }
        }

        //Store the message to add to the ticket with the AI analysis results and insights
        string aiInsightsMessage;

        //Evaluate if the AI confidence score meets the trust threshold defined in the organization settings
        bool isTrusted = payload.ConfidenceScore >= (double)ticket.Creator.Organization.AiTrustThreshold;
        if (isTrusted)
        {
            //If the AI analysis is trusted, update the ticket status to reflect that AI insights are available
            ticket.Status = Status.AWAITING_CUSTOMER_RESPONSE;

            //Publish an event to notify that the ticket status has been updated based on the AI analysis results
            TicketStatusChangedEvent ticketStatusChangedEvent = new(ticket.Id, ticket.Creator.OrganizationId, Status.NEW, ticket.Status, aiUser.Id, Role.AI);
            await _publisher.PublishAsync("TicketStatusChanged", ticketStatusChangedEvent, cancellationToken);

            //Add the AI insights to the message that will be added to the ticket
            aiInsightsMessage = "Automated AI Response:\n\n" + payload.AnswerText;
        }
        else
        {
            //If the AI analysis is not trusted, update the ticket status to indicate that human review is needed
            ticket.Status = Status.UNASSIGNED;
            ticket.AssignedOperatorId = null;

            //Send an event to notify that the ticket has been escalated for human review due to low AI confidence score
            TicketEscalatedEvent ticketEscalatedEvent = new(ticket.Id, ticket.Creator.OrganizationId, ticket.AssignedOperatorId ?? Guid.Empty, ticket.Status);
            await _publisher.PublishAsync("TicketEscalated", ticketEscalatedEvent, cancellationToken);

            _logger.LogInformation("AI analysis for Ticket ID {TicketId} did not meet the required confidence level. Marking ticket for human review.", payload.TicketId);

            //Add a message indicating that the AI analysis was completed but did not meet the trust thresholdand that human review is needed
            aiInsightsMessage = "AI analysis had completed but did not meet the required confidence level. The ticket has been redirected to a human reviever.";
        }

        //Add a new message to the ticket with the AI analysis results
        TicketMessage aiMessage = new()
        {
            Id = Guid.NewGuid(),
            TicketId = payload.TicketId,
            SenderId = aiUser.Id,
            SenderType = Role.AI,
            Message = aiInsightsMessage,
            SentAt = DateTime.UtcNow
        };
        _db.TicketMessages.Add(aiMessage);

        //Log the ticket creation
        TicketAuditLog ticketAuditLog = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangedById = aiUser.Id,
            ActorType = Role.AI,
            Action = isTrusted ? "AI Analysis Completed - Trusted" : "AI Analysis Completed - Not Trusted",
            OldValue = isTrusted ? $"Status: {Status.NEW}" : $"Status: {Status.NEW}, OperatorId: {aiUser.Id}",
            NewValue = isTrusted ? $"Status: {ticket.Status}" : $"Status: {ticket.Status}, OperatorId: null",
            Message = $"AI analysis completed for ticket {ticket.Id} with confidence score {payload.ConfidenceScore}. {(isTrusted ? "Insights added to the ticket and status updated." : "Ticket marked for human review due to low confidence score.")}",
            CreatedAt = DateTime.UtcNow
        };
        _db.TicketAuditLogs.Add(ticketAuditLog);

        //Save changes to the database
        await _db.SaveChangesAsync(cancellationToken);

        //Publish an event to notify that a new AI message has been added to the ticket
        var aiMessageAddedEvent = new TicketMessageAddedEvent(
            payload.TicketId,
            ticket.Creator.OrganizationId,
            aiMessage.Id,
            aiMessage.SenderId,
            aiMessage.SenderType,
            aiMessage.Message
        );
        await _publisher.PublishAsync("TicketMessageAdded", aiMessageAddedEvent, cancellationToken);

        //If there are new tags added, publish an event to update the ticket tags in other services
        if (hasNewTags)
        {
            var currentTags = await _db.TicketTags
                .Include(tt => tt.Tag)
                .Where(tt => tt.TicketId == payload.TicketId)
                .Select(tt => tt.TagId)
                .ToArrayAsync(cancellationToken);

            TicketTagUpdatedEvent ticketTagUpdatedEvent = new(payload.TicketId, ticket.Creator.OrganizationId, aiUser.Id, currentTags);
            await _publisher.PublishAsync("TicketTagUpdated", ticketTagUpdatedEvent, cancellationToken);
        }
    }
}