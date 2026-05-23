using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessOrganizationDeletedCommand(OrganizationDeletedPayload Payload) : IRequest;

public class ProcessOrganizationDeletedHandler(
    TicketDbContext db,
    ILogger<ProcessOrganizationDeletedHandler> logger) : IRequestHandler<ProcessOrganizationDeletedCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly ILogger<ProcessOrganizationDeletedHandler> _logger = logger;

    public async Task Handle(ProcessOrganizationDeletedCommand request, CancellationToken cancellationToken)
    {
        //Get the payload from the command
        OrganizationDeletedPayload payload = request.Payload;

        //Fetch the organization snapshot from the database
        var organizationSnapshot = await _db.OrganizationSnapshots
            .FirstOrDefaultAsync(o => o.Id == payload.OrganizationId, cancellationToken);
        if (organizationSnapshot == null)
        {
            _logger.LogWarning("Organization snapshot with ID {OrganizationId} not found for deletion processing.", payload.OrganizationId);
            return;
        }

        //Find all tickets for the organization and delete them
        var tickets = await _db.Tickets
            .Where(t => t.Creator.OrganizationId == payload.OrganizationId)
            .ToListAsync(cancellationToken);
        _db.Tickets.RemoveRange(tickets);

        //Find all tags for the organization and delete them
        var tags = await _db.Tags
            .Where(t => t.OrganizationId == payload.OrganizationId)
            .ToListAsync(cancellationToken);
        _db.Tags.RemoveRange(tags);

        //Find all user snapshots for the organization and delete them
        var users = await _db.UserSnapshots
            .Where(u => u.OrganizationId == payload.OrganizationId)
            .ToListAsync(cancellationToken);
        _db.UserSnapshots.RemoveRange(users);

        //Remove the organization snapshot
        _db.OrganizationSnapshots.Remove(organizationSnapshot);

        //Save all changes to the database
        await _db.SaveChangesAsync(cancellationToken);
    }
}