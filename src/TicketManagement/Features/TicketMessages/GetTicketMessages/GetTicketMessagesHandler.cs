using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketMessages.GetTicketMessages;

public class GetTicketMessagesHandler(TicketDbContext db) : IRequestHandler<GetTicketMessagesQuery, PagedResult<TicketMessageSummaryResponse>>
{
    private readonly TicketDbContext _db = db;

    public async Task<PagedResult<TicketMessageSummaryResponse>> Handle(GetTicketMessagesQuery request, CancellationToken cancellationToken)
    {
        //Retrieve the requesting user and check if they exist, otherwise return 401 Unauthorized
        var requestingUser = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (requestingUser == null)
            throw new AppException(StatusCodes.Status401Unauthorized, $"User {request.UserId} could not be found.");

        //Initialize the base query
        var query = _db.TicketMessages
            .Include(t => t.Sender)
            .Include(t => t.Ticket)
            .AsNoTracking()
            .AsQueryable();

        //Enforce Data Visibility Rules
        bool isSystemAdmin = requestingUser.Role == Role.SUPPORT_HUB_ADMINISTRATOR || requestingUser.Role == Role.SYSTEM;
        if (!isSystemAdmin)
        {
            //Lock all non-system admins to their own organization
            query = query.Where(t => t.Sender.OrganizationId == requestingUser.OrganizationId);

            //Apply additional visibility rules based on the user's role within the organization
            switch (requestingUser.Role)
            {
                case Role.EMPLOYEE:
                    query = query.Where(t => t.Ticket.CreatorId == requestingUser.Id);
                    break;
                case Role.LEVEL_1_OPERATOR:
                case Role.LEVEL_2_OPERATOR:
                    query = query.Where(t => t.Ticket.CreatorId == requestingUser.Id || t.Ticket.AssignedOperatorId == requestingUser.Id);
                    break;
                case Role.ORGANIZATION_MANAGER:
                case Role.AI:
                    break;
                default:
                    throw new AppException(StatusCodes.Status403Forbidden, "Unrecognized role attempting to query tickets.");
            }
        }

        //Apply User-Requested Filters
        if (request.TicketId.HasValue)
            query = query.Where(t => t.TicketId == request.TicketId.Value); 

        if (request.SenderId.HasValue)
            query = query.Where(t => t.SenderId == request.SenderId.Value);

        if (request.SenderType.HasValue)
            query = query.Where(t => t.SenderType == request.SenderType.Value);

        if (request.SentAfter.HasValue)
            query = query.Where(t => t.SentAt >= request.SentAfter.Value);
        
        if (request.SentBefore.HasValue)
            query = query.Where(t => t.SentAt <= request.SentBefore.Value);

        //Apply Sorting
        query = query.ApplySort(request.SortBy, request.SortDescending);

        //Apply Pagination
        int totalCount = await query.CountAsync(cancellationToken);

        var tickets = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TicketMessageSummaryResponse(
                t.Id,
                t.TicketId,
                t.SenderId,
                t.SenderType,
                t.Message,
                t.SentAt
            ))
            .ToListAsync(cancellationToken);

        //Return the paginated list of ticket summaries along with pagination metadata
        return new PagedResult<TicketMessageSummaryResponse>(
            tickets,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}