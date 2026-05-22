using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.Tags.GetTags;

public class GetTagsHandler(TicketDbContext db) : IRequestHandler<GetTagsQuery, PagedResult<TagSummaryResponse>>
{
    private readonly TicketDbContext _db = db;

    public async Task<PagedResult<TagSummaryResponse>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        //Retrieve the requesting user and check if they exist, otherwise return 401 Unauthorized
        var requestingUser = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (requestingUser == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Initialize the base query
        var query = _db.Tags.AsNoTracking().AsQueryable();

        //Enforce Data Visibility Rules
        bool isSystemAdmin = requestingUser.Role == Role.SUPPORT_HUB_ADMINISTRATOR || requestingUser.Role == Role.SYSTEM;
        if (!isSystemAdmin)
        {
            //Lock all non-system admins to their own organization
            query = query.Where(t => t.OrganizationId == requestingUser.OrganizationId);
        }
        else if (request.OrganizationId.HasValue)
        {
            //System admins can optionally filter by organization
            query = query.Where(t => t.OrganizationId == request.OrganizationId.Value);
        }

        //Apply User-Requested Filters
        if (request.ExcludedFromAi.HasValue)
            query = query.Where(t => t.ExcludedFromAi == request.ExcludedFromAi.Value);

        //Apply Sorting
        query = query.ApplySort(request.SortBy, request.SortDescending);

        //Apply Pagination
        int totalCount = await query.CountAsync(cancellationToken);

        var Tags = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TagSummaryResponse(
                t.Id,
                t.OrganizationId,
                t.Name,
                t.Description,
                t.ExcludedFromAi
            ))
            .ToListAsync(cancellationToken);

        //Return the paginated list of Tag summaries along with pagination metadata
        return new PagedResult<TagSummaryResponse>(
            Tags,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}