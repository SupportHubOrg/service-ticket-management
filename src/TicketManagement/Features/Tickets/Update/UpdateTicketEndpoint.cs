using System.Security.Claims;
using MediatR;

using SupportHub.TicketManagement.Domain.Enums;

namespace SupportHub.TicketManagement.Features.Tickets.UpdateTicket;

public static class UpdateTicketEndpoint
{
    /// <summary>
    /// Maps the endpoint for updating a ticket's status and assigned operator.
    /// </summary>
    /// <param name="app"></param>
    public static void MapUpdateTicketEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/tickets/{ticketId}", async (
            Guid ticketId,
            UpdateTicketRequest request,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator ) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command to update the ticket
            var command = new UpdateTicketCommand(userId, ticketId, request.Status, request.OperatorId);
            await mediator.Send(command);

            //Return success response
            return Results.NoContent();
        }).RequireAuthorization();
    }
}

public record UpdateTicketRequest(Status Status, Guid? OperatorId);