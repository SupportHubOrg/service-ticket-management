using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketTags.GetTicketTags;

public class GetTicketTagsHandler(TicketDbContext db) : IRequestHandler<GetTicketTagsQuery, PagedResult<TicketTagSummaryResponse>>
{
    private readonly TicketDbContext _db = db;

    public async Task<PagedResult<TicketTagSummaryResponse>> Handle(GetTicketTagsQuery request, CancellationToken cancellationToken)
    {
        //Retrieve the requesting user and check if they exist, if not return 401 Unauthorized
        var requestingUser = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (requestingUser == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Initialize the base query
        var query = _db.TicketTags.Include(tt => tt.Ticket).ThenInclude(t => t.Creator).AsNoTracking().AsQueryable();

        //Enforce Data Visibility Rules
        bool isSystemAdmin = requestingUser.Role == Role.SUPPORT_HUB_ADMINISTRATOR || requestingUser.Role == Role.SYSTEM;
        if (!isSystemAdmin)
        {
            //Lock all non-system admins to their own organization
            query = query.Where(tt => tt.Ticket.Creator.OrganizationId == requestingUser.OrganizationId);

            //Apply additional visibility rules based on the user's role within the organization
            switch (requestingUser.Role)
            {
                case Role.EMPLOYEE:
                    query = query.Where(tt => tt.Ticket.CreatorId == requestingUser.Id);
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

        //Apply User-Requested Filters
        if (request.TagId.HasValue)
            query = query.Where(t => t.TagId == request.TagId.Value);
        if (request.AssignedById.HasValue)
            query = query.Where(t => t.AssignedById == request.AssignedById.Value);
        if (request.TicketId.HasValue)
            query = query.Where(t => t.TicketId == request.TicketId.Value);

        //Apply Sorting
        query = query.ApplySort(request.SortBy, request.SortDescending);

        //Count total items before pagination
        int totalCount = await query.CountAsync(cancellationToken);

        //Get the paginated list of ticket tags and project to TicketTagSummaryResponse
        var ticketTags = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(tt => tt.Tag)
            .Select(t => new TicketTagSummaryResponse(
                t.TicketId,
                t.TagId,
                t.AssignedById,
                t.Tag.Name,
                t.Tag.Description,
                t.Tag.OrganizationId,
                t.Tag.ExcludedFromAi
            ))
            .ToListAsync(cancellationToken);

        //Return the paginated list of ticket summaries along with pagination metadata
        return new PagedResult<TicketTagSummaryResponse>(
            ticketTags,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}