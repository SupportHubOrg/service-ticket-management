using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

namespace SupportHub.TicketManagement.Features.Tickets.ProposeArticle;

public class ProposeArticleHandler(TicketDbContext db, IEventPublisher publisher) : IRequestHandler<ProposeArticleCommand, bool>
{
    private readonly TicketDbContext _db = db;
    private readonly IEventPublisher _publisher = publisher;

    public async Task<bool> Handle(ProposeArticleCommand request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket with AsNoTracking
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        //Check if the ticket exists and return 404 Not Found if not
        if (ticket == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");
        
        //Retrieve the user to verify permissions
        var requestingUser = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        //Check if the user exists and return 401 Unauthorized if not
        if (requestingUser == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Enforce Authorization Rules
        if (requestingUser.Role == Role.EMPLOYEE)
            throw new AppException(StatusCodes.Status403Forbidden, "Employees are not permitted to propose knowledge base articles.");

        //Users can only propose articles for tickets within their own organization or as a system administrator
        bool isSystemAdmin = requestingUser.Role == Role.SUPPORT_HUB_ADMINISTRATOR || requestingUser.Role == Role.SYSTEM;
        bool isSameOrganization = requestingUser.OrganizationId == ticket.Creator.OrganizationId;
        if (!isSystemAdmin && !isSameOrganization)
            throw new AppException(StatusCodes.Status403Forbidden, "Users can only propose knowledge base articles for tickets within their own organization.");

        //Publish the proposed article event to Kafka
        Guid operatorId = ticket.AssignedOperatorId ?? Guid.Empty;
        var proposedEvent = new KnowledgeArticleProposedEvent(
            ticket.Id,
            operatorId,
            request.UserId,
            request.ProposedSubject,
            request.Tags
        );
        await _publisher.PublishAsync("KnowledgeArticleProposed", proposedEvent, cancellationToken);

        return true;
    }
}