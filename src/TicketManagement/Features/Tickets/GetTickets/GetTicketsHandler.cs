using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tickets.GetTickets;

public class GetTicketsHandler(TicketDbContext db) : IRequestHandler<GetTicketsQuery, PagedResult<TicketSummaryResponse>>
{
    private readonly TicketDbContext _db = db;

    public async Task<PagedResult<TicketSummaryResponse>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        //Retrieve the requesting user and check if they exist, otherwise return 401 Unauthorized
        var requestingUser = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (requestingUser == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Initialize the base query
        var query = _db.Tickets.AsNoTracking().AsQueryable();

        //Enforce Data Visibility Rules
        bool isSystemAdmin = requestingUser.Role == Role.SUPPORT_HUB_ADMINISTRATOR || requestingUser.Role == Role.SYSTEM;
        if (!isSystemAdmin)
        {
            //Lock all non-system admins to their own organization
            query = query.Where(t => t.Creator.OrganizationId == requestingUser.OrganizationId);

            //Apply additional visibility rules based on the user's role within the organization
            switch (requestingUser.Role)
            {
                case Role.EMPLOYEE:
                    query = query.Where(t => t.CreatorId == requestingUser.Id);
                    break;
                case Role.LEVEL_1_OPERATOR:
                case Role.LEVEL_2_OPERATOR:
                case Role.ORGANIZATION_MANAGER:
                case Role.AI:
                    break;
                default:
                    throw new AppException(StatusCodes.Status403Forbidden, "Unrecognized role attempting to query tickets.");
            }
        }
        else if (request.OrganizationId.HasValue)
        {
            //System admins can optionally filter by organization
            query = query.Where(t => t.Creator.OrganizationId == request.OrganizationId.Value);
        }

        //Apply User-Requested Filters
        if (request.CreatorId.HasValue)
            query = query.Where(t => t.CreatorId == request.CreatorId.Value);

        if (request.AssignedOperatorId.HasValue)
            query = query.Where(t => t.AssignedOperatorId == request.AssignedOperatorId.Value);

        if (request.CreatedAfter.HasValue)
            query = query.Where(t => t.CreatedAt >= request.CreatedAfter.Value);

        if (request.CreatedBefore.HasValue)
            query = query.Where(t => t.CreatedAt <= request.CreatedBefore.Value);

        //Apply Sorting
        query = query.ApplySort(request.SortBy, request.SortDescending);

        //Apply Pagination
        int totalCount = await query.CountAsync(cancellationToken);

        var tickets = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TicketSummaryResponse(
                t.Id,
                t.Subject,
                t.Description,
                t.Status,
                t.CreatorId,
                t.AssignedOperatorId,
                t.CreatedAt,
                t.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        //Return the paginated list of ticket summaries along with pagination metadata
        return new PagedResult<TicketSummaryResponse>(
            tickets,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}