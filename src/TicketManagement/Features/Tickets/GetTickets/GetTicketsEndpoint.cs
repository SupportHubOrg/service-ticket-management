using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.GetTickets;

public static class GetTicketsEndpoint
{
    /// <summary>
    /// Maps the endpoint for retrieving a collection of tickets based on provided filters.
    /// </summary>
    public static void MapGetTicketsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tickets", async (
            [AsParameters] GetTicketsRequest parameters,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Construct the query with the provided filters and pagination parameters
            var query = new GetTicketsQuery(
                UserId: userId,
                OrganizationId: parameters.OrganizationId,
                CreatorId: parameters.CreatorId,
                AssignedOperatorId: parameters.AssignedOperatorId,
                CreatedAfter: parameters.CreatedAfter,
                CreatedBefore: parameters.CreatedBefore,
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
public record GetTicketsRequest(
    Guid? OrganizationId,
    Guid? CreatorId,
    Guid? AssignedOperatorId,
    DateTime? CreatedAfter,
    DateTime? CreatedBefore,
    string? SortBy,
    bool? SortDescending,
    int? PageNumber,
    int? PageSize
);