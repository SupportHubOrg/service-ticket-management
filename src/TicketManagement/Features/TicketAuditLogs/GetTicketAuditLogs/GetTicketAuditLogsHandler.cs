using MediatR;
using Microsoft.EntityFrameworkCore;

using SupportHub.TicketManagement.Domain.Enums;
using SupportHub.TicketManagement.Domain.Exceptions;
using SupportHub.TicketManagement.Infrastructure.Data;

namespace SupportHub.TicketManagement.Features.TicketAuditLogs.GetTicketAuditLogs;

public class GetTicketAuditLogsHandler(TicketDbContext db) : IRequestHandler<GetTicketAuditLogsQuery, PagedResult<TicketAuditLogSummaryResponse>>
{
    private readonly TicketDbContext _db = db;

    public async Task<PagedResult<TicketAuditLogSummaryResponse>> Handle(GetTicketAuditLogsQuery request, CancellationToken cancellationToken)
    {
        //Retrieve the requesting user and check if they exist, otherwise return 401 Unauthorized
        var requestingUser = await _db.UserSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (requestingUser == null)
            throw new AppException(StatusCodes.Status401Unauthorized, "User could not be found.");

        //Initialize the base query
        var query = _db.TicketAuditLogs
            .Include(t => t.Ticket)
                .ThenInclude(t => t.Creator)
            .AsNoTracking()
            .AsQueryable();

        //Enforce Data Visibility Rules
        bool isSystemAdmin = requestingUser.Role == Role.SUPPORT_HUB_ADMINISTRATOR || requestingUser.Role == Role.SYSTEM;
        if (!isSystemAdmin)
        {

            //Only organanization managers can see audit logs
            if (requestingUser.Role != Role.ORGANIZATION_MANAGER)
                throw new AppException(StatusCodes.Status403Forbidden, "Only organization managers and system admins can view ticket audit logs.");

            //Lock all non-system admins to their own organization
            query = query.Where(t => t.Ticket.Creator.OrganizationId == requestingUser.OrganizationId);
        }
        else if (request.OrganizationId.HasValue)
        {
            //System admins can optionally filter by organization
            query = query.Where(t => t.Ticket.Creator.OrganizationId == request.OrganizationId.Value);
        }

        //Apply User-Requested Filters
        if (request.ChangedById.HasValue)
            query = query.Where(t => t.ChangedById == request.ChangedById.Value);

        if (request.TicketId.HasValue)
            query = query.Where(t => t.TicketId == request.TicketId.Value);

        if (request.ActorType.HasValue)
            query = query.Where(t => t.ActorType == request.ActorType.Value);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(t => t.Action.Contains(request.Action.Trim()));

        if (!string.IsNullOrWhiteSpace(request.OldValue))
            query = query.Where(t => t.OldValue != null && t.OldValue.Contains(request.OldValue.Trim()));

        if (!string.IsNullOrWhiteSpace(request.NewValue))
            query = query.Where(t => t.NewValue != null && t.NewValue.Contains(request.NewValue.Trim()));

        if (request.CreatedAfter.HasValue)
            query = query.Where(t => t.CreatedAt >= request.CreatedAfter.Value);

        if (request.CreatedBefore.HasValue)
            query = query.Where(t => t.CreatedAt <= request.CreatedBefore.Value);

        //Apply Sorting
        query = query.ApplySort(request.SortBy, request.SortDescending);

        //Apply Pagination
        int totalCount = await query.CountAsync(cancellationToken);

        var ticketAuditLogs = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TicketAuditLogSummaryResponse(
                t.Id,
                t.TicketId,
                t.ChangedById,
                t.ActorType,
                t.Action,
                t.OldValue,
                t.NewValue,
                t.Message,
                t.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        //Return the paginated list of ticket summaries along with pagination metadata
        return new PagedResult<TicketAuditLogSummaryResponse>(
            ticketAuditLogs,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}