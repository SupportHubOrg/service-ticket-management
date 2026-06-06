using System.Security.Claims;
using MediatR;

using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Features.TicketAuditLogs.GetTicketAuditLogs;

public static class GetTicketAuditLogsEndpoint
{
    /// <summary>
    /// Maps the endpoint for retrieving a collection of ticket audit logs based on provided filters.
    /// </summary>
    public static void MapGetTicketAuditLogsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tickets/logs", async (
            [AsParameters] GetTicketAuditLogsRequest parameters,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Construct the query with the provided filters and pagination parameters
            var query = new GetTicketAuditLogsQuery(
                UserId: userId,
                TicketId: parameters.TicketId,
                ChangedById: parameters.ChangedById,
                ActorType: parameters.ActorType,
                Action: parameters.Action,
                OldValue: parameters.OldValue,
                NewValue: parameters.NewValue,
                CreatedAfter: parameters.CreatedAfter,
                CreatedBefore: parameters.CreatedBefore,
                OrganizationId: parameters.OrganizationId,
                SortBy: parameters.SortBy,
                SortDescending: parameters.SortDescending ?? true,
                PageNumber: parameters.PageNumber ?? 1,
                PageSize: parameters.PageSize ?? 20
            );

            //Send the query to retrieve the list of tickets
            var result = await mediator.Send(query);

            //Return 200 OK with the list of ticket summaries and pagination metadata
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}

/// <summary>
/// HTTP request model bound from the query string.
/// </summary>
public record GetTicketAuditLogsRequest(
    Guid? TicketId,
    Guid? ChangedById,
    Role? ActorType,
    string? Action,
    string? OldValue,
    string? NewValue,
    DateTime? CreatedAfter,
    DateTime? CreatedBefore,
    Guid? OrganizationId,
    string? SortBy,
    bool? SortDescending,
    int? PageNumber,
    int? PageSize
);