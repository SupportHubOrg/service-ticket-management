using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessUserRemovedFromOrganizationCommand(UserRemovedFromOrganizationPayload Payload) : IRequest;

public class ProcessUserRemovedFromOrganizationHandler(
    TicketDbContext db,
    IEventPublisher publisher,
    ILogger<ProcessUserRemovedFromOrganizationHandler> logger) : IRequestHandler<ProcessUserRemovedFromOrganizationCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;
    private readonly ILogger<ProcessUserRemovedFromOrganizationHandler> _logger = logger;

    public async Task Handle(ProcessUserRemovedFromOrganizationCommand request, CancellationToken cancellationToken)
    {
        //Get the payload from the command
        UserRemovedFromOrganizationPayload payload = request.Payload;

        //Fetch the user snapshot from the database
        var userBeingRemoved = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == payload.UserId && u.OrganizationId == payload.OrganizationId, cancellationToken);
        if (userBeingRemoved == null)
        {
            _logger.LogWarning("User snapshot with ID {UserId} in organization {OrganizationId} not found for removal processing.", 
                payload.UserId, payload.OrganizationId);
            return;
        }

        //Fetch the AI user snapshot to use as the new creator for orphaned tickets
        UserSnapshot? aiUser = await _db.UserSnapshots
            .Where(u => u.Role == Role.AI && u.OrganizationId == payload.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (aiUser == null)
        {
            _logger.LogError("No AI user found in organization {OrganizationId} for user removal processing.", payload.OrganizationId);
            return;
        }

        //Find all tickets created by the removed user in the organization
        var createdTickets = await _db.Tickets
            .Where(t => t.CreatorId == payload.UserId)
            .Include(t => t.Creator)
                .ThenInclude(c => c.Organization)
            .ToListAsync(cancellationToken);

        //Reassign created tickets to the AI user
        foreach (var ticket in createdTickets)
        {
            var previousCreatorId = ticket.CreatorId;
            ticket.CreatorId = aiUser.Id;

            //Add an audit log entry for each ticket creator reassignment
            TicketAuditLog auditLog = new()
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                ChangedById = null,
                ActorType = Role.SYSTEM,
                Action = "User Removed From Organization - Creator Reassigned",
                OldValue = $"CreatorId: {previousCreatorId}",
                NewValue = $"CreatorId: {aiUser.Id}",
                Message = $"Ticket creator changed to AI user because original creator {payload.UserId} was removed from the organization.",
                CreatedAt = DateTime.UtcNow
            };
            _db.TicketAuditLogs.Add(auditLog);
        }

        //Find all tickets assigned to the removed user in the organization
        var assignedTickets = await _db.Tickets
            .Where(t => t.AssignedOperatorId == payload.UserId)
            .Include(t => t.Creator)
                .ThenInclude(c => c.Organization)
            .ToListAsync(cancellationToken);

        //Unassign all tickets from the removed user
        foreach (var ticket in assignedTickets)
        {
            ticket.AssignedOperatorId = null;
            if (ticket.Status == Status.IN_PROGRESS_LEVEL_1 || ticket.Status == Status.IN_PROGRESS_LEVEL_2)
                ticket.Status = Status.UNASSIGNED;

            //Add an audit log entry for each ticket reassignment
            TicketAuditLog auditLog = new()
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                ChangedById = null,
                ActorType = Role.SYSTEM,
                Action = "User Removed From Organization - Ticket Unassigned",
                OldValue = $"AssignedOperatorId: {payload.UserId}",
                NewValue = "AssignedOperatorId: null, Status: UNASSIGNED",
                Message = $"Ticket automatically unassigned because operator {payload.UserId} was removed from the organization.",
                CreatedAt = DateTime.UtcNow
            };
            _db.TicketAuditLogs.Add(auditLog);

            if (ticket.Status == Status.IN_PROGRESS_LEVEL_1 || ticket.Status == Status.IN_PROGRESS_LEVEL_2)
            {
                //Publish an event for each unassigned ticket
                var ticketStatusChangedEvent = new TicketStatusChangedEvent(
                    ticket.Id, 
                    ticket.Creator.OrganizationId, 
                    ticket.Status, 
                    Status.UNASSIGNED, 
                    aiUser.Id, 
                    Role.SYSTEM
                );
                await _publisher.PublishAsync("TicketStatusChanged", ticketStatusChangedEvent, cancellationToken);
            }
        }

        //Remove the user snapshot from the database
        _db.UserSnapshots.Remove(userBeingRemoved);

        //Save all changes to the database
        await _db.SaveChangesAsync(cancellationToken);
    }
}