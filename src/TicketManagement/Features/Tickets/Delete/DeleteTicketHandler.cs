using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Tickets.DeleteTicket;

public class DeleteTicketHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<DeleteTicketCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(DeleteTicketCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket to be deleted and check if it exists, otherwise return 404 Not Found
        Ticket? ticket = await _db.Tickets
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        if (ticket == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");

        //Retrieve the user to verify permissions and check if they exist, otherwise return 401 Unauthorized
        UserSnapshot? user = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, $"User {request.UserId} could not be found.");

        //Enforce strict authorization rules for deletion
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = user.Role == Role.ORGANIZATION_MANAGER && user.OrganizationId == ticket.Creator.OrganizationId;
        if (!isSystemAdmin && !isOrganizationManager)
            throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} attempted to delete ticket {request.TicketId} without sufficient permissions. Only Organization Managers or SupportHub Administrators can delete tickets.");

        //Remove the ticket from the database
        _db.Tickets.Remove(ticket);

        //Log the ticket deletion in the audit log
        TicketAuditLog ticketAuditLog = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangedById = request.UserId,
            ActorType = user.Role,
            Action = "Ticket Deleted",
            OldValue = $"Ticket {request.TicketId} with subject '{ticket.Subject}' deleted",
            NewValue = null,
            Message = $"User {request.UserId} deleted ticket {request.TicketId}.",
            CreatedAt = DateTime.UtcNow
        };
        _db.TicketAuditLogs.Add(ticketAuditLog);

        await _db.SaveChangesAsync(cancellationToken);

        //Publish an event to notify other services of the deletion
        var ticketDeletedEvent = new TicketDeletedEvent(ticket.Id, ticket.Creator.OrganizationId, request.UserId, user.Role);
        await _publisher.PublishAsync("TicketCreated", ticketDeletedEvent, cancellationToken);

        return true;
    }
}