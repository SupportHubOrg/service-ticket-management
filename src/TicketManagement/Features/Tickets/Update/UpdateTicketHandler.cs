using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tickets.UpdateTicket;

/// <summary>
/// Handles updating a ticket's status and/or assigned operator.
/// </summary>
/// <param name="db"></param>
/// <param name="publisher"></param>
public class UpdateTicketHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<UpdateTicketCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket to be updated
        Ticket? ticket = await _db.Tickets
            .Include(t => t.AssignedOperator)
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        
        //If the ticket doesn't exist, return 404 Not Found
        if (ticket == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");

        //Ensure the user has permission to update the ticket
        var user = await _db.UserSnapshots.Where(u => u.Id == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (user == null || user.OrganizationId != ticket.Creator.OrganizationId)
            throw new AppException(StatusCodes.Status401Unauthorized, "User does not have access to this ticket.");
        if (user.Role != Role.ORGANIZATION_MANAGER && user.Role != Role.SUPPORT_HUB_ADMINISTRATOR && user.Role != Role.SYSTEM && user.Id != ticket.AssignedOperatorId)
            throw new AppException(StatusCodes.Status403Forbidden, "User does not have permission to update this ticket.");

        //Determine what actually changed to conditionally publish events
        bool statusChanged = ticket.Status != request.TargetStatus;
        bool operatorChanged = request.TargetOperatorId.HasValue && ticket.AssignedOperatorId != request.TargetOperatorId;

        //Store old values for event publishing
        Status oldStatus = ticket.Status;
        Guid? oldOperatorId = ticket.AssignedOperatorId;

        //Apply updates to the ticket
        ticket.Status = request.TargetStatus;
        if (operatorChanged)
            ticket.AssignedOperatorId = request.TargetOperatorId;

        //Log the ticket creation
        TicketAuditLog ticketAuditLog = new TicketAuditLog
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangedById = request.UserId,
            ActorType = user.Role,
            Action = "Ticket Updated",
            OldValue = $"Status: {oldStatus}, AssignedOperatorId: {oldOperatorId}",
            NewValue = $"Status: {ticket.Status}, AssignedOperatorId: {ticket.AssignedOperatorId}",
            Message = $"Ticket updated by User {request.UserId}. Status changed: {statusChanged}. Operator changed: {operatorChanged}.",
            CreatedAt = DateTime.UtcNow
        };
        _db.TicketAuditLogs.Add(ticketAuditLog);

        //Save changes to the database
        await _db.SaveChangesAsync(cancellationToken);

        //Publish events based on what changed
        if (statusChanged)
        {
            await _publisher.PublishAsync("TicketStatusChanged",
                new TicketStatusChangedEvent(ticket.Id, ticket.Creator.OrganizationId, oldStatus, ticket.Status, request.UserId, user.Role),
            cancellationToken);
        }

        if (operatorChanged)
        {
            await _publisher.PublishAsync("TicketReassigned",
                new TicketReasignedEvent(ticket.Id, ticket.Creator.OrganizationId, request.UserId, oldOperatorId, request.TargetOperatorId),
                cancellationToken);
        }

        return true;
    }
}