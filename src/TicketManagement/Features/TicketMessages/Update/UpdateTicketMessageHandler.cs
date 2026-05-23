using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketMessages.UpdateTicketMessage;

public class UpdateTicketHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<UpdateTicketMessageCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(UpdateTicketMessageCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket message to be updated and check if it exists, otherwise return 404 Not Found
        TicketMessage? ticketMessage = await _db.TicketMessages
            .Include(t => t.Sender)
            .Include(t => t.Ticket)
                .ThenInclude(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == request.TicketMessageId, cancellationToken);
        if (ticketMessage == null)
            throw new AppException(StatusCodes.Status404NotFound, $"TicketMessage {request.TicketMessageId} could not be found.");

        //Check if the user exists and return 401 Unauthorized if not
        var user = await _db.UserSnapshots.Where(u => u.Id == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Enforce strict authorization rules
        bool isSender = ticketMessage.SenderId == request.UserId;
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = user.Role == Role.ORGANIZATION_MANAGER && user.OrganizationId == ticketMessage.Ticket.Creator.OrganizationId;
        if (!isSystemAdmin && !isOrganizationManager && !isSender)
            throw new AppException(StatusCodes.Status403Forbidden, "User does not have permission to update this ticket.");

        //Apply updates to the ticket
        ticketMessage.Message = request.Message;
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}