using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Events;

public record ProcessAiAnalysisCompletedCommand(Guid TicketId) : IRequest;

public class ProcessAiAnalysisCompletedHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<ProcessAiAnalysisCompletedCommand>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task Handle(ProcessAiAnalysisCompletedCommand request, CancellationToken cancellationToken)
    {
        //Fetch the ticket from the database
        var ticket = await _db.Tickets
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        if (ticket == null)
            return;

        //Update the ticket status to Unassigned and clear the assigned operator
        ticket.Status = Status.UNASSIGNED;
        ticket.AssignedOperatorId = null;

        //Save the changes to the database
        await _db.SaveChangesAsync(cancellationToken);

        //Publish an event to notify that the ticket has been escalated
        await _publisher.PublishAsync("TicketEscalated",
            new TicketEscalatedEvent(ticket.Id, ticket.Creator.OrganizationId, ticket.AssignedOperatorId ?? Guid.Empty, ticket.Status),
            cancellationToken);
}
}