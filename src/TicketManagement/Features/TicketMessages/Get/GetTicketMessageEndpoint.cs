using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketMessages.GetTicketMessage;

public static class GetTicketMessageEndpoint
{
    /// <summary>
    /// Maps the endpoint for retrieving a specific ticket by ID.
    /// </summary>
    public static void MapGetTicketMessageEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tickets/messages/{ticketMessageId}", async (
            Guid ticketMessageId,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract the user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the query
            var query = new GetTicketMessageQuery(ticketMessageId, userId);
            var result = await mediator.Send(query);

            //Return 200 OK with the ticket details DTO
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}