using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tickets.UpdateTicket;

public class UpdateTicketHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<UpdateTicketCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket to be updated and check if it exists, otherwise return 404 Not Found
        Ticket? ticket = await _db.Tickets
            .Include(t => t.AssignedOperator)
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        if (ticket == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");

        //Check if the user exists and return 401 Unauthorized if not
        var user = await _db.UserSnapshots.Where(u => u.Id == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Enforce strict authorization rules
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = user.Role == Role.ORGANIZATION_MANAGER && user.OrganizationId == ticket.Creator.OrganizationId;
        bool isAssignedOperator = ticket.AssignedOperatorId.HasValue && ticket.AssignedOperatorId.Value == request.UserId;
        if (!isSystemAdmin && !isOrganizationManager && !isAssignedOperator)
            throw new AppException(StatusCodes.Status403Forbidden, "User does not have permission to update this ticket.");

        //Determine what actually changed to conditionally publish events
        bool statusChanged = ticket.Status != request.TargetStatus;
        bool operatorChanged = request.TargetOperatorId.HasValue && ticket.AssignedOperatorId != request.TargetOperatorId;

        //If operator is being changed, verify the new operator exists, has the correct role and belongs to the correct organization, otherwise return 400 Bad Request
        if (operatorChanged)
        {
#pragma warning disable CS8629 //Already checked that TargetOperatorId has value with the operatorChanged condition
            var newOperator = await _db.UserSnapshots.Where(u => u.Id == request.TargetOperatorId.Value)
                .FirstOrDefaultAsync(cancellationToken);
#pragma warning restore CS8629
            if (newOperator == null)
                throw new AppException(StatusCodes.Status400BadRequest, "Target operator could not be found.");
            if (newOperator.Role != Role.LEVEL_1_OPERATOR && newOperator.Role != Role.LEVEL_2_OPERATOR)
                throw new AppException(StatusCodes.Status400BadRequest, "Target operator must have SUPPORT_HUB_OPERATOR role.");
            if (newOperator.OrganizationId != ticket.Creator.OrganizationId)
                throw new AppException(StatusCodes.Status400BadRequest, "Target operator must belong to the same organization as the ticket.");
        }

        //Store old values for event publishing
        Status oldStatus = ticket.Status;
        Guid? oldOperatorId = ticket.AssignedOperatorId;

        //Apply updates to the ticket
        ticket.Status = request.TargetStatus;
        if (operatorChanged)
            ticket.AssignedOperatorId = request.TargetOperatorId;

        //Log the ticket update
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