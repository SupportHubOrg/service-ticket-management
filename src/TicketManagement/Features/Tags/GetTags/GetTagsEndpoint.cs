using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.GetTags;

public static class GetTagsEndpoint
{
    /// <summary>
    /// Maps the endpoint for retrieving a collection of Tags based on provided filters.
    /// </summary>
    public static void MapGetTagsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/Tags", async (
            [AsParameters] GetTagsRequest parameters,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Construct the query with the provided filters and pagination parameters
            var query = new GetTagsQuery(
                UserId: userId,
                OrganizationId: parameters.OrganizationId,
                ExcludedFromAi: parameters.ExcludedFromAi,
                SortBy: parameters.SortBy,
                SortDescending: parameters.SortDescending ?? true,
                PageNumber: parameters.PageNumber ?? 1,
                PageSize: parameters.PageSize ?? 20
            );

            //Send the query to retrieve the list of Tags
            var result = await mediator.Send(query);

            //Return 200 OK with the list of Tag summaries and pagination metadata
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}

/// <summary>
/// HTTP request model bound from the query string.
/// </summary>
public record GetTagsRequest(
    Guid? OrganizationId,
    bool? ExcludedFromAi,
    string? SortBy,
    bool? SortDescending,
    int? PageNumber,
    int? PageSize
);