using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Tickets.EvaluateAiResponse;

public class EvaluateAiResponseHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<EvaluateAiResponseCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(EvaluateAiResponseCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket and check if it exists
        var ticket = await _db.Tickets
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        if (ticket == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");
        
        //Retrieve the user to verify permissions with AsNoTracking
        var evaluatingUser = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.EvaluatorId, cancellationToken);
        //Check if the user exists and return 401 Unauthorized if not
        if (evaluatingUser == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Enforce Authorization Rules where only the ticket creator or administration can evaluate the AI response
        bool isSystemAdmin = evaluatingUser.Role == Role.SUPPORT_HUB_ADMINISTRATOR || evaluatingUser.Role == Role.SYSTEM;
        bool isTicketCreator = ticket.CreatorId == request.EvaluatorId;
        if (!isSystemAdmin && !isTicketCreator)
            throw new AppException(StatusCodes.Status403Forbidden, "Only the ticket creator can evaluate the AI response.");

        //If the response was not accepted, send an escalation event to Kafka
        if (!request.IsHelpful)
        {
            //Change ticket status to UNASSIGNED
            ticket.Status = Status.UNASSIGNED;
            _db.Tickets.Update(ticket);
            await _db.SaveChangesAsync(cancellationToken);

            //Publish a Ticket Escalated event to Kafka
            var ticketEscalatedEvent = new TicketEscalatedEvent(
                ticket.Id,
                evaluatingUser.OrganizationId,
                request.EvaluatorId,
                Status.UNASSIGNED
            );
            await _publisher.PublishAsync("TicketEscalated", ticketEscalatedEvent, cancellationToken);
        }
        //Otherwise, if the response was accepted, mark the ticket as CLOSED
        else
        {
            //Change ticket status to CLOSED
            ticket.Status = Status.CLOSED;
            _db.Tickets.Update(ticket);
            await _db.SaveChangesAsync(cancellationToken);

            //Publish a Ticket Status Changed event to Kafka
            var ticketStatusChangedEvent = new TicketStatusChangedEvent(
                ticket.Id,
                evaluatingUser.OrganizationId,
                Status.NEW,
                Status.CLOSED,
                request.EvaluatorId,
                evaluatingUser.Role
            );
            await _publisher.PublishAsync("TicketStatusChanged", ticketStatusChangedEvent, cancellationToken);
        }

        return true;
    }
}