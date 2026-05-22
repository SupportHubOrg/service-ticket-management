using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tickets.GetTicket;

public class GetTicketHandler(TicketDbContext db) : IRequestHandler<GetTicketQuery, TicketDetailsResponse>
{
    private readonly TicketDbContext _db = db;

    public async Task<TicketDetailsResponse> Handle(GetTicketQuery request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket with AsNoTracking for read-optimization
        Ticket? ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        //Check if the ticket exists and return 404 Not Found if it doesn't
        if (ticket == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {request.TicketId} could not be found.");

        //Retrieve the user requesting the ticket
        UserSnapshot? user = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        //Check if the user exists and return 401 Unauthorized if not
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Enforce Data Visibility Rules
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isSameOrganization = user.OrganizationId == ticket.Creator.OrganizationId;

        if (!isSystemAdmin)
        {
            //Check if the user belongs to the same organization as the ticket creator. If not, return 403 Forbidden.
            if (!isSameOrganization)
                throw new AppException(StatusCodes.Status403Forbidden, "You do not have permission to view this ticket.");

            //Apply role-based visibility rules within the organization
            bool canView = user.Role switch
            {
                Role.AI => true,
                Role.ORGANIZATION_MANAGER => true,
                Role.LEVEL_1_OPERATOR => true,
                Role.LEVEL_2_OPERATOR => true,
                Role.EMPLOYEE => ticket.CreatorId == user.Id,
                _ => false
            };

            //If the user doesn't have permission to view this ticket, return 403 Forbidden
            if (!canView)
            {
                throw new AppException(StatusCodes.Status403Forbidden, "You do not have permission to view this ticket.");
            }
        }

        //Map the Database Entity to the DTO and return it
        return new TicketDetailsResponse(
            ticket.Id,
            ticket.Subject,
            ticket.Description,
            ticket.Status,
            ticket.CreatorId,
            ticket.AssignedOperatorId,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.SlaFirstResponseDeadline,
            ticket.SlaHumanResponseDeadline,
            ticket.SlaResolutionDeadline
        );
    }
}