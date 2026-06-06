using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.OperatorTags.GetOperatorTags;

public static class GetOperatorTagsEndpoint
{
    /// <summary>
    /// Maps the endpoint for retrieving a collection of operator tags based on provided filters.
    /// </summary>
    public static void MapGetOperatorTagsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tickets/tags/operators", async (
            [AsParameters] GetOperatorTagsRequest parameters,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Construct the query with the provided filters and pagination parameters
            var query = new GetOperatorTagsQuery(
                UserId: userId,
                TagId: parameters.TagId,
                OperatorId: parameters.OperatorId,
                SortBy: parameters.SortBy,
                SortDescending: parameters.SortDescending ?? true,
                PageNumber: parameters.PageNumber ?? 1,
                PageSize: parameters.PageSize ?? 20
            );

            //Send the query to retrieve the list of operator tags
            var result = await mediator.Send(query);

            //Return 200 OK with the list of operator tag summaries and pagination metadata
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}

/// <summary>
/// HTTP request model bound from the query string.
/// </summary>
public record GetOperatorTagsRequest(
    Guid? TagId,
    Guid? OperatorId,
    string? SortBy,
    bool? SortDescending,
    int? PageNumber,
    int? PageSize
);