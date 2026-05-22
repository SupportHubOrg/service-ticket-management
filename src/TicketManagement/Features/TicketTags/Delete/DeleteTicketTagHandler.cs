using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.TicketTags.DeleteTicketTag;

public class DeleteTicketTagHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<DeleteTicketTagCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(DeleteTicketTagCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket tag to be deleted and check if it exists, return 404 Not Found if not
        TicketTag? ticketTag = await _db.TicketTags
            .Include(t => t.Ticket)
            .FirstOrDefaultAsync(t => t.TicketId == request.TicketId && t.TagId == request.TagId, cancellationToken);
        if (ticketTag == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");

        //Retrieve the user and check if they exist, return 401 Unauthorized if not
        UserSnapshot? user = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, $"User {request.UserId} could not be found.");

        //Check if the assignee is the assigned operator, administator or the organization manager, otherwise return 403 Forbidden
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = (user.Role == Role.ORGANIZATION_MANAGER || user.Role == Role.AI) && user.OrganizationId == ticketTag.Ticket.Creator.OrganizationId;
        bool isAssignedOperator = ticketTag.Ticket.AssignedOperatorId != null && ticketTag.Ticket.AssignedOperatorId == request.UserId;
        if (!isSystemAdmin && !isOrganizationManager && !isAssignedOperator)
            throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} attempted to delete tag {request.TagId} from ticket {request.TicketId} without sufficient permissions. Only the assigned operator, Organization Managers or SupportHub Administrators can remove tags from a ticket.");

        //Remove the ticket tag from the database
        _db.TicketTags.Remove(ticketTag);
        await _db.SaveChangesAsync(cancellationToken);

        //Log the ticket tag removal in the audit log
        TicketAuditLog ticketAuditLog = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketTag.Ticket.Id,
            ChangedById = request.UserId,
            ActorType = user.Role,
            Action = "Ticket Tag Removed",
            OldValue = $"TagId: {request.TagId}, AssignedById: {request.UserId}",
            NewValue = null,
            Message = $"User {request.UserId} removed tag {request.TagId} from ticket {request.TicketId}.",
            CreatedAt = DateTime.UtcNow
        };
        _db.TicketAuditLogs.Add(ticketAuditLog);

        //Save changes to the database
        await _db.SaveChangesAsync(cancellationToken);

        //Get the tag names into an array to include in the event payload
        var tagNames = await _db.TicketTags
            .Where(tt => tt.TicketId == ticketTag.Ticket.Id)
            .Join(_db.Tags, tt => tt.TagId, t => t.Id, (tt, t) => t.Name)
            .ToArrayAsync(cancellationToken);

        //Publish the tag update event to Kafka
        var ticketTagUpdatedEvent = new TicketTagUpdatedEvent(ticketTag.Ticket.Id, ticketTag.Ticket.Creator.OrganizationId, request.UserId, tagNames); 
        await _publisher.PublishAsync("TicketTagUpdatedEvent", ticketTagUpdatedEvent, cancellationToken);

        return true;
    }
}