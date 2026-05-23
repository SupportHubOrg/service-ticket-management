using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessKnowledgeArticleRejectedCommand(KnowledgeArticleRejectedPayload Payload) : IRequest;

public class ProcessKnowledgeArticleRejectedHandler(
    TicketDbContext db,
    IEventPublisher publisher,
    ILogger<ProcessKnowledgeArticleRejectedHandler> logger) : IRequestHandler<ProcessKnowledgeArticleRejectedCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;
    private readonly ILogger<ProcessKnowledgeArticleRejectedHandler> _logger = logger;

    public async Task Handle(ProcessKnowledgeArticleRejectedCommand request, CancellationToken cancellationToken)
    {
        //Get the payload from the command
        KnowledgeArticleRejectedPayload payload = request.Payload;

        //Fetch the ticket from the database
        var ticket = await _db.Tickets
            .Include(t => t.Creator)
                .ThenInclude(c => c.Organization)
            .FirstOrDefaultAsync(t => t.Id == payload.SourceTicketId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket with ID {TicketId} not found for knowledge base sync processing.", payload.SourceTicketId);
            return;
        }

        //Get the user snapshot of the person who rejected the knowledge article to use as the actor of the ticket update
        UserSnapshot? rejectingUser = await _db.UserSnapshots.Where(u => u.Id == payload.RejectedById)
            .FirstOrDefaultAsync(cancellationToken);
        if (rejectingUser == null)
        {
            _logger.LogWarning("Rejecting user with ID {UserId} not found for knowledge base sync processing.", payload.RejectedById);
            return;
        }

        //Save the previous status for the audit log
        Status previousStatus = ticket.Status;

        //Update the ticket status
        ticket.Status = Status.NOT_IN_KNOWLEDGE_BASE;

        //Add a new audit log entry for the knowledge article rejection event
        TicketAuditLog ticketAuditLog = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangedById = rejectingUser.Id,
            ActorType = rejectingUser.Role,
            Action = "Knowledge Base Sync",
            OldValue = $"Status: {previousStatus}",
            NewValue = $"Status: {ticket.Status}",
            Message = $"Ticket {ticket.Id} marked as NOT_IN_KNOWLEDGE_BASE after knowledge article rejection. Rejection reason: {payload.RejectionReason}",
            CreatedAt = DateTime.UtcNow
        };
        _db.TicketAuditLogs.Add(ticketAuditLog);

        //Save the changes to the database
        await _db.SaveChangesAsync(cancellationToken);

        //Publish an event to notify the ticket status change
        TicketStatusChangedEvent ticketStatusChangedEvent = new(ticket.Id, ticket.Creator.OrganizationId, previousStatus, ticket.Status, payload.RejectedById, rejectingUser.Role);
            await _publisher.PublishAsync("TicketStatusChanged", ticketStatusChangedEvent, cancellationToken);
    }
}