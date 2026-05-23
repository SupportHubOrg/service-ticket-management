using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessUserRoleUpdatedCommand(UserRoleUpdatedPayload Payload) : IRequest;

public class ProcessUserRoleUpdatedHandler(
    TicketDbContext db,
    ILogger<ProcessUserRoleUpdatedHandler> logger) : IRequestHandler<ProcessUserRoleUpdatedCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly ILogger<ProcessUserRoleUpdatedHandler> _logger = logger;

    public async Task Handle(ProcessUserRoleUpdatedCommand request, CancellationToken cancellationToken)
    {
        //Get the payload from the command
        UserRoleUpdatedPayload payload = request.Payload;

        //Fetch the user snapshot from the database
        UserSnapshot? user = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == payload.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User snapshot with ID {UserId} not found. Cannot update role.", payload.UserId);
            return;
        }

        //Update the user's role
        user.Role = payload.NewRole;
        await _db.SaveChangesAsync(cancellationToken);
    }
}