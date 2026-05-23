using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessAiFailureCommand(AiAnalysisFailedPayload Payload) : IRequest;

public class ProcessAiFailureHandler(
    TicketDbContext db,
    IEventPublisher publisher,
    ILogger<ProcessAiFailureHandler> logger) : IRequestHandler<ProcessAiFailureCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;
    private readonly ILogger<ProcessAiFailureHandler> _logger = logger;

    public async Task Handle(ProcessAiFailureCommand request, CancellationToken cancellationToken)
    {
        //Get the payload from the command
        AiAnalysisFailedPayload payload = request.Payload;

        //Fetch the ticket from the database
        var ticket = await _db.Tickets
            .Include(t => t.Creator)
                .ThenInclude(c => c.Organization)
            .FirstOrDefaultAsync(t => t.Id == payload.TicketId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket with ID {TicketId} not found for AI failure processing.", payload.TicketId);
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

        //Update the ticket status to Unassigned and clear the assigned operator
        ticket.Status = Status.UNASSIGNED;
        ticket.AssignedOperatorId = null;

        //Add a new audit log entry for the AI failure event
        TicketAuditLog ticketAuditLog = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangedById = ticket.CreatorId,
            ActorType = Role.AI,
            Action = "AI Analysis Failed",
            OldValue = $"Status: {Status.NEW}",
            NewValue = $"Status: {Status.UNASSIGNED}, OperatorId: null",
            Message = $"AI analysis failed for ticket {ticket.Id}. Ticket status set to UNASSIGNED for human review.",
            CreatedAt = DateTime.UtcNow
        };
        _db.TicketAuditLogs.Add(ticketAuditLog);

        //Add a new message to the ticket indicating that the AI analysis failed and the ticket has been marked for human review
        TicketMessage aiMessage = new()
        {
            Id = Guid.NewGuid(),
            TicketId = payload.TicketId,
            SenderId = aiUser.Id,
            SenderType = Role.AI,
            Message = $"AI analysis failed with error code {payload.FailureCode} and message: {payload.FailureMessage}.\nThe ticket has been marked for human review.",
            SentAt = DateTime.UtcNow
        };
        _db.TicketMessages.Add(aiMessage);

        //Save the changes to the database
        await _db.SaveChangesAsync(cancellationToken);

        //Publish an event to notify that a new AI failure message has been added to the ticket
        TicketMessageAddedEvent aiMessageAddedEvent = new(
            payload.TicketId,
            ticket.Creator.OrganizationId,
            aiMessage.Id,
            aiMessage.SenderId,
            aiMessage.SenderType,
            aiMessage.Message
        );
        await _publisher.PublishAsync("TicketMessageAdded", aiMessageAddedEvent, cancellationToken);

        //Publish the TicketEscalated event to Kafka
        TicketEscalatedEvent ticketEscalatedEvent = new(ticket.Id, ticket.Creator.OrganizationId, ticket.AssignedOperatorId ?? Guid.Empty, ticket.Status);
        await _publisher.PublishAsync("TicketEscalated", ticketEscalatedEvent, cancellationToken);
}
}