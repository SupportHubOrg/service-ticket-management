using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketMessages.CreateTicketMessage;

public class CreateTicketMessageHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<CreateTicketMessageCommand, Guid>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<Guid> Handle(CreateTicketMessageCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the user and chceck if they exist, if not return 401 Unauthorized
        UserSnapshot? user = await _db.UserSnapshots
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, $"User {request.UserId} could not be found.");

        //Check if the ticket exists, if not return 404 Not Found
        Ticket? ticket = await _db.Tickets.Include(t => t.Creator).FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        if (ticket == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");
        
        //Apply authorization rules for creating ticket messages
        bool isTicketCreator = ticket.CreatorId == request.UserId;
        bool isAssignedOperator = ticket.AssignedOperatorId.HasValue && ticket.AssignedOperatorId.Value == request.UserId;
        bool isOrganizationManager = (user.Role == Role.ORGANIZATION_MANAGER || user.Role == Role.AI) && user.OrganizationId == ticket.Creator.OrganizationId;
        bool isSystemAdministrator = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        if (!isTicketCreator && !isAssignedOperator && !isOrganizationManager && !isSystemAdministrator)
            throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} does not have permission to send messages on ticket {request.TicketId}. Only the ticket creator, assigned operators, organization managers, or SupportHub administrators can send messages on a ticket.");

        //Create the new ticket message
        var ticketMessage = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            SenderId = request.UserId,
            SenderType = user.Role,
            Message = request.Message,
            SentAt = DateTime.UtcNow
        };

        //Save the ticket to the database
        _db.TicketMessages.Add(ticketMessage);
        await _db.SaveChangesAsync(cancellationToken);

        //Publish the creation event to Kafka
        var ticketMessageAddedEvent = new TicketMessageAddedEvent(
            request.TicketId,
            user.OrganizationId,
            ticketMessage.Id,
            ticketMessage.SenderId,
            ticketMessage.SenderType, 
            ticketMessage.Message
        );
        await _publisher.PublishAsync("TicketMessageAdded", ticketMessageAddedEvent, cancellationToken);

        //Return the assigned ID
        return ticketMessage.Id;
    }
}