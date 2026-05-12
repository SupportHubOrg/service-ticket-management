using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.GetTicket;

public static class GetTicketEndpoint
{
    /// <summary>
    /// Maps the endpoint for retrieving a specific ticket by ID.
    /// </summary>
    public static void MapGetTicketEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tickets/{ticketId}", async (
            Guid ticketId,
            HttpContext httpContext,
            ClaimsPrincipal claimsPrincipal,
            IMediator mediator) =>
        {
            //Extract the user ID from JWT claims
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the query
            var query = new GetTicketQuery(ticketId, userId);
            var result = await mediator.Send(query);

            //Return 200 OK with the ticket details DTO
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}