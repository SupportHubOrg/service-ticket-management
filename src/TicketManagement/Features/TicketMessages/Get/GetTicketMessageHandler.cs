using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportHub.TicketManagement.Domain.Entities;
using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketMessages.GetTicketMessage;

public class GetTicketMessageHandler(TicketDbContext db) : IRequestHandler<GetTicketMessageQuery, TicketMessageDetailsResponse>
{
    private readonly TicketDbContext _db = db;

    public async Task<TicketMessageDetailsResponse> Handle(GetTicketMessageQuery request, CancellationToken cancellationToken)
    {
        //Retrieve the ticket with AsNoTracking for read-optimization
        TicketMessage? ticketMessage = await _db.TicketMessages
            .AsNoTracking()
            .Include(t => t.Sender)
            .FirstOrDefaultAsync(t => t.Id == request.TicketMessageId, cancellationToken);
        //Check if the ticket exists and return 404 Not Found if it doesn't
        if (ticketMessage == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket Message {request.TicketMessageId} could not be found.");

        //Retrieve the ticket and check if the ticket exists, otherwise return 404 Not Found
        var ticket = await _db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketMessage.TicketId, cancellationToken);
        if (ticket == null)
            throw new AppException(StatusCodes.Status404NotFound, $"Ticket {ticketMessage.TicketId} associated with ticket message {request.TicketMessageId} could not be found.");

        //Retrieve the user requesting the ticket and check if they exist, otherwise return 401 Unauthorized
        UserSnapshot? user = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null)
            throw new AppException(StatusCodes.Status401Unauthorized, $"User {request.UserId} could not be found.");

        //Enforce Data Visibility Rules
        bool isSystemAdmin = user.Role == Role.SUPPORT_HUB_ADMINISTRATOR || user.Role == Role.SYSTEM;
        bool isSameOrganization = user.OrganizationId == ticketMessage.Sender.OrganizationId;
        if (!isSystemAdmin)
        {
            //Check if the user belongs to the same organization as the ticket creator. If not, return 403 Forbidden.
            if (!isSameOrganization)
                throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} does not have permission to view ticket message {request.TicketMessageId}.");

            //Apply role-based visibility rules within the organization
            bool canView = false;
            
            switch (user.Role)
            {
                case Role.EMPLOYEE:
                    if (ticket.CreatorId == user.Id)
                        canView = true;
                    break;
                case Role.LEVEL_1_OPERATOR:
                case Role.LEVEL_2_OPERATOR:
                    if (ticket.AssignedOperatorId == user.Id || ticket.CreatorId == user.Id)
                        canView = true;
                    break;
                case Role.ORGANIZATION_MANAGER:
                case Role.AI:
                    break;
                default:
                    throw new AppException(StatusCodes.Status403Forbidden, "Unrecognized role attempting to query tickets.");
            }

            //If the user doesn't have permission to view this ticket, return 403 Forbidden
            if (!canView)
            {
                throw new AppException(StatusCodes.Status403Forbidden, $"User {request.UserId} does not have permission to view ticket message {request.TicketMessageId}.");
            }
        }

        //Map the Database Entity to the DTO and return it
        return new TicketMessageDetailsResponse(
            Id: ticketMessage.Id,
            TicketId: ticketMessage.TicketId,
            SenderId: ticketMessage.SenderId,
            SenderType: ticketMessage.SenderType,
            Message: ticketMessage.Message,
            SentAt: ticketMessage.SentAt
        );
    }
}