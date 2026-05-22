using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketTags.GetTicketTags;

public static class GetTicketTagsEndpoint
{
    /// <summary>
    /// Maps the endpoint for retrieving a collection of ticket tags based on provided filters.
    /// </summary>
    public static void MapGetTicketTagsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ticket-tags", async (
            [AsParameters] GetTicketTagsRequest parameters,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Construct the query with the provided filters and pagination parameters
            var query = new GetTicketTagsQuery(
                UserId: userId,
                TagId: parameters.TagId,
                TicketId: parameters.TicketId,
                AssignedById: parameters.AssignedById,
                SortBy: parameters.SortBy,
                SortDescending: parameters.SortDescending ?? true,
                PageNumber: parameters.PageNumber ?? 1,
                PageSize: parameters.PageSize ?? 20
            );

            //Send the query to retrieve the list of ticket tags
            var result = await mediator.Send(query);

            //Return 200 OK with the list of ticket tag summaries and pagination metadata
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}

/// <summary>
/// HTTP request model bound from the query string.
/// </summary>
public record GetTicketTagsRequest(
    Guid? TagId,
    Guid? TicketId,
    Guid? AssignedById,
    string? SortBy,
    bool? SortDescending,
    int? PageNumber,
    int? PageSize
);