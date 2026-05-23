using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketTags.CreateTicketTag;

public class CreateTicketTagHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<CreateTicketTagCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(CreateTicketTagCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the user and check if they exist, returning 401 Unauthorized if not found
        UserSnapshot? user = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == request.AssignedById, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Get the ticket and check if it exists, otherwise return 404 Not Found
        Ticket? ticket = await _db.Tickets
            .Include(t => t.Creator)
            .Include(t => t.TicketTags)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        if (ticket == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");

        //Get the tag and check if it exists, otherwise return 404 Not Found
        Tag? tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken);
        if (tag == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Tag {request.TagId} could not be found.");

        //Check if the ticket does not already have the tag, if it does return 400 Bad Request
        bool alreadyHasTag = await _db.TicketTags.AnyAsync(tt => tt.TicketId == request.TicketId && tt.TagId == request.TagId, cancellationToken);
        if (alreadyHasTag)
            throw new AppException(StatusCodes.Status400BadRequest, $"Ticket {request.TicketId} already has tag {request.TagId}.");

        //Check if the assignee is the assigned operator, administator or the organization manager, otherwise return 403 Forbidden
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isTagInOrganization = tag.OrganizationId == ticket.Creator.OrganizationId;
        bool isOrganizationManager = (user.Role == Role.ORGANIZATION_MANAGER || user.Role == Role.AI) && user.OrganizationId == ticket.Creator.OrganizationId && isTagInOrganization;
        bool isAssignedOperator = ticket.AssignedOperatorId != null && ticket.AssignedOperatorId == request.AssignedById && isTagInOrganization;
        if (!isSystemAdmin && !isOrganizationManager && !isAssignedOperator)
            throw new AppException(StatusCodes.Status403Forbidden, $"User {request.AssignedById} attempted to add a tag to ticket {request.TicketId} without sufficient permissions. Only the assigned operator, Organization Managers or SupportHub Administrators can add tags to a ticket.");

        //Create the new ticket tag
        var ticketTag = new TicketTag
        {
            TicketId = request.TicketId,
            TagId = request.TagId,
            AssignedById = request.AssignedById
        };

        //Save the ticket tag to the database
        _db.TicketTags.Add(ticketTag);

        //Log the ticket tag addition in the audit log
        TicketAuditLog ticketAuditLog = new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangedById = request.AssignedById,
            ActorType = user.Role,
            Action = "Ticket Tag Added",
            OldValue = null,
            NewValue = $"TagId: {request.TagId}, AssignedById: {request.AssignedById}",
            Message = $"User {request.AssignedById} added tag {request.TagId} to ticket {request.TicketId}.",
            CreatedAt = DateTime.UtcNow
        };
        _db.TicketAuditLogs.Add(ticketAuditLog);

        //Save changes to the database
        await _db.SaveChangesAsync(cancellationToken);

        //Get the tag names into an array to include in the event payload
        var tagNames = await _db.TicketTags
            .Where(tt => tt.TicketId == ticket.Id)
            .Join(_db.Tags, tt => tt.TagId, t => t.Id, (tt, t) => t.Name)
            .ToArrayAsync(cancellationToken);

        //Publish the tag update event to Kafka
        var ticketTagUpdatedEvent = new TicketTagUpdatedEvent(ticket.Id, ticket.Creator.OrganizationId, request.AssignedById, tagNames); 
        await _publisher.PublishAsync("TicketTagUpdated", ticketTagUpdatedEvent, cancellationToken);

        return true;
    }
}