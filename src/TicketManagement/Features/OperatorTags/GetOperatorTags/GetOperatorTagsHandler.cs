using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.OperatorTags.GetOperatorTags;

public class GetOperatorTagsHandler(TicketDbContext db) : IRequestHandler<GetOperatorTagsQuery, PagedResult<OperatorTagSummaryResponse>>
{
    private readonly TicketDbContext _db = db;

    public async Task<PagedResult<OperatorTagSummaryResponse>> Handle(GetOperatorTagsQuery request, CancellationToken cancellationToken)
    {
        //Retrieve the requesting user and check if they exist, if not return 401 Unauthorized
        var requestingUser = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (requestingUser == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Initialize the base query
        var query = _db.OperatorTags
            .Include(ot => ot.Operator)
            .AsNoTracking()
            .AsQueryable();

        //Enforce Data Visibility Rules
        bool isSystemAdmin = requestingUser.Role == Role.SUPPORT_HUB_ADMINISTRATOR || requestingUser.Role == Role.SYSTEM;
        if (!isSystemAdmin)
            query = query.Where(tt => tt.Operator.OrganizationId == requestingUser.OrganizationId);

        //Apply User-Requested Filters
        if (request.TagId.HasValue)
            query = query.Where(t => t.TagId == request.TagId.Value);
        if (request.OperatorId.HasValue)
            query = query.Where(t => t.OperatorId == request.OperatorId.Value);

        //Apply Sorting
        query = query.ApplySort(request.SortBy, request.SortDescending);

        //Count total items before pagination
        int totalCount = await query.CountAsync(cancellationToken);

        //Get the paginated list of operator tags and project to OperatorTagSummaryResponse
        var operatorTags = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(tt => tt.Tag)
            .Select(t => new OperatorTagSummaryResponse(
                t.TagId,
                t.OperatorId,
                t.Tag.Name,
                t.Tag.Description,
                t.Tag.OrganizationId,
                t.Tag.ExcludedFromAi
            ))
            .ToListAsync(cancellationToken);

        //Return the paginated list of ticket summaries along with pagination metadata
        return new PagedResult<OperatorTagSummaryResponse>(
            operatorTags,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}