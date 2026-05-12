using MediatR;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Events;

public record SyncKnowledgeBaseArticleCommand(Guid TicketId, bool IsApproved) : IRequest;

public class SyncKnowledgeBaseArticleHandler(TicketDbContext db) : IRequestHandler<SyncKnowledgeBaseArticleCommand>
{
    private readonly TicketDbContext _db = db;

    public async Task Handle(SyncKnowledgeBaseArticleCommand request, CancellationToken cancellationToken)
    {
        //Fetch the ticket from the database
        var ticket = await _db.Tickets.FindAsync([request.TicketId], cancellationToken);
        if (ticket == null)
            return;

        ticket.Status = request.IsApproved ? Status.IN_KNOWLEDGE_BASE : Status.NOT_IN_KNOWLEDGE_BASE;

        //Save the changes to the database
        await _db.SaveChangesAsync(cancellationToken);
    }
}