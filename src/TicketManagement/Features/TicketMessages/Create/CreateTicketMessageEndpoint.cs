using System.Security.Claims;
using MediatR;

namespace SupportHub.TicketManagement.Features.TicketMessages.CreateTicketMessage;

public static class CreateTicketMessageEndpoint
{
    /// <summary>
    /// Maps the endpoint for creating a new support ticket.
    /// </summary>
    /// <param name="app"></param>
    public static void MapCreateTicketMessageEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tickets/{ticketId}/messages", async (
            Guid ticketId,
            CreateTicketMessageRequest request,
            HttpContext httpContext,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            //Extract user ID from JWT claims
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid userId))
                return Results.Unauthorized();

            //Create and send the command
            var command = new CreateTicketMessageCommand(userId, ticketId, request.Message);
            var ticketMessageId = await mediator.Send(command);

            //Return 201 Created with the new resource route
            return Results.Created($"/api/tickets/{ticketId}/messages/{ticketMessageId}", new CreateTicketMessageResult(ticketMessageId));
        }).RequireAuthorization();
    }
}

public record CreateTicketMessageRequest(string Message);
public record CreateTicketMessageResult(Guid TicketMessageId);