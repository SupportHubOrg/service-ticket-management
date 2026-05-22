using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tags.GetTag;

public static class GetTagEndpoint
{
    /// <summary>
    /// Maps the endpoint for retrieving a specific Tag by ID.
    /// </summary>
    public static void MapGetTagEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/Tags/{TagId}", async (
            Guid TagId,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract the user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the query
            var query = new GetTagQuery(TagId, userId);
            var result = await mediator.Send(query);

            //Return 200 OK with the Tag details DTO
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}