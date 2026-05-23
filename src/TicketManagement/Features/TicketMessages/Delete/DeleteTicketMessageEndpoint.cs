using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketMessages.DeleteTicketMessage;

public static class DeleteTicketMessageEndpoint
{
    /// <summary>
    /// Maps the endpoint for deleting a ticket.
    /// </summary>
    public static void MapDeleteTicketMessageEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/tickets/messages/{ticketMessageId}", async (
            Guid ticketMessageId,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command to delete the ticket
            var command = new DeleteTicketMessageCommand(ticketMessageId, userId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}