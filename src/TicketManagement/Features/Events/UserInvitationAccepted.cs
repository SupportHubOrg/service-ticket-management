using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessUserInvitationAcceptedCommand(UserInvitationAcceptedPayload Payload) : IRequest;

public class ProcessUserInvitationAcceptedHandler(
    TicketDbContext db,
    ILogger<ProcessUserInvitationAcceptedHandler> logger) : IRequestHandler<ProcessUserInvitationAcceptedCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly ILogger<ProcessUserInvitationAcceptedHandler> _logger = logger;

    public async Task Handle(ProcessUserInvitationAcceptedCommand request, CancellationToken cancellationToken)
    {
        //Get the payload from the command
        UserInvitationAcceptedPayload payload = request.Payload;

        //Check if the user snapshot already exists in the database
        UserSnapshot? existingUserSnapshot = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == payload.UserId, cancellationToken);
        if (existingUserSnapshot != null)
        {
            _logger.LogInformation("User snapshot with ID {UserId} already exists. Skipping creation.", payload.UserId);
            return;
        }

        //Create a new user snapshot entry for the accepted user
        UserSnapshot userSnapshot = new()
        {
            Id = payload.UserId,
            Role = payload.Role,
            OrganizationId = payload.OrganizationId != Guid.Empty ? payload.OrganizationId : null,
        };
        _db.UserSnapshots.Add(userSnapshot);
        await _db.SaveChangesAsync(cancellationToken);
    }
}