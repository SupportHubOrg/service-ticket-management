using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.Tickets.DeleteTicket;

public static class DeleteTicketEndpoint
{
    /// <summary>
    /// Maps the endpoint for deleting a ticket.
    /// </summary>
    public static void MapDeleteTicketEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/tickets/{ticketId}", async (
            Guid ticketId,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command to delete the ticket
            var command = new DeleteTicketCommand(ticketId, userId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}