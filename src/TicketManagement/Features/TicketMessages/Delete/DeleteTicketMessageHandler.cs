using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.TicketMessages.DeleteTicketMessage;

public class DeleteTicketMessageHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<DeleteTicketMessageCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(DeleteTicketMessageCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket message to be deleted and check if it exists, otherwise return 404 Not Found
        TicketMessage? ticketMessage = await _db.TicketMessages
            .Include(t => t.Sender)
            .FirstOrDefaultAsync(t => t.Id == request.TicketMessageId, cancellationToken);
        if (ticketMessage == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket message {request.TicketMessageId} could not be found.");

        //Retrieve the user to verify permissions and check if they exist, otherwise return 401 Unauthorized
        UserSnapshot? user = await _db.UserSnapshots
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, $"User {request.UserId} could not be found.");

        //Enforce strict authorization rules for deletion
        bool isSender = ticketMessage.SenderId == request.UserId;
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isOrganizationManager = user.Role == Role.ORGANIZATION_MANAGER && user.OrganizationId == ticketMessage.Sender.OrganizationId;
        if (!isSystemAdmin && !isOrganizationManager && !isSender)
            throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} attempted to delete ticket message {request.TicketMessageId} without sufficient permissions. Only Organization Managers or SupportHub Administrators can delete tickets.");

        //Remove the ticket message from the database
        _db.TicketMessages.Remove(ticketMessage);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}